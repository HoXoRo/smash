using System;
using Audio;
using Core;
using Haptics;
using LocalSave;
using Service;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Page
{
	public class SettingsPage : BasePage
	{
		[SerializeField]
		private OnOffButton musicButton;

		[SerializeField]
		private OnOffButton sfxButton;

		[SerializeField]
		private OnOffButton hapticButton;

		[SerializeField]
		private FlowButton supportButton;

		[SerializeField]
		private FlowButton termsButton;

		[SerializeField]
		private FlowButton privacyButton;

		[SerializeField]
		private Button debugButton;

		[SerializeField]
		private TextMeshProUGUI debugText;

		private int _debugTapCount;

		private float _debugFirstTapTime;

		private const float DebugTapWindow = 0.6f;

		private const string PrivacyUrl = "https://www.flowgames.net/privacy";

		private const string TermsUrl = "https://www.flowgames.net/termsofservice";

		private const string ContactMail = "contact@flowgames.net";

		private static string ContactSubject => "Smash Fest Support Request: " + UserHelper.GetUserId();

		public override void Prepare()
		{
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
			if (supportButton != null)
			{
				supportButton.OnClick.RemoveListener(OnSupportClicked);
				supportButton.OnClick.AddListener(OnSupportClicked);
			}
			if (termsButton != null)
			{
				termsButton.OnClick.RemoveListener(OnTermsClicked);
				termsButton.OnClick.AddListener(OnTermsClicked);
			}
			if (privacyButton != null)
			{
				privacyButton.OnClick.RemoveListener(OnPrivacyClicked);
				privacyButton.OnClick.AddListener(OnPrivacyClicked);
			}
			if (debugButton != null)
			{
				debugButton.onClick.RemoveListener(OnDebugTap);
				debugButton.onClick.AddListener(OnDebugTap);
			}
			if (SaveService.Data != null)
			{
				musicButton?.SetState(SaveService.Data.MusicOn);
				sfxButton?.SetState(SaveService.Data.SfxOn);
				hapticButton?.SetState(SaveService.Data.HapticOn);
			}
			if (debugText != null)
			{
				debugText.text = UserHelper.GetUserId() + " / " + VersionHelper.GetVersion();
			}
		}

		private void OnMusicButtonClicked()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.MusicOn = !SaveService.Data.MusicOn;
			AudioHelper audioHelper = ServiceLocator.Get<AudioHelper>();
			if (SaveService.Data.MusicOn)
			{
				audioHelper?.PlayMusicForCurrentScene();
			}
			else
			{
				audioHelper?.StopMusic();
			}
			musicButton?.SetState(SaveService.Data.MusicOn);
			SaveService.Save();
		}

		private void OnSfxButtonClicked()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			AudioHelper audioHelper = ServiceLocator.Get<AudioHelper>();
			if (SaveService.Data.SfxOn)
			{
				SaveService.Data.SfxOn = false;
				audioHelper?.DisableSfx();
			}
			else
			{
				SaveService.Data.SfxOn = true;
				audioHelper?.EnableSfx();
			}
			sfxButton?.SetState(SaveService.Data.SfxOn);
			SaveService.Save();
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
			hapticButton?.SetState(SaveService.Data.HapticOn);
			SaveService.Save();
		}

		private void OnSupportClicked()
		{
			OpenMail(ContactMail, ContactSubject);
		}

		private void OnPrivacyClicked()
		{
			Application.OpenURL(PrivacyUrl);
		}

		private void OnTermsClicked()
		{
			Application.OpenURL(TermsUrl);
		}

		private void OpenMail(string address, string subject = "", string body = "")
		{
			string url = "mailto:" + address + "?subject=" + Uri.EscapeDataString(subject ?? string.Empty) + "&body=" + Uri.EscapeDataString(body ?? string.Empty);
			Application.OpenURL(url);
		}

		private void OnDebugTap()
		{
			float now = Time.unscaledTime;
			if (_debugTapCount <= 0 || now - _debugFirstTapTime > DebugTapWindow)
			{
				_debugFirstTapTime = now;
				_debugTapCount = 1;
				return;
			}
			_debugTapCount++;
			if (_debugTapCount >= 7)
			{
				_debugTapCount = 0;
				OnMultiTapTriggered();
			}
		}

		private void OnMultiTapTriggered()
		{
			if (SRDebug.Instance == null)
			{
				return;
			}
			if (SRDebug.Instance.IsDebugPanelVisible)
			{
				SRDebug.Instance.HideDebugPanel();
			}
			else
			{
				SRDebug.Instance.ShowDebugPanel(SRDebugger.DefaultTabs.Options, false);
			}
		}

		private void OnDestroy()
		{
			musicButton?.OnClick.RemoveListener(OnMusicButtonClicked);
			sfxButton?.OnClick.RemoveListener(OnSfxButtonClicked);
			hapticButton?.OnClick.RemoveListener(OnHapticButtonClicked);
			supportButton?.OnClick.RemoveListener(OnSupportClicked);
			termsButton?.OnClick.RemoveListener(OnTermsClicked);
			privacyButton?.OnClick.RemoveListener(OnPrivacyClicked);
			debugButton?.onClick.RemoveListener(OnDebugTap);
		}
	}
}
