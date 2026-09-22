using System;
using System.Collections;
using DG.Tweening;
using Service;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Menu
{
	public class UITutorialController : ServiceMonoBehaviour
	{
		private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.85f);

		private const float TintFadeDuration = 0.25f;

		private const float HintAppearDelay = 0.5f;

		private const float ArrowMoveDuration = 0.75f;

		private const float ArrowTargetY = -275f;

		[SerializeField]
		private GameObject tutorialCanvas;

		[SerializeField]
		private Image tutorialTint;

		[SerializeField]
		private FlowButton tintButton;

		[SerializeField]
		private RectTransform highlightParent;

		[SerializeField]
		private RectTransform arrowTransform;

		[SerializeField]
		private RectTransform prelevelBoosterTextPanel;

		private RectTransform _activeHighlightTarget;

		private Transform _activeHighlightParent;

		private int _activeHighlightSiblingIndex;

		private Vector2 _activeArrowStartPosition;

		private Tweener _activeArrowTween;

		private FlowButton _activeButton;

		private UnityAction _activeButtonClickAction;

		private UnityAction _activeTintClickAction;

		public void PlayPrelevelStreakTutorial(RectTransform transformToHighlight, Action onClick)
		{
			CleanupTutorialState();
			ResetOverlayVisuals();
			StartCoroutine(PrelevelStreakTutorialRoutine(transformToHighlight, onClick));
		}

		private IEnumerator PrelevelStreakTutorialRoutine(RectTransform transformToHighlight, Action onClick)
		{
			if (transformToHighlight == null)
			{
				yield break;
			}

			bool clicked = false;
			UnityAction tintClickAction = () => clicked = true;

			RememberHighlightTarget(transformToHighlight);
			MoveHighlightTarget(transformToHighlight);
			ShowOverlay(showBoosterText: false);

			if (tutorialTint != null)
			{
				yield return tutorialTint.DOFade(OverlayColor.a, TintFadeDuration).WaitForCompletion();
			}

			yield return new WaitForSeconds(HintAppearDelay);

			_activeArrowTween = StartArrowTween();
			if (tintButton != null)
			{
				tintButton.OnClick.RemoveListener(tintClickAction);
				tintButton.OnClick.AddListener(tintClickAction);
			}
			_activeTintClickAction = tintClickAction;

			yield return new WaitUntil(() => clicked);

			HideOverlay();
			onClick?.Invoke();
		}

		public void PlayPrelevelBoosterRocketTutorial(RectTransform transformToHighlight, FlowButton button)
		{
			CleanupTutorialState();
			ResetOverlayVisuals();
			StartCoroutine(PrelevelBoosterRocketTutorialRoutine(transformToHighlight, button));
		}

		private IEnumerator PrelevelBoosterRocketTutorialRoutine(RectTransform transformToHighlight, FlowButton button)
		{
			if (transformToHighlight == null || button == null)
			{
				yield break;
			}

			bool clicked = false;
			UnityAction buttonClickAction = () => clicked = true;

			RememberHighlightTarget(transformToHighlight);
			MoveHighlightTarget(transformToHighlight);
			ShowOverlay(showBoosterText: true);

			if (tutorialTint != null)
			{
				yield return tutorialTint.DOFade(OverlayColor.a, TintFadeDuration).WaitForCompletion();
			}

			yield return new WaitForSeconds(HintAppearDelay);

			_activeArrowTween = StartArrowTween();
			button.OnClick.RemoveListener(buttonClickAction);
			button.OnClick.AddListener(buttonClickAction);
			_activeButton = button;
			_activeButtonClickAction = buttonClickAction;

			yield return new WaitUntil(() => clicked);

			HideOverlay();
		}

		private void RememberHighlightTarget(RectTransform transformToHighlight)
		{
			_activeHighlightTarget = transformToHighlight;
			_activeHighlightParent = transformToHighlight != null ? transformToHighlight.parent : null;
			_activeHighlightSiblingIndex = transformToHighlight != null ? transformToHighlight.GetSiblingIndex() : 0;
			_activeArrowStartPosition = arrowTransform != null ? arrowTransform.anchoredPosition : Vector2.zero;
		}

		private void MoveHighlightTarget(RectTransform transformToHighlight)
		{
			if (transformToHighlight == null || highlightParent == null)
			{
				return;
			}
			transformToHighlight.SetParent(highlightParent, true);
			transformToHighlight.SetAsLastSibling();
		}

		private void ShowOverlay(bool showBoosterText)
		{
			if (tutorialCanvas != null)
			{
				tutorialCanvas.SetActive(true);
			}
			if (tutorialTint != null)
			{
				tutorialTint.DOKill();
				tutorialTint.gameObject.SetActive(true);
				tutorialTint.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, 0f);
			}
			if (arrowTransform != null)
			{
				arrowTransform.DOKill();
				arrowTransform.gameObject.SetActive(true);
			}
			if (prelevelBoosterTextPanel != null)
			{
				prelevelBoosterTextPanel.gameObject.SetActive(showBoosterText);
			}
		}

		private Tweener StartArrowTween()
		{
			if (arrowTransform == null)
			{
				return null;
			}
			arrowTransform.DOKill();
			return arrowTransform.DOAnchorPosY(ArrowTargetY, ArrowMoveDuration, false).SetLoops(-1, LoopType.Yoyo);
		}

		private void HideOverlay()
		{
			if (tintButton != null && _activeTintClickAction != null)
			{
				tintButton.OnClick.RemoveListener(_activeTintClickAction);
			}
			if (_activeButton != null && _activeButtonClickAction != null)
			{
				_activeButton.OnClick.RemoveListener(_activeButtonClickAction);
			}
			_activeArrowTween?.Kill(false);
			if (arrowTransform != null)
			{
				arrowTransform.DOKill();
				arrowTransform.anchoredPosition = _activeArrowStartPosition;
				arrowTransform.gameObject.SetActive(false);
			}
			if (tutorialTint != null)
			{
				tutorialTint.DOKill();
				tutorialTint.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, 0f);
				tutorialTint.gameObject.SetActive(false);
			}
			if (prelevelBoosterTextPanel != null)
			{
				prelevelBoosterTextPanel.gameObject.SetActive(false);
			}
			if (tutorialCanvas != null)
			{
				tutorialCanvas.SetActive(false);
			}
			if (_activeHighlightTarget != null && _activeHighlightParent != null)
			{
				_activeHighlightTarget.SetParent(_activeHighlightParent, true);
				_activeHighlightTarget.SetSiblingIndex(Mathf.Clamp(_activeHighlightSiblingIndex, 0, _activeHighlightParent.childCount - 1));
			}
			_activeHighlightTarget = null;
			_activeHighlightParent = null;
			_activeHighlightSiblingIndex = 0;
			_activeArrowTween = null;
			_activeButton = null;
			_activeButtonClickAction = null;
			_activeTintClickAction = null;
		}

		private void ResetOverlayVisuals()
		{
			HideOverlay();
			if (tintButton != null)
			{
				tintButton.OnClickDown.RemoveAllListeners();
				tintButton.OnClickUp.RemoveAllListeners();
			}
			if (tutorialTint != null)
			{
				tutorialTint.DOKill();
				tutorialTint.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, 0f);
				tutorialTint.gameObject.SetActive(false);
			}
			if (arrowTransform != null)
			{
				arrowTransform.DOKill();
				arrowTransform.gameObject.SetActive(false);
			}
			if (prelevelBoosterTextPanel != null)
			{
				prelevelBoosterTextPanel.gameObject.SetActive(false);
			}
			if (tutorialCanvas != null)
			{
				tutorialCanvas.SetActive(false);
			}
		}

		private void OnDisable()
		{
			CleanupTutorialState();
		}

		private void OnDestroy()
		{
			CleanupTutorialState();
		}

		private void CleanupTutorialState()
		{
			StopAllCoroutines();
			HideOverlay();
		}
	}
}
