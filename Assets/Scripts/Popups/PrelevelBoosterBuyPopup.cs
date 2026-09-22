using System.Collections.Generic;
using Gameplay;
using Inventory;
using Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Popups
{
	public class PrelevelBoosterBuyPopup : BasePopup
	{
		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private FlowButton buyButton;

		[SerializeField]
		private Image boosterIcon;

		[SerializeField]
		private TextMeshProUGUI boosterCountText;

		[SerializeField]
		private TextMeshProUGUI priceText;

		[SerializeField]
		private TextMeshProUGUI headerText;

		[SerializeField]
		private Sprite[] boosterIconSprites;

		private PrelevelBoosterType _boosterToBuy;

		private static Dictionary<PrelevelBoosterType, (string title, int count, int coinPrice)> _map = new Dictionary<PrelevelBoosterType, (string title, int count, int coinPrice)>
		{
			[PrelevelBoosterType.Rocket] = ("Rocket", 3, 900)
		};

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			_boosterToBuy = PrelevelBoosterType.Rocket;
			if (openParameters != null && openParameters.TryGetValue("boosterType", out object boosterTypeObj) && boosterTypeObj is PrelevelBoosterType boosterType)
			{
				_boosterToBuy = boosterType;
			}
			if (!_map.TryGetValue(_boosterToBuy, out (string title, int count, int coinPrice) item))
			{
				item = ("Rocket", 3, 900);
			}

			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(Close);
				closeButton.OnClick.AddListener(Close);
			}
			if (buyButton != null)
			{
				buyButton.OnClick.RemoveListener(OnBuyClicked);
				buyButton.OnClick.AddListener(OnBuyClicked);
			}
			if (headerText != null)
			{
				headerText.text = item.title;
			}
			if (boosterCountText != null)
			{
				boosterCountText.text = string.Format("x{0}", item.count);
			}
			if (priceText != null)
			{
				priceText.text = item.coinPrice.ToString();
			}
			if (boosterIcon != null && boosterIconSprites != null)
			{
				int index = (int)_boosterToBuy;
				if (index >= 0 && index < boosterIconSprites.Length)
				{
					boosterIcon.sprite = boosterIconSprites[index];
				}
			}
		}

		private void OnBuyClicked()
		{
			if (!_map.TryGetValue(_boosterToBuy, out (string title, int count, int coinPrice) item))
			{
				return;
			}
			InventoryPayload price = new InventoryPayload(InventoryItemType.Coin, item.coinPrice);
			if (!InventoryHelper.TrySpend(price, InventorySpendSource.PrelevelBoosterBuy))
			{
				return;
			}
			InventoryItemType itemType = PrelevelBoosterHelper.ToInventoryItemType(_boosterToBuy);
			InventoryHelper.AddAmount(new InventoryPayload(itemType, item.count), false, InventoryEarnSource.PrelevelBoosterBuy);
			Close();
		}
	}
}
