using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Popups
{
	public class NoAdsPopup : BasePopup
	{
		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private FlowButton purchaseButton;

		[SerializeField]
		private TextMeshProUGUI priceText;

		private int _lastPreparedFrame = -1;

		private void OnEnable()
		{
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(OnCloseClicked);
				closeButton.OnClick.AddListener(OnCloseClicked);
			}
			if (purchaseButton != null)
			{
				purchaseButton.gameObject.SetActive(false);
			}
			if (Time.frameCount != _lastPreparedFrame)
			{
				Prepare(null);
			}
		}

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			_lastPreparedFrame = Time.frameCount;
			if (priceText != null)
			{
				priceText.text = string.Empty;
			}
			if (purchaseButton != null)
			{
				purchaseButton.gameObject.SetActive(false);
			}
		}

		private void OnCloseClicked()
		{
			Close();
		}

		private void OnDisable()
		{
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(OnCloseClicked);
			}
		}
	}
}
