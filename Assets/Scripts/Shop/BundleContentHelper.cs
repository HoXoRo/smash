using IAP;

namespace Shop
{
	public static class BundleContentHelper
	{
		public static BundleContentItemType CoinAmountToType(int amount)
		{
			return amount <= 14999 ? (amount >= 6000 ? BundleContentItemType.Coin3 : BundleContentItemType.Coin1) : BundleContentItemType.Coin5;
		}

		public static string GetBundleName(IAPItemType type)
		{
			return type switch
			{
				IAPItemType.StarterPack => "Starter Pack",
				IAPItemType.Bundle1 => "Bundle 1",
				IAPItemType.Bundle2 => "Bundle 2",
				IAPItemType.Bundle3 => "Bundle 3",
				IAPItemType.FailOffer => "Special Offer",
				_ => string.Empty
			};
		}
	}
}
