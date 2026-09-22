using System.Collections.Generic;
using Gameplay;
using LocalSave;
using Scene;
using Service;
using UnityEngine;
using UnityEngine.UI;

namespace Popups
{
	public class ExitPopup : BasePopup
	{
		[SerializeField]
		private FlowButton closeButton;

		[SerializeField]
		private FlowButton quitButton;

		[SerializeField]
		private GameObject[] steps;

		[SerializeField]
		private Sprite[] streakGiftSprites;

		[SerializeField]
		private Image streakGiftImage;

		private int _currentStep;

		private int _streakCount;

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			_streakCount = 0;
			if (openParameters != null && openParameters.TryGetValue("streakCount", out object streakCountObj) && streakCountObj is int streakCount)
			{
				_streakCount = streakCount;
			}
			_currentStep = 0;
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(OnCloseClick);
				closeButton.OnClick.AddListener(OnCloseClick);
			}
			if (quitButton != null)
			{
				quitButton.OnClick.RemoveListener(OnQuitClick);
				quitButton.OnClick.AddListener(OnQuitClick);
			}
			UpdateVisuals();
		}

		private void OnCloseClick()
		{
			Close();
		}

		private void OnQuitClick()
		{
			if (steps != null && _currentStep + 1 < steps.Length && ShouldShowStep(_currentStep + 1))
			{
				_currentStep++;
				UpdateVisuals();
				return;
			}
			ServiceLocator.Get<GameController>()?.OnGameQuit();
			SceneHandler.LoadScene(SceneType.Menu, false);
		}

		private bool ShouldShowStep(int step)
		{
			return steps != null && step >= 0 && step < steps.Length && (step == 0 || _streakCount > 0);
		}

		private void UpdateVisuals()
		{
			if (steps != null)
			{
				for (int i = 0; i < steps.Length; i++)
				{
					if (steps[i] != null)
					{
						steps[i].SetActive(ShouldShowStep(i) && i == _currentStep);
					}
				}
			}
			if (streakGiftImage != null && streakGiftSprites != null && streakGiftSprites.Length > 0)
			{
				int index = Mathf.Clamp(_streakCount, 0, streakGiftSprites.Length - 1);
				streakGiftImage.sprite = streakGiftSprites[index];
			}
		}
	}
}
