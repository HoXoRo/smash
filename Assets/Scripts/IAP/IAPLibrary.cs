using System;
using System.Collections.Generic;
using System.Linq;
using Inventory;
using Inventory.TimedInventory;
using LocalSave;

namespace IAP
{
	public static class IAPLibrary
	{
		private const int UnlimitedLifeDurationSeconds = 7200;

		private static readonly List<IAPItemData> Items = new List<IAPItemData>
		{
			CreateCoinPack(IAPItemType.Coin1, "flow.cannonfest.coin1", 1000),
			CreateCoinPack(IAPItemType.Coin2, "flow.cannonfest.coin2", 3000),
			CreateCoinPack(IAPItemType.Coin3, "flow.cannonfest.coin3", 6000),
			CreateCoinPack(IAPItemType.Coin4, "flow.cannonfest.coin4", 10000),
			CreateCoinPack(IAPItemType.Coin5, "flow.cannonfest.coin5", 20000),
			CreateCoinPack(IAPItemType.Coin6, "flow.cannonfest.coin6", 50000),
			new IAPItemData
			{
				IAPItemType = IAPItemType.NoAds,
				ProductId = "flow.cannonfest.noads",

				CustomProcessor = ProcessNoAds
			},
			CreateBundle(IAPItemType.StarterPack, "flow.cannonfest.starterpack", 3000, 1, true),
			CreateBundle(IAPItemType.Bundle1, "flow.cannonfest.bundle1", 6000, 1, true),
			CreateBundle(IAPItemType.Bundle2, "flow.cannonfest.bundle2", 20000, 2, true),
			CreateBundle(IAPItemType.Bundle3, "flow.cannonfest.bundle3", 50000, 3, true),
			CreateBundle(IAPItemType.FailOffer, "flow.cannonfest.failoffer", 20000, 3, false)
		};

		public static IAPItemData GetDataByIAPItemType(IAPItemType type)
		{
			return Items.FirstOrDefault(x => x.IAPItemType == type);
		}

		public static IAPItemData GetDataById(string productId)
		{
			return Items.FirstOrDefault(x => x.ProductId == productId);
		}

		private static IAPItemData CreateCoinPack(IAPItemType type, string productId, int amount)
		{
			return new IAPItemData
			{
				IAPItemType = type,
				ProductId = productId,

				Payload = new List<InventoryPayload>
				{
					new InventoryPayload(InventoryItemType.Coin, amount)
				}
			};
		}

		private static IAPItemData CreateBundle(IAPItemType type, string productId, int coinAmount, int rocketAmount, bool includeUnlimitedLife)
		{
			IAPItemData data = new IAPItemData
			{
				IAPItemType = type,
				ProductId = productId,

				Payload = new List<InventoryPayload>
				{
					new InventoryPayload(InventoryItemType.Coin, coinAmount),
					new InventoryPayload(InventoryItemType.PrelevelRocket, rocketAmount)
				}
			};
			if (includeUnlimitedLife)
			{
				data.TimedPayload = new List<TimedInventoryPayload>
				{
					new TimedInventoryPayload(TimedInventoryItemType.UnlimitedLife, UnlimitedLifeDurationSeconds)
				};
			}
			return data;
		}

		private static void ProcessNoAds()
		{
			if (SaveService.Data != null)
			{
				SaveService.Data.HasNoAds = true;
			}
		}
	}
}
