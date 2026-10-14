using System;
using UnityEngine;

namespace Inventory
{
	[Serializable]
	public struct InventoryPayload
	{
		[SerializeField]
		private InventoryItemType type;

		[SerializeField]
		private int amount;

		public InventoryItemType Type => type;

		public int Amount => amount;

		public InventoryPayload(InventoryItemType type, int amount)
		{
			this.type = type;
			this.amount = amount;
		}
	}
}
