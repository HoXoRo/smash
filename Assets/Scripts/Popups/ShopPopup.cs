using System.Collections.Generic;
using System.Linq;
using IAP;
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

		private IAPSource _source;

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
			_source = IAPSource.ShopPage;
			if (openParameters != null && openParameters.TryGetValue(SourceKey, out object sourceObj) && sourceObj is IAPSource source)
			{
				_source = source;
			}

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
			List<ShopSection> visibleSections = sections?.Where(x => x != null && x.ShouldShowSection()).ToList() ?? new List<ShopSection>();
			foreach (ShopSection section in sections ?? new List<ShopSection>())
			{
				if (section != null)
				{
					section.gameObject.SetActive(visibleSections.Contains(section));
				}
			}
			foreach (ShopSection section in visibleSections)
			{
				section.Init(_source, ReprepareWithSameSource);
				section.SetHeaderActive(visibleSections.Count > 1);
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
