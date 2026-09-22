using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Menu;
using Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Popups.PrelevelPopup
{
	public class PrelevelPopupStreakContent : PrelevelPopupContentBase
	{
		[SerializeField]
		private TextMeshProUGUI headerText;

		[SerializeField]
		private FlowButton playButton;

		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private FlowButton infoButton;

		[SerializeField]
		private List<PrelevelBoosterSelectButton> boosterButtons;

		[SerializeField]
		private Sprite[] streakGiftSprites;

		[SerializeField]
		private Image streakGiftImage;

		[SerializeField]
		private TextMeshProUGUI streakStatusText;

		[SerializeField]
		private Image streakStatusFiller;

		[SerializeField]
		private TextMeshProUGUI playButtonText;

		[SerializeField]
		private GameObject lockedStreakPanel;

		[SerializeField]
		private TextMeshProUGUI lockText;

		[SerializeField]
		private RectTransform streakPanel;

		[SerializeField]
		private Animator infoTooltipAnimator;

		private PrelevelPopup _masterPopup;

		private bool _tooltipOpen;

		private int _lastTooltipCloseFrame;

		public override void Prepare(PrelevelPopup popup, int level, PrelevelPopupType type, int currentStreak, int maxStreak)
		{
			_masterPopup = popup;
			_tooltipOpen = false;
			_lastTooltipCloseFrame = 0;
			if (headerText != null)
			{
				headerText.text = string.Format("Level {0}", level);
			}
			if (infoTooltipAnimator != null)
			{
				infoTooltipAnimator.SetBool("Open", false);
				infoTooltipAnimator.Play("TooltipCloseAnim", 0, 0f);
			}
			if (playButtonText != null)
			{
				playButtonText.text = type == PrelevelPopupType.TryAgain ? "Try Again" : "Play";
			}
			if (playButton != null)
			{
				playButton.OnClick.RemoveListener(PlayClicked);
				playButton.OnClick.AddListener(PlayClicked);
			}
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(CloseClicked);
				closeButton.OnClick.AddListener(CloseClicked);
				closeButton.gameObject.SetActive(type == PrelevelPopupType.TryAgain);
			}
			if (infoButton != null)
			{
				infoButton.OnClick.RemoveListener(InfoClicked);
				infoButton.OnClick.AddListener(InfoClicked);
			}
			if (boosterButtons != null)
			{
				foreach (PrelevelBoosterSelectButton button in boosterButtons)
				{
					if (button != null)
					{
						button.Prepare();
					}
				}
			}

			bool unlocked = StreakHelper.IsUnlocked();
			if (lockedStreakPanel != null)
			{
				lockedStreakPanel.SetActive(!unlocked);
			}
			if (lockText != null)
			{
				lockText.text = string.Format("Reach Level {0}\nto unlock", StreakHelper.StreakUnlockLevel);
			}
			if (streakStatusText != null && unlocked)
			{
				streakStatusText.text = string.Format("{0}/{1}", Mathf.Clamp(currentStreak, 0, maxStreak), maxStreak);
			}
			if (streakStatusFiller != null)
			{
				streakStatusFiller.fillAmount = unlocked && maxStreak > 0 ? Mathf.Clamp01((float)currentStreak / maxStreak) : 0f;
			}
			if (streakGiftImage != null && streakGiftSprites != null && streakGiftSprites.Length > 0)
			{
				int index = Mathf.Clamp(currentStreak, 0, streakGiftSprites.Length - 1);
				streakGiftImage.sprite = streakGiftSprites[index];
			}
			TryPlayTutorial(level);
		}

		private void Update()
		{
			if (!_tooltipOpen || Time.frameCount == _lastTooltipCloseFrame)
			{
				return;
			}
			if (Input.GetMouseButtonDown(0))
			{
				_tooltipOpen = false;
				_lastTooltipCloseFrame = Time.frameCount;
				if (infoTooltipAnimator != null)
				{
					infoTooltipAnimator.SetBool("Open", false);
				}
			}
		}

		private void PlayClicked()
		{
			_masterPopup?.PlayClicked();
		}

		private void InfoClicked()
		{
			_tooltipOpen = !_tooltipOpen;
			_lastTooltipCloseFrame = Time.frameCount;
			if (infoTooltipAnimator != null)
			{
				infoTooltipAnimator.SetBool("Open", _tooltipOpen);
			}
		}

		private void CloseClicked()
		{
			_masterPopup?.CloseClicked();
		}

		public override List<PrelevelBoosterType> GetSelectedBoosters()
		{
			if (boosterButtons == null)
			{
				return new List<PrelevelBoosterType>();
			}
			return boosterButtons.Where((PrelevelBoosterSelectButton x) => x != null && x.GetSelected()).Select((PrelevelBoosterSelectButton x) => x.type).ToList();
		}

		private void TryPlayTutorial(int level)
		{
			UITutorialController tutorialController = ServiceLocator.Get<UITutorialController>();
			if (tutorialController == null)
			{
				return;
			}
			if (StreakHelper.ShouldShowPrelevelTutorial(level))
			{
				if (streakPanel != null)
				{
					tutorialController.PlayPrelevelStreakTutorial(streakPanel, InfoClicked);
					StreakHelper.MarkPrelevelTutorialDone();
				}
				return;
			}
			if (!PrelevelBoosterHelper.ShouldShowPrelevelTutorial(PrelevelBoosterType.Rocket, level) || boosterButtons == null)
			{
				return;
			}
			PrelevelBoosterSelectButton rocketButton = boosterButtons.FirstOrDefault((PrelevelBoosterSelectButton x) => x != null && x.type == PrelevelBoosterType.Rocket);
			RectTransform highlight = rocketButton != null ? rocketButton.GetComponent<RectTransform>() : null;
			if (rocketButton == null || rocketButton.button == null || highlight == null)
			{
				return;
			}
			tutorialController.PlayPrelevelBoosterRocketTutorial(highlight, rocketButton.button);
			PrelevelBoosterHelper.MarkPrelevelTutorialDone(PrelevelBoosterType.Rocket);
		}
	}
}
