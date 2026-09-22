using System.Collections.Generic;
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
