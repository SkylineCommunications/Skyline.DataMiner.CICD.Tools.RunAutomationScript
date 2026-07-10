namespace Skyline.DataMiner.CICD.Tools.RunAutomationScript
{
	using System;
	using System.Linq;

	using global::Skyline.DataMiner.Net.GRPCConnection;
	using global::Skyline.DataMiner.Net.Messages;

	using Skyline.DataMiner.Net;

	internal sealed class SLNetCommunication : IDisposable
	{
		private SLNetCommunication(string hostname, string username, string password)
		{
			if (hostname.Contains(".dataminer.services"))
			{
				throw new InvalidOperationException("Unable to directly deploy to a cloud agent. Please use CatalogUpload tool and Deployment from-catalog with this tool.");
			}

			try
			{
				Connection = new GRPCConnection(hostname);
			}
			catch (Exception ex)
			{
				// DataMinerOfflineException when APIGateway is not present or too low version. (Tested on DM 10.1 & 10.2)
				// Keeping it a generic catch to deal with possible differences between DM versions. Error should be clearer instead of an unclear error from DataMiner.

				throw new InvalidOperationException("Unable to reach DataMiner. Make sure that DataMiner and APIGateway are up and running and DataMiner has a minimum version of MR 10.3 / FR 10.3.2 ", ex);
			}

			Connection.PollingRequestTimeout = 120000;
			Connection.ConnectTimeoutTime = 120000;
			Connection.AuthenticateMessageTimeout = 120000;
			Connection.Authenticate(username, password);

			// NOTE: Do NOT open a subscription here. Executing an automation script only needs a
			// plain request/response connection (a single synchronous ExecuteScriptMessage).
			// Calling Connection.Subscribe(...) upgrades this to a stateful, server-tracked
			// subscribed session that DataMiner keeps registered until a heartbeat/connection-check
			// timeout (see the 120000 ms timeouts above) even after the client channel is disposed.
			// When the tool is invoked rapidly in a loop (e.g. a stability test running the same
			// script dozens of times), those lingering server-side sessions accumulate and hit the
			// per-user SLNet connection cap (default 40), making the ~41st invocation fail with
			// "maximum number of connections". A subscription-free connection is reaped promptly, so
			// it does not accumulate.

			EndPoint = hostname;
		}

		public Connection Connection { get; }

		public string EndPoint { get; private set; }

		public static SLNetCommunication GetConnection(string endUrlPoint, string username, string password)
		{
			return new SLNetCommunication(endUrlPoint, username, password);
		}

		public void Dispose()
		{
			// Defensively release any server-side subscription state before tearing down the
			// connection. Disposing the connection only drops the local gRPC channel; it does not
			// send a graceful logout, so the server would otherwise retain any subscription until a
			// heartbeat timeout. ClearSubscriptions() is a no-op when nothing is subscribed, so this
			// is safe even though we no longer call Subscribe(...) above.
			try
			{
				Connection.ClearSubscriptions();
			}
			catch (Exception)
			{
				// Best effort: the connection may already be gone. Never let cleanup throw on dispose.
			}

			Connection.Dispose();
		}

		public DMSMessage[] SendMessage(DMSMessage message)
		{

			var result = Connection.SendAsyncOverConnection(new[] { message }, 3600);
			return result;
		}

		public DMSMessage? SendSingleResponseMessage(DMSMessage message)
		{
			var result = Connection.SendAsyncOverConnection(new[] { message }, 3600);
			return result.FirstOrDefault();
		}
	}
}
