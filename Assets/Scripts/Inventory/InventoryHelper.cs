using System.Collections.Generic;
using LocalSave;
using Scene;

namespace Inventory
{
	public static class InventoryHelper
	{
		private static Dictionary<InventoryItemType, int> _delayDict;

		static InventoryHelper()
		{
			_delayDict = new Dictionary<InventoryItemType, int>();
			SceneHandler.OnSceneChange += delegate(SceneType currentScene, SceneType nextScene)
			{
				if (currentScene == SceneType.Menu && nextScene == SceneType.Gameplay)
				{
					ResetDelays();
				}
			};
		}

		public static int GetAmount(InventoryItemType itemType)
		{
			return itemType switch
			{
				InventoryItemType.Coin => SaveService.Data.Coins,
				InventoryItemType.PrelevelRocket => SaveService.Data.PrelevelRocket,
				_ => 0
			};
		}

		private static void SetAmount(InventoryItemType itemType, int amount, bool triggerTrackers = true)
		{
			switch (itemType)
			{
				case InventoryItemType.Coin:
					SaveService.Data.Coins = amount;
					break;
				case InventoryItemType.PrelevelRocket:
					SaveService.Data.PrelevelRocket = amount;
					break;
			}
			if (triggerTrackers)
			{
				InventoryTrackerHelper.TriggerTrackers(itemType);
			}
		}

		public static void AddAmount(InventoryPayload payload, bool delayed, InventoryEarnSource source)
		{
			InventoryAnalyticsHelper.SendEarnEvent(payload, source);
			if (delayed)
			{
				SetAmount(payload.Type, GetAmount(payload.Type) + payload.Amount, false);
				AddDelayedAmount(payload);
			}
			else
			{
				SetAmount(payload.Type, GetAmount(payload.Type) + payload.Amount);
			}
		}

		public static bool TrySpend(InventoryPayload payload, InventorySpendSource source)
		{
			int amount = GetAmount(payload.Type);
			if (amount < payload.Amount)
			{
				return false;
			}
			SetAmount(payload.Type, amount - payload.Amount);
			InventoryAnalyticsHelper.SendSpendEvent(payload, source);
			return true;
		}

		private static void AddDelayedAmount(InventoryPayload payload)
		{
			_delayDict.TryGetValue(payload.Type, out int delayedAmount);
			_delayDict[payload.Type] = delayedAmount + payload.Amount;
			InventoryTrackerHelper.TriggerTrackers(payload.Type);
		}

		public static int GetDelayedAmount(InventoryItemType itemType)
		{
			return _delayDict.TryGetValue(itemType, out int delayedAmount) ? delayedAmount : 0;
		}

		public static void ReduceDelayedAmount(InventoryPayload payload)
		{
			if (!_delayDict.TryGetValue(payload.Type, out int delayedAmount))
			{
				return;
			}
			_delayDict[payload.Type] = delayedAmount > payload.Amount ? delayedAmount - payload.Amount : 0;
			InventoryTrackerHelper.TriggerTrackers(payload.Type);
		}

		public static void ResetDelays()
		{
			_delayDict.Clear();
			foreach (InventoryItemType itemType in System.Enum.GetValues(typeof(InventoryItemType)))
			{
				InventoryTrackerHelper.TriggerTrackers(itemType);
			}
		}
	}
}
