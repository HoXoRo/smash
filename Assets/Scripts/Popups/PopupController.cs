using System.Collections.Generic;
using Service;
using UnityEngine;
using UnityEngine.UI;

namespace Popups
{
	public class PopupController : ServiceMonoBehaviour
	{
		[SerializeField]
		private GameObject tint;

		[SerializeField]
		private BasePopup winPopup;

		[SerializeField]
		private BasePopup egoPopup;

		[SerializeField]
		private BasePopup moreLivesPopup;

		[SerializeField]
		private BasePopup levelExitPopup;

		[SerializeField]
		private BasePopup prelevelPopup;

		[SerializeField]
		private BasePopup prelevelBoosterBuyPopup;

		private readonly Stack<(PopupType type, BasePopup popup)> _openPopups = new Stack<(PopupType type, BasePopup popup)>();

		private const int CloseFrameBuffer = 1;

		private static bool _isAnyPopupOpening;

		private int _lastCloseFrame;
		private bool _isOpeningPopup;

		private Image _tintImage;

		private void OnEnable()
		{
			_openPopups.Clear();
			_isOpeningPopup = false;
			_isAnyPopupOpening = false;

			if (tint != null)
			{
				_tintImage = tint.GetComponent<Image>();
				tint.SetActive(false);
			}

			foreach (BasePopup popup in GetConfiguredPopups())
			{
				if (popup != null && popup.gameObject.activeSelf)
				{
					Hide(popup);
				}
			}
		}

		private float GetTintAlpha(PopupType type)
		{
			return type == PopupType.Prelevel || type == PopupType.PrelevelBoosterBuy ? 0.85f : 0.65f;
		}

		public void Open(PopupType type, Dictionary<string, object> openParameters = null)
		{
			PruneInactivePopups();

			if (_isOpeningPopup || _isAnyPopupOpening)
			{
				Debug.LogWarning("Ignoring re-entrant popup open: " + type);
				return;
			}

			BasePopup popup = GetPopup(type);
			if (popup == null)
			{
				Debug.LogWarning("Popup is not configured: " + type);
				return;
			}
			if (_openPopups.Count > 0 && _openPopups.Peek().popup == popup && popup.gameObject.activeSelf)
			{
				return;
			}

			_isOpeningPopup = true;
			_isAnyPopupOpening = true;
			try
			{
				if (_openPopups.Count > 0)
				{
					Hide(_openPopups.Peek().popup);
				}

				_openPopups.Push((type, popup));
				Show((type, popup), openParameters, true);
			}
			finally
			{
				_isOpeningPopup = false;
				_isAnyPopupOpening = false;
			}
		}

		public void Close(BasePopup popup)
		{
			PruneInactivePopups();

			while (_openPopups.Count > 0)
			{
				(PopupType type, BasePopup popup) item = _openPopups.Pop();
				Hide(item.popup);
				if (item.popup == popup)
				{
					break;
				}
			}

			if (_openPopups.Count > 0)
			{
				Show(_openPopups.Peek(), null, false);
			}
			else
			{
				if (tint != null)
				{
					tint.SetActive(false);
				}
				_lastCloseFrame = Time.frameCount;
			}
		}

		private void Show((PopupType type, BasePopup popup) item)
		{
			Show(item, null, false);
		}

		private void Show((PopupType type, BasePopup popup) item, Dictionary<string, object> openParameters, bool prepare)
		{
			if (tint != null)
			{
				tint.SetActive(true);
			}
			if (_tintImage != null)
			{
				Color color = _tintImage.color;
				color.a = GetTintAlpha(item.type);
				_tintImage.color = color;
			}
			if (prepare)
			{
				item.popup.Prepare(openParameters ?? new Dictionary<string, object>());
			}
			item.popup.Open();
			if (item.popup != null && item.popup.gameObject.activeInHierarchy)
			{
				item.popup.PlayPopupOpenAnimation();
			}
		}

		private void Hide(BasePopup popup)
		{
			if (popup == null)
			{
				return;
			}
			popup.KillPopupOpenAnimation();
			popup.gameObject.SetActive(false);
		}

		public bool AreThereOpenPopups()
		{
			if (_isOpeningPopup || _isAnyPopupOpening)
			{
				return true;
			}

			PruneInactivePopups();

			if (_openPopups.Count > 0 || Time.frameCount - _lastCloseFrame < CloseFrameBuffer + 1)
			{
				return true;
			}
			return SRDebug.Instance != null && SRDebug.Instance.IsDebugPanelVisible;
		}

		private void PruneInactivePopups()
		{
			if (_isOpeningPopup || _isAnyPopupOpening)
			{
				return;
			}

			while (_openPopups.Count > 0)
			{
				(PopupType type, BasePopup popup) item = _openPopups.Peek();
				if (item.popup != null && item.popup.gameObject.activeSelf)
				{
					return;
				}
				_openPopups.Pop();
			}
		}

		private BasePopup GetPopup(PopupType type)
		{
			return type switch
			{
				PopupType.Win => winPopup,
				PopupType.Ego => egoPopup,
				PopupType.MoreLives => moreLivesPopup,
				PopupType.LevelExit => levelExitPopup,
				PopupType.Prelevel => prelevelPopup,
				PopupType.PrelevelBoosterBuy => prelevelBoosterBuyPopup,
				_ => null
			};
		}

		private IEnumerable<BasePopup> GetConfiguredPopups()
		{
			yield return winPopup;
			yield return egoPopup;
			yield return moreLivesPopup;
			yield return levelExitPopup;
			yield return prelevelPopup;
			yield return prelevelBoosterBuyPopup;
		}
	}
}
