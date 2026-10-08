using Inventory.TimedInventory;
using Util;

namespace Life
{
	public static class LifeHelper
	{
		public const int MaxLifeCount = PlayerDataModel.MaxLifeCount;

		private static PlayerDataModel PlayerData => GF.DataModel != null ? GF.DataModel.GetDataModel<PlayerDataModel>() : null;

		public static void Refresh()
		{
			if (PlayerData == null) return;
			PlayerData.RefreshLife();
			TriggerTrackers();
		}

		public static void LoseLife()
		{
			if (PlayerData == null || TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife)) return;
			PlayerData.RefreshLife();
			SetCurrentLifeCount(PlayerData.LifeCount - 1);
		}

		public static void AddLife(int count)
		{
			if (PlayerData == null) return;
			PlayerData.AddLife(count);
			TriggerTrackers();
		}

		public static int GetCurrentLifeCount()
		{
			return PlayerData != null ? PlayerData.LifeCount : MaxLifeCount;
		}

		public static bool HasLife()
		{
			Refresh();
			return TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife) || GetCurrentLifeCount() > 0;
		}

		public static void FullLife()
		{
			SetCurrentLifeCount(MaxLifeCount);
		}

		public static void SetCurrentLifeCount(int val, bool triggerTrackers = true)
		{
			if (PlayerData == null) return;
			PlayerData.SetLifeCount(val);
			if (triggerTrackers) TriggerTrackers();
		}

		public static int GetLifeTimerStart()
		{
			return PlayerData != null ? PlayerData.LifeTimerStart : 0;
		}

		public static void StartLifeTimer()
		{
			if (PlayerData == null) return;
			PlayerData.LifeTimerStart = TimeUtil.GetNowSecondsUtc();
			PlayerData.Save();
			TriggerTrackers();
		}

		public static int GetSecondsUntilNextLife()
		{
			return PlayerData != null ? PlayerData.GetSecondsUntilNextLife() : -1;
		}

		private static void TriggerTrackers()
		{
			LifeTrackerHelper.TriggerLifeCountTrackers();
			LifeTrackerHelper.TriggerLifeTimerTrackers();
		}

	}
}
