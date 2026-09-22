using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Gameplay.UI
{
	public class SlidingText : MonoBehaviour
	{
		private Sequence _playSequence;

		private Action _onComplete;

		private bool _completionNotified;

		[SerializeField]
		private RectTransform rectTransform;

		[SerializeField]
		private TextMeshProUGUI text;

		public void Play(float startY, string textStr, Action onComplete)
		{
			_onComplete = onComplete;
			_completionNotified = false;
			if (rectTransform == null || text == null)
			{
				NotifyComplete();
				Destroy(gameObject);
				return;
			}

			KillPlaySequence();

			text.text = textStr;
			Color color = text.color;
			color.a = 1f;
			text.color = color;

			float startAnchoredY = startY <= 1f ? Screen.height * startY : startY;
			rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startAnchoredY);

			const float EndYOffset = 120f;
			const float Duration = 0.75f;
			_playSequence = DOTween.Sequence();
			_playSequence.Join(rectTransform.DOAnchorPosY(startAnchoredY + EndYOffset, Duration));
			_playSequence.Join(text.DOFade(0f, Duration));
			_playSequence.OnComplete(delegate
			{
				_playSequence = null;
				NotifyComplete();
				Destroy(gameObject);
			});
		}

		private void OnDisable()
		{
			KillPlaySequence();
			NotifyComplete();
		}

		private void OnDestroy()
		{
			KillPlaySequence();
			NotifyComplete();
		}

		private void KillPlaySequence()
		{
			if (_playSequence != null)
			{
				_playSequence.Kill();
				_playSequence = null;
			}
		}

		private void NotifyComplete()
		{
			if (_completionNotified)
			{
				return;
			}
			_completionNotified = true;
			_onComplete?.Invoke();
			_onComplete = null;
		}
	}
}
