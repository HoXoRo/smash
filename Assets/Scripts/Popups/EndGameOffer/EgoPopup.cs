using System.Collections.Generic;
using ABTesting;
using Audio;
using DG.Tweening;
using Gameplay;
using IAP;
using Inventory;
using LocalSave;
using Service;
using SRDebugger;
using Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Popups.EndGameOffer
{
	public class EgoPopup : BasePopup
	{
		[SROption]
		public static bool FailOfferEnabled = true;

		[SerializeField]
		private RectTransform contentTransform;

		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private FlowButton[] egoButtons;

		[SerializeField]
		private FlowButton rewardedEgoButton;

		[SerializeField]
		private RectTransform coinPanelTransform;

		[SerializeField]
		private TextMeshProUGUI[] egoPriceTexts;

		[SerializeField]
		private List<RectTransform> contents;

		[SerializeField]
		private Sprite[] streakGiftSprites;

		[SerializeField]
		private Image streakGiftImage;

		[SerializeField]
		private ShopItemBase failOffer;

		[SerializeField]
		private GameObject noRewardedParent;

		[SerializeField]
		private GameObject rewardedParent;

		private int _currentContentIndex;

		private bool _isSlidingContent;

		private int _streakCount;

		private int _lastPreparedFrame = -1;

		private bool _hasCoinPanelTargetPosition;

		private Vector2 _coinPanelTargetPosition;

		private const int EgoPrice = 900;

		private const int EgoMoveRewardCount = 5;

		private const int RewardedEgoMoveRewardCount = 3;

		private void OnEnable()
		{
			BindButtons();
			if (Time.frameCount != _lastPreparedFrame)
			{
				RefreshVisuals();
			}
			PlayCoinPanelAnimation();
		}

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			_lastPreparedFrame = Time.frameCount;
			_streakCount = 0;
			if (openParameters != null && openParameters.TryGetValue("streakCount", out object streakCountObj) && streakCountObj is int streakCount)
			{
				_streakCount = streakCount;
			}

			_currentContentIndex = 0;
			_isSlidingContent = false;

			BindButtons();
			RefreshVisuals();
		}

		private void BindButtons()
		{
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(OnCloseClicked);
				closeButton.OnClick.AddListener(OnCloseClicked);
			}

			foreach (FlowButton button in egoButtons ?? new FlowButton[0])
			{
				if (button != null)
				{
					button.OnClick.RemoveListener(OnEgoClicked);
					button.OnClick.AddListener(OnEgoClicked);
				}
			}

			if (rewardedEgoButton != null)
			{
				rewardedEgoButton.OnClick.RemoveListener(OnRewardedEgoClicked);
				rewardedEgoButton.OnClick.AddListener(OnRewardedEgoClicked);
			}
		}

		private void RefreshVisuals()
		{
			bool showRewardedEgo = ShouldShowRewardedEgo();
			if (rewardedParent != null)
			{
				rewardedParent.SetActive(showRewardedEgo);
			}
			if (noRewardedParent != null)
			{
				noRewardedParent.SetActive(!showRewardedEgo);
			}
			if (rewardedEgoButton != null)
			{
				rewardedEgoButton.gameObject.SetActive(showRewardedEgo);
			}

			foreach (TextMeshProUGUI priceText in egoPriceTexts ?? new TextMeshProUGUI[0])
			{
				if (priceText != null)
				{
					priceText.text = EgoPrice.ToString();
				}
			}

			if (streakGiftImage != null && streakGiftSprites != null && streakGiftSprites.Length > 0)
			{
				int index = Mathf.Clamp(_streakCount, 0, streakGiftSprites.Length - 1);
				streakGiftImage.sprite = streakGiftSprites[index];
			}

			bool showFailOffer = ShouldShowFailOffer();
			if (failOffer != null)
			{
				failOffer.gameObject.SetActive(showFailOffer);
				if (showFailOffer)
				{
					failOffer.Init(IAPSource.FailOffer, OnFailOfferPurchased, null);
				}
			}

			SetContentItemAnchors(0f);
		}

		private void PlayCoinPanelAnimation()
		{
			if (coinPanelTransform == null)
			{
				return;
			}

			coinPanelTransform.DOKill();
			if (!_hasCoinPanelTargetPosition)
			{
				_coinPanelTargetPosition = coinPanelTransform.anchoredPosition;
				_hasCoinPanelTargetPosition = true;
			}

			coinPanelTransform.anchoredPosition = _coinPanelTargetPosition + Vector2.up * 500f;
			coinPanelTransform.DOAnchorPosY(_coinPanelTargetPosition.y, 0.4f).SetEase(Ease.OutBack).SetDelay(0.2f);
		}

		private bool ShouldShowFailOffer()
		{
			return false;
		}

		private bool ShouldShowRewardedEgo()
		{
			return false;
		}

		public override void PlayPopupOpenAnimation()
		{
		}

		private void SetContentItemAnchors(float duration)
		{
			KillContentTweens();
			List<RectTransform> visibleContents = new List<RectTransform>();
			foreach (RectTransform content in contents ?? new List<RectTransform>())
			{
				if (content == null)
				{
					continue;
				}
				EgoContentItem contentItem = content.GetComponent<EgoContentItem>();
				bool shouldShow = contentItem == null || contentItem.ShouldShow();
				content.gameObject.SetActive(shouldShow);
				if (shouldShow)
				{
					visibleContents.Add(content);
				}
			}

			if (visibleContents.Count == 0)
			{
				_isSlidingContent = false;
				return;
			}

			_currentContentIndex = Mathf.Clamp(_currentContentIndex, 0, visibleContents.Count - 1);
			_isSlidingContent = duration > 0f;

			for (int i = 0; i < visibleContents.Count; i++)
			{
				RectTransform content2 = visibleContents[i];
				float offset = i - _currentContentIndex;
				Vector2 anchorMin = new Vector2(offset, 0f);
				Vector2 anchorMax = new Vector2(offset + 1f, 1f);
				if (duration > 0f)
				{
					content2.DOAnchorMin(anchorMin, duration).SetEase(Ease.OutCubic);
					Tweener maxTween = content2.DOAnchorMax(anchorMax, duration).SetEase(Ease.OutCubic);
					if (i == visibleContents.Count - 1)
					{
						maxTween.OnComplete(delegate
						{
							_isSlidingContent = false;
						});
					}
				}
				else
				{
					content2.anchorMin = anchorMin;
					content2.anchorMax = anchorMax;
				}
			}

			if (duration <= 0f)
			{
				_isSlidingContent = false;
			}
		}

		private void OnCloseClicked()
		{
			if (_isSlidingContent)
			{
				return;
			}

			int visibleCount = 0;
			foreach (RectTransform content in contents ?? new List<RectTransform>())
			{
				if (content != null && content.gameObject.activeSelf)
				{
					visibleCount++;
				}
			}

			if (_currentContentIndex + 1 < visibleCount)
			{
				_currentContentIndex++;
				SetContentItemAnchors(0.25f);
				return;
			}

			ServiceLocator.Get<GameController>()?.OnEgoRejected();
			Close();
			ServiceLocator.Get<PopupController>()?.Open(PopupType.Prelevel);
		}

		private void OnEgoClicked()
		{
			if (!InventoryHelper.TrySpend(new InventoryPayload(InventoryItemType.Coin, EgoPrice), InventorySpendSource.EndGameOffer))
			{
				return;
			}

			ServiceLocator.Get<GameController>()?.OnEgoBought(EgoMoveRewardCount);
			ServiceLocator.Get<AudioHelper>()?.PlayMusicForCurrentScene();
			Close();
		}

		private void OnRewardedEgoClicked()
		{
			Debug.Log("[EgoPopup] Rewarded ego is disabled in this recovery pass.");
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			CleanupAnimatedState();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			CleanupAnimatedState();
		}

		private void OnFailOfferPurchased()
		{
			ServiceLocator.Get<GameController>()?.OnEgoBought(EgoMoveRewardCount);
			ServiceLocator.Get<AudioHelper>()?.PlayMusicForCurrentScene();
			Close();
		}

		private void CleanupAnimatedState()
		{
			coinPanelTransform?.DOKill();
			KillContentTweens();
			_isSlidingContent = false;
		}

		private void KillContentTweens()
		{
			foreach (RectTransform content in contents ?? new List<RectTransform>())
			{
				content?.DOKill();
			}
		}
	}
}
