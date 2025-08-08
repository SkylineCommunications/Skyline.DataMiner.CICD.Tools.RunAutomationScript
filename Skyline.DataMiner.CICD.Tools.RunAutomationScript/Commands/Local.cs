using System.Runtime.InteropServices;
using System.Xml.Linq;

using Skyline.DataMiner.CICD.Tools.RunAutomationScript.SystemCommandLine;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Correlation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.ReportsAndDashboards;

namespace Skyline.DataMiner.CICD.Tools.RunAutomationScript.Commands
{
	internal class Local : Command
	{
		public Local() :
			base(name: "Local", description: "Executes an automationscript on the local agent without input parameters or dummies.")
		{
			AddOption(new System.CommandLine.Option<string?>(
				aliases: ["--script-name", "-sn"],
				description: "The automation script name.")
			{
				IsRequired = true
			}
			);

			var checkSetsOption = new System.CommandLine.Option<bool>(
				aliases: ["--check-sets", "-cs"],
				description: "Indicates whether gets after sets will be performed. Default value is true.")
			{
				IsRequired = false,
			};

			checkSetsOption.SetDefaultValue(true);
			AddOption(checkSetsOption);

			var extendedErrorOption = new System.CommandLine.Option<bool>(
				aliases: ["--extended-error-info", "-eri"],
				description: "Indicates whether extended error info is desired or not. Default value is true.")
			{
				IsRequired = false,
			};

			extendedErrorOption.SetDefaultValue(true);
			AddOption(extendedErrorOption);

			var isSynchronousOption = new System.CommandLine.Option<bool>(
				aliases: ["--is-synchronous", "-is"],
				description: "Indicates whether this call should wait until the script finishes or return immediately. Default value is true.")
			{
				IsRequired = false,
			};

			isSynchronousOption.SetDefaultValue(true);
			AddOption(isSynchronousOption);

			var dataMinerUserNameOption = new System.CommandLine.Option<string?>(
				aliases: ["--username", "-user"],
				description: "Username for a DataMiner user with permissions to execute automation scripts. This is optional, the username can also be provided using the 'DATAMINER_DEPLOY_USER' environment variable (unix/win) or using 'DATAMINER_DEPLOY_USER_ENCRYPTED' configured with Skyline.DataMiner.CICD.Tools.WinEncryptedKeys (windows).")
			{
				IsRequired = false,
			};

			dataMinerUserNameOption.SetDefaultValue(String.Empty);
			AddOption(dataMinerUserNameOption);

			var dataMinerPasswordOption = new System.CommandLine.Option<string?>(
				aliases: ["--password", "-pass"],
				description: "Password for a DataMiner user with permissions to execute automation scripts. This is optional, the password can also be provided using the 'DATAMINER_DEPLOY_PASSWORD' environment variable (unix/win) or using 'DATAMINER_DEPLOY_PASSWORD_ENCRYPTED' configured with Skyline.DataMiner.CICD.Tools.WinEncryptedKeys (windows).")
			{
				IsRequired = false,
			};

			dataMinerPasswordOption.SetDefaultValue(String.Empty);
			AddOption(dataMinerPasswordOption);
		}
	}

	internal class LocalCommandHandler(ILogger<LocalCommandHandler> logger, IConfiguration configuration) : ICommandHandler
	{
		/*
         * Automatic binding with System.CommandLine.NamingConventionBinder
         * The property names need to match with the command line argument names.
         * Example: --example-package-file will bind to ExamplePackageFile
         */

		private string? pwFromEnv;

		private string? userFromEnv;

		public bool CheckSets { get; set; }

		public bool ExtendedErrorInfo { get; set; }

		public bool IsSynchronous { get; set; }

		public string? Password { get; set; }

		public required string ScriptName { get; set; }

		public string? Username { get; set; }

		public int Invoke(InvocationContext context)
		{
			// InvokeAsync is called in Program.cs
			return (int)ExitCodes.NotImplemented;
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<int> InvokeAsync(InvocationContext context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			logger.LogDebug("Starting {method}...", nameof(Local));

			try
			{
				// Retrieve user secrets/environment variables
				TryFindEnvironmentKeys();
				string? actualUser;
				string? actualPassword;

				if (!String.IsNullOrWhiteSpace(Username))
				{
					actualUser = Username;
					logger.LogDebug("User provided through arguments, this takes precedence over environment variables.");
				}
				else
				{
					actualUser = userFromEnv;
				}

				if (!String.IsNullOrWhiteSpace(Password))
				{
					actualPassword = Password;
					logger.LogDebug("Password provided through arguments, this takes precedence over environment variables.");
				}
				else
				{
					actualPassword = pwFromEnv;
				}

				logger.LogInformation("Starting execution of automationscript {scriptname}...", ScriptName);

				var runOptions = new DmsAutomationScriptRunOptions()
				{
					CheckSets = CheckSets,
					ExtendedErrorInfo = ExtendedErrorInfo,
					LockElements = false,
					ForceLockElements = false,
					WaitWhenLocked = true
				};

				if (actualUser == null || actualPassword == null)
				{
					logger.LogError("DataMiner user and/or password was null. Please provide input arguments or provide (non)encrypted environment variables");
					return (int)ExitCodes.Fail;
				}

				using (var slnet = SLNetCommunication.GetConnection("localhost", actualUser, actualPassword))
				{
					List<string> actionArray = new List<string>();
					actionArray.Add(ParseRunFlags(runOptions));

					if (runOptions.CheckSets)
					{
						actionArray.Add("CHECKSETS:TRUE");
					}
					else
					{
						actionArray.Add("CHECKSETS:FALSE");
					}

					if (runOptions.ExtendedErrorInfo)
					{
						actionArray.Add("EXTENDED_ERROR_INFO");
					}

					if (IsSynchronous)
					{
						actionArray.Add("DEFER:FALSE");
					}
					else
					{
						actionArray.Add("DEFER:TRUE");
					}

					var msg = new ExecuteScriptMessage(ScriptName)
					{
						Options = new SA(actionArray.ToArray())
					};

					var response = slnet.SendSingleResponseMessage(msg) as ExecuteScriptResponseMessage;

					if (response != null)
					{
						if (response.ScriptOutput != null)
						{
							foreach (var outputLine in response.ScriptOutput)
							{
								logger.LogDebug("{key}:{value}", outputLine.Key, outputLine.Value);
							}
						}

						if (response.HadError)
						{
							foreach (var errorMessage in response.ErrorMessages)
							{
								logger.LogError("Failed to execute automation script: (errorcode {errorcode}) {errorMessage}", response.ErrorCode, errorMessage);
							}

							return (int)ExitCodes.Fail;
						}
					}
					else
					{
						logger.LogError("Automation script execution returned a null response.");
						return (int)ExitCodes.Fail;
					}
				}

				logger.LogInformation("Finished execution of automationscript {scriptname}...", ScriptName);
				return (int)ExitCodes.Ok;
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed the example command.");
				return (int)ExitCodes.UnexpectedException;
			}
			finally
			{
				logger.LogDebug("Finished {method}.", nameof(Local));
			}
		}

		internal static string ParseRunFlags(DmsAutomationScriptRunOptions runOptions)
		{
			var scriptRunFlags = runOptions.LockElements ? DmsAutomationScriptRunFlags.Lock : DmsAutomationScriptRunFlags.None;

			if (runOptions.ForceLockElements)
			{
				scriptRunFlags |= DmsAutomationScriptRunFlags.ForceLock;
			}

			if (!runOptions.WaitWhenLocked)
			{
				scriptRunFlags |= DmsAutomationScriptRunFlags.NoWait;
			}

			return $"OPTIONS:{(int)scriptRunFlags}";
		}

		private string TryFindEncryptedEnvironmentKey(string key)
		{
			if (WinEncryptedKeys.Lib.Keys.TryRetrieveKey(key, out var encryptedKey))
			{
				logger.LogDebug($"OK: Found token in Env Variable: '{key}' created by WinEncryptedKeys.");
				return encryptedKey;
			}
			else
			{
				return String.Empty;
			}
		}

		private string? TryFindEnvironmentKey(string key)
		{
			string? userFromEnvironment = Environment.GetEnvironmentVariable(key);

			if (String.IsNullOrWhiteSpace(userFromEnvironment))
			{
				return null;
			}

			if (!String.IsNullOrWhiteSpace(userFromEnv))
			{
				logger.LogDebug("OK: Overriding previously encrypted key with found token in Env Variable: 'DATAMINER_DEPLOY_USER'.");
			}
			else
			{
				logger.LogDebug("OK: Found token in Env Variable: 'DATAMINER_DEPLOY_USER'.");
			}

			return userFromEnvironment;
		}

		/// <summary>
		///  Attempts to find the necessary API key in Environment Variables. In order of priority:
		///  <para>- key stored as an Environment Variable called "DATAMINER_X_USER". (unix/win)</para>
		///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "DATAMINER_X_ENCRYPTED" (windows only)</para>
		/// </summary>
		private void TryFindEnvironmentKeys()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Order of priority. Priority for regular environment keys as they are win/unix and industry standard in pipelines
				userFromEnv = TryFindEncryptedEnvironmentKey("DATAMINER_DEPLOY_USER_ENCRYPTED") ?? userFromEnv;
				pwFromEnv = TryFindEncryptedEnvironmentKey("DATAMINER_DEPLOY_PASSWORD_ENCRYPTED") ?? pwFromEnv;
			}

			userFromEnv = TryFindEnvironmentKey("DATAMINER_DEPLOY_USER") ?? userFromEnv;
			pwFromEnv = TryFindEnvironmentKey("DATAMINER_DEPLOY_PASSWORD") ?? pwFromEnv;
		}
	}
}