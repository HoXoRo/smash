using Inventory;
using Life;
using LocalSave;
using Service;
using Util;

namespace Inventory.TimedInventory
{
	public static class TimedInventoryHelper
	{
		private static TimeHelper _subscribedTimeHelper;

		private static PlayerDataModel PlayerData
		{
			get
			{
				PlayerDataModel playerData = GF.DataModel != null ? GF.DataModel.GetDataModel<PlayerDataModel>() : null;
				if (playerData != null && !playerData.UnlimitedLifeStorageMigrated)
				{
					int oldTime = SaveService.Data.UnlimitedLifeTime;
					int oldEnd = SaveService.Data.UnlimitedLifeEnd;
					if (oldTime > 0 && playerData.UnlimitedLifeEnd <= 0)
					{
						playerData.UnlimitedLifeTime = oldTime;
						playerData.UnlimitedLifeEnd = oldEnd > 0 ? oldEnd : TimeUtil.GetNowSecondsUtc() + oldTime;
					}
					playerData.UnlimitedLifeStorageMigrated = true;
					playerData.Save();
				}
				return playerData;
			}
		}

		static TimedInventoryHelper()
		{
			EnsureSubscribed();
			CheckAll();
		}

		private static void EnsureSubscribed()
		{
			TimeHelper timeHelper = ServiceLocator.Get<TimeHelper>() ?? UnityEngine.Object.FindObjectOfType<TimeHelper>();
			if (timeHelper == null || timeHelper == _subscribedTimeHelper)
			{
				return;
			}
			if (_subscribedTimeHelper != null)
			{
				_subscribedTimeHelper.OnTick -= CheckAll;
			}
			timeHelper.OnTick -= CheckAll;
			timeHelper.OnTick += CheckAll;
			_subscribedTimeHelper = timeHelper;
		}

		private static void CheckAll()
		{
			EnsureSubscribed();
			Check(TimedInventoryItemType.UnlimitedLife);
		}

		private static void Check(TimedInventoryItemType itemType)
		{
			int time = GetTime(itemType);
			if (time <= 0)
			{
				return;
			}
			int now = TimeUtil.GetNowSecondsUtc();
			int end = GetEnd(itemType);
			if (end <= 0)
			{
				SetEnd(itemType, now + time);
			}
			else
			{
				int remaining = end - now;
				if (remaining < time)
				{
					SetTime(itemType, remaining > 0 ? remaining : 0);
				}
			}
			TimedInventoryTrackerHelper.TriggerTrackers(itemType);
		}

		public static int GetTime(TimedInventoryItemType itemType)
		{
			EnsureSubscribed();
			return itemType == TimedInventoryItemType.UnlimitedLife && PlayerData != null ? PlayerData.UnlimitedLifeTime : 0;
		}

		public static bool HasTime(TimedInventoryItemType itemType)
		{
			Check(itemType);
			return GetTime(itemType) > 0;
		}

		private static int GetEnd(TimedInventoryItemType itemType)
		{
			return itemType == TimedInventoryItemType.UnlimitedLife && PlayerData != null ? PlayerData.UnlimitedLifeEnd : 0;
		}

		private static void SetTime(TimedInventoryItemType itemType, int time)
		{
			if (itemType == TimedInventoryItemType.UnlimitedLife)
			{
				if (PlayerData != null) PlayerData.UnlimitedLifeTime = time;
			}
		}

		private static void SetEnd(TimedInventoryItemType itemType, int end)
		{
			if (itemType == TimedInventoryItemType.UnlimitedLife)
			{
				if (PlayerData == null) return;
				PlayerData.UnlimitedLifeEnd = end;
				PlayerData.Save();
			}
		}

		public static void AddTime(TimedInventoryPayload payload, InventoryEarnSource source)
		{
			AddTimeInternal(payload);
			InventoryAnalyticsHelper.SendEarnEvent(payload, source);
		}

		public static void AddTimeForTest(TimedInventoryPayload payload)
		{
			AddTimeInternal(payload);
		}

		private static void AddTimeInternal(TimedInventoryPayload payload)
		{
			EnsureSubscribed();
			Check(payload.Type);
			int newTime = GetTime(payload.Type) + payload.TimeAmount;
			SetTime(payload.Type, newTime);
			SetEnd(payload.Type, TimeUtil.GetNowSecondsUtc() + newTime);
			if (payload.Type == TimedInventoryItemType.UnlimitedLife)
			{
				LifeHelper.FullLife();
			}
			TimedInventoryTrackerHelper.TriggerTrackers(payload.Type);
		}
	}
}
