namespace IAP
{
	public static class IAPSourceExtensions
	{
		public static string ToSourceString(this IAPSource source)
		{
			return source switch
			{
				IAPSource.ShopPage => "shop_page",
				IAPSource.EndGameOfferShop => "end_game_offer_shop",
				IAPSource.MoreLivesShop => "more_lives_shop",
				IAPSource.NoAdsPopup => "no_ads_popup",
				IAPSource.PrelevelBoosterBuy => "prelevel_booster_buy",
				IAPSource.FailOffer => "fail_offer",
				_ => "unknown"
			};
		}
	}
}
