using System.Collections.Generic;
using Gameplay.Level;
using Inventory.TimedInventory;
using Life;
using LocalSave;
using Popups;
using Scene;
using Service;
using TMPro;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Menu.Page
{
	public class HomePage : BasePage
	{
		[SerializeField]
		private List<FlowButton> playButtons;

		[SerializeField]
		private List<TextMeshProUGUI> levelTexts;

		[SerializeField]
		private FlowButton noAdsButton;

		private static int _lastAnyPlayClickFrame = -1;

		private static bool _isHandlingPlayClick;

		private static float _playClickLockedUntilRealtime = -1f;

		private int _lastPlayClickFrame = -1;

		private bool _playButtonsPendingRestore;

		private void Start()
		{
			OnNoAdsUpdated();
		}

		public override void Prepare()
		{
			_isHandlingPlayClick = false;
			_playClickLockedUntilRealtime = -1f;
			_playButtonsPendingRestore = false;
			int level = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
			if (levelTexts != null)
			{
				levelTexts.ForEach(delegate(TextMeshProUGUI x)
				{
					if (x != null)
					{
						x.text = string.Format("Level {0}", level);
					}
				});
			}

			if (playButtons == null || playButtons.Count == 0)
			{
				return;
			}

			playButtons.ForEach(delegate(FlowButton x)
			{
				if (x != null)
				{
					x.gameObject.SetActive(false);
					x.Interactable = true;
					x.OnClick.RemoveListener(OnPlayClicked);
					x.OnClick.AddListener(OnPlayClicked);
				}
			});

			int buttonIndex = 0;
			Level.LevelData levelData = LevelLoader.GetLevel(level);
			if (levelData != null)
			{
				buttonIndex = Mathf.Clamp((int)levelData.difficulty, 0, playButtons.Count - 1);
			}
			FlowButton selectedButton = playButtons[buttonIndex];
			if (selectedButton != null)
			{
				selectedButton.gameObject.SetActive(true);
			}
			OnNoAdsUpdated();
		}

		private void OnPlayClicked()
		{
			if (_isHandlingPlayClick || Time.unscaledTime < _playClickLockedUntilRealtime || _lastPlayClickFrame == Time.frameCount || _lastAnyPlayClickFrame == Time.frameCount)
			{
				return;
			}

			if (!CanHandlePlayClick())
			{
				return;
			}

			_isHandlingPlayClick = true;
			_playClickLockedUntilRealtime = Time.unscaledTime + 0.75f;
			_lastPlayClickFrame = Time.frameCount;
			_lastAnyPlayClickFrame = Time.frameCount;
			_playButtonsPendingRestore = true;
			SetPlayButtonsInteractable(false);

			try
			{
				bool hasUnlimitedLife = TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife);
				LifeHelper lifeHelper = ServiceLocator.Get<LifeHelper>();
				if (!hasUnlimitedLife && lifeHelper != null && lifeHelper.GetCurrentLifeCount() < 1)
				{
					ServiceLocator.Get<PopupController>()?.Open(PopupType.MoreLives);
				}
				else
				{
					ServiceLocator.Get<PopupController>()?.Open(PopupType.Prelevel);
				}
			}
			finally
			{
				_isHandlingPlayClick = false;
				TryRestorePlayButtons(immediate: true);
			}
		}

		private void Update()
		{
			TryRestorePlayButtons(immediate: false);
		}

		private void TryRestorePlayButtons(bool immediate)
		{
			if (!_playButtonsPendingRestore || _isHandlingPlayClick || !isActiveAndEnabled || !gameObject.activeInHierarchy)
			{
				return;
			}

			if (!immediate && Time.unscaledTime < _playClickLockedUntilRealtime)
			{
				return;
			}

			PopupController popupController = ServiceLocator.Get<PopupController>();
			if (popupController != null && popupController.AreThereOpenPopups())
			{
				return;
			}

			_playButtonsPendingRestore = false;
			SetPlayButtonsInteractable(true);
		}

		private bool CanHandlePlayClick()
		{
			if (!isActiveAndEnabled || !gameObject.activeInHierarchy || SceneHandler.GetCurrentScene() != SceneType.Menu || SceneHandler.IsInLoading)
			{
				return false;
			}

			PopupController popupController = ServiceLocator.Get<PopupController>();
			if (popupController != null && popupController.AreThereOpenPopups())
			{
				return false;
			}

			UnityEngine.SceneManagement.Scene activeScene = UnitySceneManager.GetActiveScene();
			if (!activeScene.IsValid() || activeScene.name != SceneType.Menu.ToString())
			{
				return false;
			}

			return gameObject.scene.IsValid() && gameObject.scene.name == SceneType.Menu.ToString();
		}

		private void SetPlayButtonsInteractable(bool value)
		{
			if (playButtons == null)
			{
				return;
			}

			for (int i = 0; i < playButtons.Count; i++)
			{
				if (playButtons[i] != null)
				{
					playButtons[i].Interactable = value;
				}
			}
		}

		private void OnNoAdsUpdated()
		{
			if (noAdsButton != null)
			{
				noAdsButton.gameObject.SetActive(false);
			}
		}

		private void OnDestroy()
		{
			if (playButtons != null)
			{
				playButtons.ForEach(delegate(FlowButton x)
				{
					if (x != null)
					{
						x.OnClick.RemoveListener(OnPlayClicked);
					}
				});
			}
		}
	}
}
