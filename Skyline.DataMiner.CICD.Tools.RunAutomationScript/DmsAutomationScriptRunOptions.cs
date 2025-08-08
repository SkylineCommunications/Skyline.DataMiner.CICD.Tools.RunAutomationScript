namespace Skyline.DataMiner.CICD.Tools.RunAutomationScript
{
	/// <summary>
	/// Defines the options to execute an Automation Script.
	/// </summary>
	public class DmsAutomationScriptRunOptions
	{
		/// <summary>
		/// Gets or sets the DataMiner Agent where the script will be executed.
		/// If <c>null</c> the script will be executed in the agent the method was called.
		/// </summary>
		public int? AgentId { get; set; } = null;

		/// <summary>
		/// Gets or sets a value indicating whether extended error info is desired or not.
		/// Default value is <c>true</c>.
		/// </summary>
		public bool ExtendedErrorInfo { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether lock elements will be forced.
		/// Default value is <c>false</c>.
		/// </summary>
		public bool ForceLockElements { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether elements will be locked.
		/// Default value is <c>false</c>.
		/// </summary>
		public bool LockElements { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether gets after sets will be performed.
		/// Default value is <c>true</c>.
		/// </summary>
		public bool CheckSets { get; set; } = true;

		/// <summary>
		/// Gets or sets the user that will execute the script.
		/// If <c>null</c> the script will be executed with the user from the SLNet connection. 
		/// </summary>
		public string? UserCookie { get; set; } = null;

		/// <summary>
		/// Gets or sets a value indicating whether wait when locked is enabled.
		/// Default value is <c>true</c>.
		/// </summary>
		public bool WaitWhenLocked { get; set; } = true;
	}
}
