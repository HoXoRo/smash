using Inventory.TimedInventory;

namespace Inventory
{
	public static class InventoryAnalyticsHelper
	{
		public static void SendEarnEvent(InventoryPayload payload, InventoryEarnSource source)
		{
			_ = GetItemTypeString(payload.Type);
			_ = payload.Amount;
			_ = GetEarnSourceString(source);
		}

		public static void SendEarnEvent(TimedInventoryPayload payload, InventoryEarnSource source)
		{
			_ = GetItemTypeString(payload.Type);
			_ = payload.TimeAmount;
			_ = GetEarnSourceString(source);
		}

		public static void SendSpendEvent(InventoryPayload payload, InventorySpendSource source)
		{
			_ = GetItemTypeString(payload.Type);
			_ = payload.Amount;
			_ = GetSpendSourceString(source);
		}

		private static string GetItemTypeString(InventoryItemType type)
		{
			return type switch
			{
				InventoryItemType.Coin => "coin",
				InventoryItemType.PrelevelRocket => "prelevel_rocket",
				_ => string.Empty
			};
		}

		private static string GetItemTypeString(TimedInventoryItemType type)
		{
			return type == TimedInventoryItemType.UnlimitedLife ? "unlimited_life" : string.Empty;
		}

		private static string GetEarnSourceString(InventoryEarnSource source)
		{
			return source switch
			{
				InventoryEarnSource.LevelWin => "level_win",
				InventoryEarnSource.Purchase => "purchase",
				InventoryEarnSource.PrelevelBoosterBuy => "prelevel_booster_buy",
				_ => "none"
			};
		}

		private static string GetSpendSourceString(InventorySpendSource source)
		{
			return source switch
			{
				InventorySpendSource.EndGameOffer => "end_game_offer",
				InventorySpendSource.MoreLives => "more_lives",
				InventorySpendSource.Prelevel => "prelevel",
				InventorySpendSource.PrelevelBoosterBuy => "prelevel_booster_buy",
				_ => string.Empty
			};
		}
	}
}
