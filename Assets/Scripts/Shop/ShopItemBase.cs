using System;
using IAP;
using TMPro;
using UnityEngine;

namespace Shop
{
	public abstract class ShopItemBase : MonoBehaviour
	{
		[SerializeField]
		protected IAPItemType iapType;

		[SerializeField]
		protected FlowButton purchaseButton;

		[SerializeField]
		protected TextMeshProUGUI priceText;

		protected IAPItemData ItemData;

		protected IAPSource Source;

		protected Action OnPurchaseSuccess;

		protected Action OnPurchaseFail;

		private int _lastPreparedFrame = -1;

		private void OnEnable()
		{
			if (purchaseButton != null)
			{
				purchaseButton.Interactable = false;
			}
			gameObject.SetActive(false);
		}

		public virtual void Init(IAPSource source, Action onPurchaseSuccess, Action onPurchaseFail)
		{
			ItemData = IAPLibrary.GetDataByIAPItemType(iapType);
			Source = source;
			OnPurchaseSuccess = onPurchaseSuccess;
			OnPurchaseFail = onPurchaseFail;
			Prepare();
		}

		protected virtual void Prepare()
		{
			if (purchaseButton != null)
			{
				purchaseButton.Interactable = false;
			}
			SetPriceString(string.Empty);
		}

		public virtual void SetPriceString(string priceString)
		{
			if (priceText != null)
			{
				priceText.text = string.IsNullOrEmpty(priceString) ? "---" : priceString;
			}
		}

		protected virtual void Purchase()
		{
			OnPurchaseFail?.Invoke();
		}

		private void OnDisable()
		{
			if (purchaseButton != null)
			{
				purchaseButton.OnClick.RemoveListener(Purchase);
			}
		}
	}
}
