using System;
using System.Collections.Generic;
using Inventory;
using Inventory.TimedInventory;

namespace IAP
{
	public class IAPItemData
	{
		public IAPItemType IAPItemType;

		public string ProductId;

		public List<InventoryPayload> Payload;

		public List<TimedInventoryPayload> TimedPayload;

		public Action CustomProcessor;
	}
}
