namespace Skyline.DataMiner.CICD.Tools.RunAutomationScript
{
	using System;

	[Flags]
#pragma warning disable S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
	internal enum DmsAutomationScriptRunFlags
#pragma warning restore S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
	{
		None = 0,
		Lock = 1,
		ForceLock = 2,
		NoWait = 4,
	}

}
