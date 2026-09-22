namespace Gameplay
{
	using LocalSave;

	public static class StreakHelper
	{
		public static bool StreakEnabled;

		public const int StreakUnlockLevel = 51;

		public const int MaxStreak = 3;

		private static int _cachedStreakCountForGameplay;

		public static void OnGameplayLevelStarted(int levelIndex)
		{
			if (!StreakEnabled)
			{
				return;
			}
			_cachedStreakCountForGameplay = levelIndex;
			SaveService.Data.StreakCount = 0;
		}

		public static int GetStreakCount()
		{
			if (!StreakEnabled)
			{
				return 0;
			}
			return SaveService.Data.StreakCount > MaxStreak ? MaxStreak : SaveService.Data.StreakCount;
		}

		public static bool IsUnlocked()
		{
			return StreakEnabled && SaveService.Data.Level >= StreakUnlockLevel;
		}

		public static void OnLevelWon(int completedLevelIndex, int activeStreakCount)
		{
			if (!StreakEnabled)
			{
				return;
			}
			int streakCount = completedLevelIndex >= StreakUnlockLevel ? activeStreakCount + 1 : 0;
			SaveService.Data.StreakCount = streakCount > MaxStreak ? MaxStreak : streakCount;
			_cachedStreakCountForGameplay = 0;
		}

		public static void OnLevelLostOrAbandoned()
		{
			SaveService.Data.StreakCount = 0;
			_cachedStreakCountForGameplay = 0;
		}

		public static bool ShouldShowPrelevelTutorial(int level)
		{
			return level >= StreakUnlockLevel && StreakEnabled && !SaveService.Data.TutorialProgress.tutorialPrelevelStreakDone;
		}

		public static void MarkPrelevelTutorialDone()
		{
			SaveService.Data.TutorialProgress.tutorialPrelevelStreakDone = true;
			SaveService.Save();
		}

		public static int GetStreakCountForAnalytics()
		{
			return SaveService.Data.StreakCount;
		}
	}
}
