using System;
using System.Collections.Generic;

namespace Inventory.TimedInventory
{
	public static class TimedInventoryTrackerHelper
	{
		private static readonly Dictionary<TimedInventoryItemType, Action<int>> _trackers;

		static TimedInventoryTrackerHelper()
		{
			_trackers = new Dictionary<TimedInventoryItemType, Action<int>>();
		}

		public static void AddTracker(TimedInventoryItemType itemType, Action<int> listener)
		{
			_trackers.TryGetValue(itemType, out Action<int> existing);
			_trackers[itemType] = (Action<int>)Delegate.Combine(existing, listener);
			TriggerTrackers(itemType);
		}

		public static void RemoveTracker(TimedInventoryItemType itemType, Action<int> listener)
		{
			if (!_trackers.TryGetValue(itemType, out Action<int> existing))
			{
				return;
			}
			_trackers[itemType] = (Action<int>)Delegate.Remove(existing, listener);
		}

		public static void TriggerTrackers(TimedInventoryItemType itemType)
		{
			if (!_trackers.TryGetValue(itemType, out Action<int> listener) || listener == null)
			{
				return;
			}
			listener(TimedInventoryHelper.GetTime(itemType));
		}
	}
}
