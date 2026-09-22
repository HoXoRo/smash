using System.Collections.Generic;
using Inventory;
using Life;
using LocalSave;
using Service;
using TMPro;
using UnityEngine;

namespace Popups
{
	public class MoreLivesPopup : BasePopup
	{
		[SerializeField]
		private GameObject rewardedContent;

		[SerializeField]
		private GameObject noRewardedContent;

		[SerializeField]
		private FlowButton[] coinRefillButtons;

		[SerializeField]
		private FlowButton[] adRefillButtons;

		[SerializeField]
		private FlowButton[] closeButtons;

		[SerializeField]
		private TextMeshProUGUI[] refillPriceTexts;

		private const int RefillPrice = 900;

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			if (rewardedContent != null)
			{
				rewardedContent.SetActive(false);
			}
			if (noRewardedContent != null)
			{
				noRewardedContent.SetActive(true);
			}
			foreach (FlowButton button in coinRefillButtons ?? new FlowButton[0])
			{
				if (button != null)
				{
					button.OnClick.RemoveListener(CoinRefillClicked);
					button.OnClick.AddListener(CoinRefillClicked);
				}
			}
			foreach (FlowButton button in adRefillButtons ?? new FlowButton[0])
			{
				if (button != null)
				{
					button.gameObject.SetActive(false);
					button.OnClick.RemoveListener(AdRefillClicked);
					button.OnClick.AddListener(AdRefillClicked);
				}
			}
			foreach (FlowButton button in closeButtons ?? new FlowButton[0])
			{
				if (button != null)
				{
					button.OnClick.RemoveListener(CloseClicked);
					button.OnClick.AddListener(CloseClicked);
				}
			}
			foreach (TextMeshProUGUI text in refillPriceTexts ?? new TextMeshProUGUI[0])
			{
				if (text != null)
				{
					text.text = RefillPrice.ToString();
				}
			}
		}

		private void CoinRefillClicked()
		{
			if (!InventoryHelper.TrySpend(new InventoryPayload(InventoryItemType.Coin, RefillPrice), InventorySpendSource.MoreLives))
			{
				return;
			}
			ServiceLocator.Get<LifeHelper>()?.FullLife();
			Close();
		}

		private void AdRefillClicked()
		{
			Debug.Log("[MoreLivesPopup] Rewarded refill is disabled in this recovery pass.");
		}

		private void CloseClicked()
		{
			Close();
		}
	}
}
