using System;
using System.Collections.Generic;
using ABTesting;
using Gameplay;
using IAP;
using LocalSave;
using UnityEngine;

namespace Shop
{
	public class ShopSection : MonoBehaviour
	{
		[SerializeField]
		private ShopSectionType sectionType;

		[SerializeField]
		private List<ShopItemBase> items;

		[SerializeField]
		private GameObject header;

		public void Init(IAPSource source, Action onPurchaseSuccess)
		{
			if (items == null)
			{
				return;
			}
			foreach (ShopItemBase item in items)
			{
				if (item != null)
				{
					item.Init(source, onPurchaseSuccess, null);
				}
			}
		}

		public bool ShouldShowSection()
		{
			return false;
		}

		public void SetHeaderActive(bool active)
		{
			if (header != null)
			{
				header.SetActive(active);
			}
		}
	}
}
