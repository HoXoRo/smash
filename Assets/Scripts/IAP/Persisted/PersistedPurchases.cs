using System;
using System.Collections.Generic;

namespace IAP.Persisted
{
	[Serializable]
	public class PersistedPurchases
	{
		public List<PersistedPurchaseItem> purchases;

		public static PersistedPurchases GetDefault()
		{
			return new PersistedPurchases
			{
				purchases = new List<PersistedPurchaseItem>()
			};
		}
	}
}
