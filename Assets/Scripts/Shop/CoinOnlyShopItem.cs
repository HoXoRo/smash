using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

namespace Shop
{
	public class CoinOnlyShopItem : ShopItemBase
	{
		[SerializeField]
		private Image rewardIcon;

		[SerializeField]
		private TextMeshProUGUI rewardAmountText;

		protected override void Prepare()
		{
			base.Prepare();
		}

		private string FormatAmount(int number)
		{
			return number.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
		}
	}
}
