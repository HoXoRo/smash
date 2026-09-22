using System;
using UnityEngine;

namespace Inventory.TimedInventory
{
	[Serializable]
	public struct TimedInventoryPayload
	{
		[SerializeField]
		private TimedInventoryItemType type;

		[SerializeField]
		private int timeAmount;

		public TimedInventoryItemType Type => type;

		public int TimeAmount => timeAmount;

		public TimedInventoryPayload(TimedInventoryItemType type, int timeAmount)
		{
			this.type = type;
			this.timeAmount = timeAmount;
		}
	}
}
