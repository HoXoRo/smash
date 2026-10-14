namespace Util
{
	public static class TimeUtil
	{
		public static int GetNowSecondsUtc()
		{
			return (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		public static int GetNowSecondsLocal()
		{
			return (int)System.DateTimeOffset.Now.ToUnixTimeSeconds();
		}

		public static int GetDayCountSinceEpoch()
		{
			return GetNowSecondsUtc() / 86400;
		}

		public static int GetDaysPassedSince(int day)
		{
			return GetDayCountSinceEpoch() - day;
		}
	}
}
