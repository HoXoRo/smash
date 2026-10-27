namespace Util
{
	// Preserve enum names used by performance logs and debug information.
	[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.Field)]
	public enum DevicePerformanceTier
	{
		Low = 0,
		Mid = 1,
		High = 2
	}
}
