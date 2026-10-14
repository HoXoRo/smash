namespace Util
{
	public static class FormatUtils
	{
		public static string FormatSecondsTime(int totalSeconds)
		{
			if (totalSeconds < 0)
			{
				totalSeconds = 0;
			}
			int hours = totalSeconds / 3600;
			int minutes = totalSeconds % 3600 / 60;
			int seconds = totalSeconds % 60;
			return hours > 0 ? string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds) : string.Format("{0:00}:{1:00}", minutes, seconds);
		}

		public static string FormatSecondsReward(int secondsAmount)
		{
			if (secondsAmount < 0)
			{
				secondsAmount = 0;
			}
			int hours = secondsAmount / 3600;
			int minutes = secondsAmount % 3600 / 60;
			int seconds = secondsAmount % 60;
			if (hours > 0)
			{
				return string.Format("{0}h:{1}m:{2}s", hours, minutes, seconds);
			}
			return minutes > 0 ? string.Format("{0}m:{1}s", minutes, seconds) : string.Format("{0}s", seconds);
		}
	}
}
