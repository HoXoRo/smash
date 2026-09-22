using System;
using System.Collections.Generic;

namespace Inventory
{
	public static class InventoryTrackerHelper
	{
		private static readonly Dictionary<InventoryItemType, Action<int>> _trackers;

		static InventoryTrackerHelper()
		{
			_trackers = new Dictionary<InventoryItemType, Action<int>>();
		}

		public static void AddTracker(InventoryItemType itemType, Action<int> listener)
		{
			_trackers.TryGetValue(itemType, out Action<int> existing);
			_trackers[itemType] = (Action<int>)Delegate.Combine(existing, listener);
			TriggerTrackers(itemType);
		}

		public static void RemoveTracker(InventoryItemType itemType, Action<int> listener)
		{
			if (!_trackers.TryGetValue(itemType, out Action<int> existing))
			{
				return;
			}
			_trackers[itemType] = (Action<int>)Delegate.Remove(existing, listener);
		}

		public static void RemoveAllTrackers()
		{
			_trackers.Clear();
		}

		public static void TriggerTrackers(InventoryItemType itemType)
		{
			if (!_trackers.TryGetValue(itemType, out Action<int> listener) || listener == null)
			{
				return;
			}
			int amount = InventoryHelper.GetAmount(itemType) - InventoryHelper.GetDelayedAmount(itemType);
			listener(amount < 0 ? 0 : amount);
		}
	}
}
