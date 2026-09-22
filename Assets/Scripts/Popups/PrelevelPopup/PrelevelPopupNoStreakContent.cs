using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Menu;
using Service;
using TMPro;
using UnityEngine;

namespace Popups.PrelevelPopup
{
	public class PrelevelPopupNoStreakContent : PrelevelPopupContentBase
	{
		[SerializeField]
		private TextMeshProUGUI headerText;

		[SerializeField]
		private FlowButton playButton;

		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private List<PrelevelBoosterSelectButton> boosterButtons;

		[SerializeField]
		private TextMeshProUGUI playButtonText;

		[SerializeField]
		private GameObject selectBoosterText;

		private PrelevelPopup _masterPopup;

		public override void Prepare(PrelevelPopup popup, int level, PrelevelPopupType type, int currentStreak, int maxStreak)
		{
			_masterPopup = popup;
			if (headerText != null)
			{
				headerText.text = string.Format("Level {0}", level);
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
			if (selectBoosterText != null)
			{
				selectBoosterText.SetActive(level >= PrelevelBoosterHelper.GetUnlockLevelForType(PrelevelBoosterType.Rocket));
			}
			TryPlayRocketTutorial(level);
		}

		private void PlayClicked()
		{
			_masterPopup?.PlayClicked();
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

		private void TryPlayRocketTutorial(int level)
		{
			if (!PrelevelBoosterHelper.ShouldShowPrelevelTutorial(PrelevelBoosterType.Rocket, level) || boosterButtons == null)
			{
				return;
			}
			PrelevelBoosterSelectButton rocketButton = boosterButtons.FirstOrDefault((PrelevelBoosterSelectButton x) => x != null && x.type == PrelevelBoosterType.Rocket);
			RectTransform highlight = rocketButton != null ? rocketButton.GetComponent<RectTransform>() : null;
			UITutorialController tutorialController = ServiceLocator.Get<UITutorialController>();
			if (tutorialController == null || rocketButton == null || rocketButton.button == null || highlight == null)
			{
				return;
			}
			tutorialController.PlayPrelevelBoosterRocketTutorial(highlight, rocketButton.button);
			PrelevelBoosterHelper.MarkPrelevelTutorialDone(PrelevelBoosterType.Rocket);
		}
	}
}
