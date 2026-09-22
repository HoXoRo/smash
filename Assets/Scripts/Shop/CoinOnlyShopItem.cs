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
			if (ItemData?.Payload == null || ItemData.Payload.Count == 0)
			{
				return;
			}
			rewardAmountText.text = FormatAmount(ItemData.Payload[0].Amount);
		}

		private string FormatAmount(int number)
		{
			return number.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
		}
	}
}
