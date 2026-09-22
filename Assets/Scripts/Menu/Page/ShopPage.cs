using System.Collections.Generic;
using IAP;
using Shop;
using UnityEngine;

namespace Menu.Page
{
	public class ShopPage : BasePage
	{
		[SerializeField]
		private Transform contentRoot;

		[SerializeField]
		private List<ShopSection> sections;

		public override void Prepare()
		{
			ReprepareWithSameSource();
		}

		private void ReprepareWithSameSource()
		{
			if (sections == null)
			{
				return;
			}
			int visibleCount = 0;
			foreach (ShopSection section in sections)
			{
				if (section != null && section.ShouldShowSection())
				{
					visibleCount++;
				}
			}
			foreach (ShopSection section in sections)
			{
				if (section == null)
				{
					continue;
				}
				bool shouldShow = section.ShouldShowSection();
				section.gameObject.SetActive(shouldShow);
				if (!shouldShow)
				{
					continue;
				}
				section.Init(IAPSource.ShopPage, ReprepareWithSameSource);
				section.SetHeaderActive(visibleCount > 1);
			}
		}
	}
}
