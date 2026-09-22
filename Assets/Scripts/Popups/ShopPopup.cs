using System.Collections.Generic;
using Shop;
using UnityEngine;

namespace Popups
{
	public class ShopPopup : BasePopup
	{
		public const string SourceKey = "source";

		[SerializeField]
		private GameObject basicItemPrefab;

		[SerializeField]
		private Transform contentRoot;

		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private List<ShopSection> sections;

		private int _lastPreparedFrame = -1;

		private void OnEnable()
		{
			BindCloseButton();
			if (Time.frameCount != _lastPreparedFrame)
			{
				RefreshSections();
			}
		}

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			_lastPreparedFrame = Time.frameCount;
			BindCloseButton();
			RefreshSections();
		}

		private void BindCloseButton()
		{
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(OnCloseClick);
				closeButton.OnClick.AddListener(OnCloseClick);
			}
		}

		private void ReprepareWithSameSource()
		{
			RefreshSections();
		}

		private void RefreshSections()
		{
			foreach (ShopSection section in sections ?? new List<ShopSection>())
			{
				if (section != null)
				{
					section.gameObject.SetActive(false);
				}
			}
		}

		public override void PlayPopupOpenAnimation()
		{
		}

		private void OnCloseClick()
		{
			Close();
		}
	}
}
