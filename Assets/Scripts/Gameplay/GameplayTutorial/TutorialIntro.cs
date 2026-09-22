using System;
using System.Collections;
using Gameplay;
using Level;
using LocalSave;
using Service;
using UnityEngine;
using Util;

namespace Gameplay.GameplayTutorial
{
	public class TutorialIntro : TutorialBase
	{
		private static readonly Color TintColor = new Color(0.1f, 0.1f, 0.1f, 0.99215686f);

		private static readonly Vector2 Step1Center = new Vector2(0.5f, 0.5f);

		private const float Step1Radius = 0.065f;

		[SerializeField]
		private GameObject hand;

		[SerializeField]
		private GameObject tapPanel;

		[SerializeField]
		private GameObject ballCountPanel;

		[SerializeField]
		private TutorialTint tint;

		public override void Play(TutorialManager manager, Action<int, int> onStageComplete)
		{
			CleanupTutorialState();
			TutorialProgressData tutorialProgress = SaveService.Data?.TutorialProgress;
			if (tutorialProgress != null)
			{
				tutorialProgress.tutorialIntroDone = true;
				SaveService.Save();
			}

			tint?.BecomeTransparentInstant();
			if (hand != null)
			{
				hand.SetActive(false);
			}
			gameObject.SetActive(true);
			if (tapPanel != null)
			{
				tapPanel.SetActive(false);
			}
			if (ballCountPanel != null)
			{
				ballCountPanel.SetActive(false);
			}

			StartCoroutine(TutorialRoutine(manager));
		}

		public override bool ShouldPlay(LevelData levelData)
		{
			if (levelData == null || levelData.levelIndex != 1)
			{
				return false;
			}

			return !(SaveService.Data?.TutorialProgress?.tutorialIntroDone ?? false);
		}

		public Vector2 GetNormalizedPointOnTint(Vector2 screenPos)
		{
			RectTransform tintRect = tint != null ? tint.GetComponent<RectTransform>() : null;
			if (tintRect == null)
			{
				return Vector2.zero;
			}

			Canvas rootCanvas = tintRect.GetRootCanvas();
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(tintRect, screenPos, rootCanvas != null ? rootCanvas.worldCamera : null, out Vector2 localPoint))
			{
				Rect rect = tintRect.rect;
				return new Vector2((localPoint.x - rect.x) / rect.width, (localPoint.y - rect.y) / rect.height);
			}

			return Vector2.zero;
		}

		private IEnumerator TutorialRoutine(TutorialManager manager)
		{
			bool clickedDownInside = false;
			bool step1Finished = false;
			bool step2Clicked = false;

			if (tint != null)
			{
				tint.SetColor(TintColor);
				tint.Set(0.5f, 0.5f, 0f, 0f, 0f);
			}
			if (tapPanel != null)
			{
				tapPanel.SetActive(true);
			}
			if (hand != null)
			{
				hand.SetActive(true);
			}

			tint?.SetOnClickDown(delegate
			{
				clickedDownInside = IsInsideStep1Circle(Input.mousePosition);
			});
			tint?.SetOnClickUp(delegate
			{
				if (clickedDownInside && IsInsideStep1Circle(Input.mousePosition))
				{
					ServiceLocator.Get<GameController>()?.OnClick();
					step1Finished = true;
				}
			});

			yield return new WaitUntil(() => step1Finished);

			tint?.ClearOnClickDown();
			tint?.ClearOnClickUp();
			if (hand != null)
			{
				hand.SetActive(false);
			}
			if (tapPanel != null)
			{
				tapPanel.SetActive(false);
			}
			if (ballCountPanel != null)
			{
				ballCountPanel.SetActive(true);
			}

			tint?.SetOnClick(delegate
			{
				step2Clicked = true;
			});

			yield return new WaitUntil(() => step2Clicked);

			tint?.ClearOnClick();
			manager?.OnTutorialEnd();
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
			tint?.ClearOnClick();
			tint?.ClearOnClickDown();
			tint?.ClearOnClickUp();
			tint?.BecomeTransparentInstant();
			if (hand != null)
			{
				hand.SetActive(false);
			}
			if (tapPanel != null)
			{
				tapPanel.SetActive(false);
			}
			if (ballCountPanel != null)
			{
				ballCountPanel.SetActive(false);
			}
		}

		private bool IsInsideStep1Circle(Vector2 screenPosition)
		{
			Vector2 normalizedPoint = GetNormalizedPointOnTint(screenPosition);
			return Vector2.Distance(normalizedPoint, Step1Center) < Step1Radius;
		}
	}
}
