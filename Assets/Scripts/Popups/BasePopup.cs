using System.Collections.Generic;
using DG.Tweening;
using Service;
using UnityEngine;

namespace Popups
{
	public abstract class BasePopup : MonoBehaviour
	{
		private Sequence _activePopupOpenSequence;
		private bool _isOpening;

		public abstract void Prepare(Dictionary<string, object> openParameters);

		public virtual void Open()
		{
			if (_isOpening)
			{
				return;
			}

			_isOpening = true;
			try
			{
				if (!gameObject.activeSelf)
				{
					gameObject.SetActive(true);
				}
			}
			finally
			{
				_isOpening = false;
			}
		}

		public virtual void Close()
		{
			ServiceLocator.Get<PopupController>()?.Close(this);
		}

		public virtual void PlayPopupOpenAnimation()
		{
			KillPopupOpenAnimation();
			transform.localScale = Vector3.one * 0.8f;
			_activePopupOpenSequence = DOTween.Sequence();
			_activePopupOpenSequence.Append(transform.DOScale(Vector3.one * 1.05f, 0.16f).SetEase(Ease.OutCubic));
			_activePopupOpenSequence.Append(transform.DOScale(Vector3.one, 0.08f).SetEase(Ease.InCubic));
		}

		public void KillPopupOpenAnimation()
		{
			if (_activePopupOpenSequence != null)
			{
				_activePopupOpenSequence.Kill();
				_activePopupOpenSequence = null;
			}
		}

		protected virtual void OnDisable()
		{
			KillPopupOpenAnimation();
		}

		protected virtual void OnDestroy()
		{
			KillPopupOpenAnimation();
		}
	}
}
