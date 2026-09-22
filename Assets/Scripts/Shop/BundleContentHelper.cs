namespace Shop
{
	public static class BundleContentHelper
	{
		public static BundleContentItemType CoinAmountToType(int amount)
		{
			return amount <= 14999 ? (amount >= 6000 ? BundleContentItemType.Coin3 : BundleContentItemType.Coin1) : BundleContentItemType.Coin5;
		}
	}
}
