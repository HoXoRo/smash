using TMPro;
using UnityEngine;
using System.Globalization;
using Util;

namespace Shop
{
	public class BundleContentItem : MonoBehaviour
	{
		[SerializeField]
		private BundleContentItemType contentItemType;

		[SerializeField]
		private TextMeshProUGUI itemAmountText;

		public void SetAmount(int amount)
		{
			itemAmountText.text = FormatAmount(contentItemType, amount);
		}

		private static string FormatAmount(BundleContentItemType type, int amount)
		{
			if (type >= BundleContentItemType.Coin1 && type <= BundleContentItemType.Coin5)
			{
				return amount.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
			}
			if (type == BundleContentItemType.PrelevelRocket)
			{
				return string.Format("x{0}", amount);
			}
			return type == BundleContentItemType.UnlimitedLife ? FormatUtils.FormatSecondsReward(amount) : string.Empty;
		}
	}
}
