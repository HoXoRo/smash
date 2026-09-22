using System;

namespace IAP.Persisted
{
	[Serializable]
	public class PersistedPurchaseItem
	{
		public string productId;

		public float price;

		public string currency;
	}
}
