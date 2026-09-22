using System.Collections.Generic;
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
			if (sections == null) return;
			foreach (ShopSection section in sections)
			{
				if (section != null) section.gameObject.SetActive(false);
			}
		}

		private void ReprepareWithSameSource()
		{
			if (sections == null)
			{
				return;
			}
			foreach (ShopSection section in sections)
			{
				if (section != null) section.gameObject.SetActive(false);
			}
		}
	}
}
