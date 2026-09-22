using TMPro;
using UnityEngine;

namespace Shop
{
	public abstract class ShopItemBase : MonoBehaviour
	{
		[SerializeField]
		protected FlowButton purchaseButton;

		[SerializeField]
		protected TextMeshProUGUI priceText;

		private void OnEnable()
		{
			if (purchaseButton != null)
			{
				purchaseButton.Interactable = false;
			}
			gameObject.SetActive(false);
		}

		protected virtual void Prepare()
		{
			SetPriceString(string.Empty);
		}

		public virtual void SetPriceString(string priceString)
		{
			if (priceText != null)
			{
				priceText.text = string.IsNullOrEmpty(priceString) ? "---" : priceString;
			}
		}
	}
}
