using System.Linq;
using IAP;
using LocalSave;

namespace IAP.Persisted
{
	public static class PersistedPurchasesHelper
	{
		public static bool GetProductIdBought(string productId)
		{
			return SaveService.Data?.Purchases?.purchases != null && SaveService.Data.Purchases.purchases.Any(x => x.productId == productId);
		}

		public static bool GetIAPItemBought(IAPItemType type)
		{
			IAPItemData data = IAPLibrary.GetDataByIAPItemType(type);
			return data != null && GetProductIdBought(data.ProductId);
		}

		public static bool HasAnyPurchases()
		{
			return SaveService.Data?.Purchases?.purchases != null && SaveService.Data.Purchases.purchases.Count > 0;
		}
	}
}
