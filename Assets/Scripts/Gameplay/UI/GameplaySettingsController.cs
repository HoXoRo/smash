using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Level;
using Audio;
using Haptics;
using LocalSave;
using Popups;
using Service;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
	public class GameplaySettingsController : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CCloseAfterOneFrame_003Ed__16 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public GameplaySettingsController _003C_003E4__this;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CCloseAfterOneFrame_003Ed__16(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[SerializeField]
		private FlowButton settingsButton;

		[SerializeField]
		private GameObject tint;

		[SerializeField]
		private OnOffButton musicButton;

		[SerializeField]
		private OnOffButton sfxButton;

		[SerializeField]
		private OnOffButton hapticButton;

		[SerializeField]
		private FlowButton exitButton;

		[SerializeField]
		private FlowButton tintButton;

		[SerializeField]
		private Image settingsButtonBGImage;

		[SerializeField]
		private Sprite[] settingsButtonBGSprites;

		private bool _open;

		private Coroutine _closeAfterOneFrameRoutine;

		public void Init(LevelData levelData)
		{
			SetVisible(false);
			_open = false;
			SetStates();
			SetCallbacks();

			if (settingsButtonBGImage != null && settingsButtonBGSprites != null && settingsButtonBGSprites.Length > 0)
			{
				int spriteIndex = levelData != null && levelData.GetStageCount() > 1 && settingsButtonBGSprites.Length > 1 ? 1 : 0;
				settingsButtonBGImage.sprite = settingsButtonBGSprites[spriteIndex];
			}
		}

		private void OnSettingsClick()
		{
			if (ServiceLocator.Get<GameController>()?.IsFinished() ?? false)
			{
				return;
			}
			_open = !_open;
			SetVisible(_open);
		}

		private void SetVisible(bool on)
		{
			if (tint != null)
			{
				tint.SetActive(on);
			}
			if (musicButton != null)
			{
				musicButton.gameObject.SetActive(on);
			}
			if (sfxButton != null)
			{
				sfxButton.gameObject.SetActive(on);
			}
			if (hapticButton != null)
			{
				hapticButton.gameObject.SetActive(on);
			}
			if (exitButton != null)
			{
				exitButton.gameObject.SetActive(on);
			}
		}

		private void SetStates()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			musicButton?.SetState(SaveService.Data.MusicOn);
			sfxButton?.SetState(SaveService.Data.SfxOn);
			hapticButton?.SetState(SaveService.Data.HapticOn);
		}

		private void SetCallbacks()
		{
			if (settingsButton != null)
			{
				settingsButton.OnClick.RemoveListener(OnSettingsClick);
				settingsButton.OnClick.AddListener(OnSettingsClick);
			}
			if (tintButton != null)
			{
				tintButton.OnClick.RemoveListener(OnTintClicked);
				tintButton.OnClick.AddListener(OnTintClicked);
			}
			if (exitButton != null)
			{
				exitButton.OnClick.RemoveListener(OnExitClicked);
				exitButton.OnClick.AddListener(OnExitClicked);
			}
			if (musicButton != null)
			{
				musicButton.OnClick.RemoveListener(OnMusicButtonClicked);
				musicButton.OnClick.AddListener(OnMusicButtonClicked);
			}
			if (sfxButton != null)
			{
				sfxButton.OnClick.RemoveListener(OnSfxButtonClicked);
				sfxButton.OnClick.AddListener(OnSfxButtonClicked);
			}
			if (hapticButton != null)
			{
				hapticButton.OnClick.RemoveListener(OnHapticButtonClicked);
				hapticButton.OnClick.AddListener(OnHapticButtonClicked);
			}
		}

		private void OnTintClicked()
		{
			CancelPendingCloseAfterOneFrame();
			_closeAfterOneFrameRoutine = StartCoroutine(CloseAfterOneFrame());
		}

		[IteratorStateMachine(typeof(_003CCloseAfterOneFrame_003Ed__16))]
		private IEnumerator CloseAfterOneFrame()
		{
			return CloseAfterOneFrameRoutine();
		}

		private IEnumerator CloseAfterOneFrameRoutine()
		{
			yield return null;
			_closeAfterOneFrameRoutine = null;
			_open = false;
			SetVisible(false);
		}

		private void OnExitClicked()
		{
			_open = false;
			SetVisible(false);
			GameController gameController = ServiceLocator.Get<GameController>();
			ServiceLocator.Get<PopupController>()?.Open(PopupType.LevelExit, new Dictionary<string, object>
			{
				["streakCount"] = gameController != null ? gameController.GetActiveStreakCount() : 0
			});
		}

		private void OnMusicButtonClicked()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.MusicOn = !SaveService.Data.MusicOn;
			if (SaveService.Data.MusicOn)
			{
				ServiceLocator.Get<AudioHelper>()?.PlayMusicForCurrentScene();
			}
			else
			{
				ServiceLocator.Get<AudioHelper>()?.StopMusic();
			}
			SaveService.Save();
			musicButton?.SetState(SaveService.Data.MusicOn);
		}

		private void OnSfxButtonClicked()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.SfxOn = !SaveService.Data.SfxOn;
			if (SaveService.Data.SfxOn)
			{
				ServiceLocator.Get<AudioHelper>()?.EnableSfx();
			}
			else
			{
				ServiceLocator.Get<AudioHelper>()?.DisableSfx();
			}
			SaveService.Save();
			sfxButton?.SetState(SaveService.Data.SfxOn);
		}

		private void OnHapticButtonClicked()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.HapticOn = !SaveService.Data.HapticOn;
			if (SaveService.Data.HapticOn)
			{
				HapticManager.Enable();
			}
			else
			{
				HapticManager.Disable();
			}
			SaveService.Save();
			hapticButton?.SetState(SaveService.Data.HapticOn);
		}

		private void OnDisable()
		{
			CancelPendingCloseAfterOneFrame();
		}

		private void OnDestroy()
		{
			CancelPendingCloseAfterOneFrame();
		}

		private void CancelPendingCloseAfterOneFrame()
		{
			if (_closeAfterOneFrameRoutine == null)
			{
				return;
			}
			StopCoroutine(_closeAfterOneFrameRoutine);
			_closeAfterOneFrameRoutine = null;
		}
	}
}
