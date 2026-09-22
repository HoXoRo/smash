using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Level;
using LocalSave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Gameplay.GameplayTutorial.GenericItemTutorial
{
	public class GenericItemTutorial : TutorialBase
	{
		private Sequence _activeIntroSequence;

		private GenericItemTutorialDataItem _activeItemData;

		private static readonly Color TintColor = new Color(0.1f, 0.1f, 0.1f, 0.99215686f);

		private static readonly Vector3 TinyScale = Vector3.one * 0.0001f;

		private const float TintCenterX = 0.5f;

		private const float TintCenterY = 0.5f;

		private const float ClickEnableDelay = 1f;

		[SerializeField]
		private TutorialTint tint;

		[SerializeField]
		private TextMeshProUGUI itemTitle;

		[SerializeField]
		private Image itemIcon;

		[SerializeField]
		private TextMeshProUGUI descriptionText;

		[SerializeField]
		private Transform newItemTitleEffects;

		[SerializeField]
		private AnimationCurve newItemTitleScaleCurve;

		[SerializeField]
		private float newItemTitleScaleTime;

		[SerializeField]
		private Transform titleEffects;

		[SerializeField]
		private AnimationCurve titleScaleCurve;

		[SerializeField]
		private float titleScaleTime;

		[SerializeField]
		private Transform descriptionEffects;

		[SerializeField]
		private AnimationCurve descriptionScaleCurve;

		[SerializeField]
		private float descriptionScaleTime;

		[SerializeField]
		private Transform continueEffects;

		[SerializeField]
		private AnimationCurve continueScaleCurve;

		[SerializeField]
		private float continueScaleTime;

		[SerializeField]
		private AnimationCurve iconScaleCurve;

		[SerializeField]
		private float iconScaleTime;

		[SerializeField]
		private float iconScaleDuration;

		[SerializeField]
		private ParticleSystem[] iconParticles;

		private GenericItemTutorialType _nextTypeToPlay;

		public override void Play(TutorialManager manager, Action<int, int> onStageComplete)
		{
			try
			{
				CleanupActiveEffects();
				GenericItemTutorialType typeToPlay = _nextTypeToPlay;
				_nextTypeToPlay = GenericItemTutorialType.None;

				GenericItemTutorialData tutorialData = Resources.Load<GenericItemTutorialData>("GenericItemTutorialData");
				GenericItemTutorialDataItem selectedItem = null;
				if (tutorialData?.items != null)
				{
					for (int i = 0; i < tutorialData.items.Count; i++)
					{
						GenericItemTutorialDataItem item = tutorialData.items[i];
						if (item != null && item.type == typeToPlay)
						{
							selectedItem = item;
							break;
						}
					}
				}

				if (selectedItem == null)
				{
					manager?.OnTutorialEnd();
					return;
				}

				_activeItemData = selectedItem;
				tint?.BecomeTransparentInstant();
				gameObject.SetActive(true);
				ApplyTutorialContent(selectedItem);
				StartCoroutine(RefreshTutorialContentNextFrame());
				TryMarkTutorialCompleted(typeToPlay);
				StartCoroutine(TutorialRoutine(manager));
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);
				if (_activeItemData != null)
				{
					gameObject.SetActive(true);
					ApplyTutorialContent(_activeItemData);
					RevealTutorialInstant();
				}
				tint?.SetOnClick(() => manager?.OnTutorialEnd());
			}
		}

		public override bool ShouldPlay(LevelData levelData)
		{
			if (levelData == null)
			{
				return false;
			}

			List<Gameplay.Objects.ObjectType> distinctObjectTypes = levelData.GetDistinctObjectTypes();
			if (distinctObjectTypes == null)
			{
				return false;
			}

			List<GenericItemTutorialType> completedTutorials = SaveService.Data?.TutorialProgress?.genericItemTutorialsDone;
			for (int i = 0; i < distinctObjectTypes.Count; i++)
			{
				GenericItemTutorialType tutorialType = GenericItemTutorialMap.GetTutorialTypeToShow(distinctObjectTypes[i]);
				if (tutorialType == GenericItemTutorialType.None)
				{
					continue;
				}
				if (completedTutorials != null && completedTutorials.Contains(tutorialType))
				{
					continue;
				}
				_nextTypeToPlay = tutorialType;
				return true;
			}

			return false;
		}

		private IEnumerator TutorialRoutine(TutorialManager manager)
		{
			bool step1Clicked = false;

			bool introPrepared = TryPrepareIntroState();
			bool introSequenceStarted = false;
			if (introPrepared)
			{
				float iconStartTime = newItemTitleScaleTime * 0.5f + titleScaleTime * 0.5f;
				float descriptionStartTime = iconStartTime + iconScaleDuration * 0.5f;
				introSequenceStarted = TryStartIntroSequence(iconStartTime, descriptionStartTime);
			}
			if (!introPrepared || !introSequenceStarted)
			{
				RevealTutorialInstant();
			}

			yield return new WaitForSecondsRealtime(ClickEnableDelay);

			if (tint != null)
			{
				tint.SetOnClick(delegate
				{
					step1Clicked = true;
				});
			}

			yield return new WaitUntil(() => step1Clicked);

			tint?.ClearOnClick();
			KillIntroSequence();
			manager?.OnTutorialEnd();
		}

		private bool TryPrepareIntroState()
		{
			try
			{
				if (tint != null)
				{
					tint.SetColor(TintColor);
					tint.Set(TintCenterX, TintCenterY, 0f, 0f, 0f);
				}

				if (itemIcon != null)
				{
					itemIcon.transform.localScale = Vector3.zero;
				}
				if (newItemTitleEffects != null)
				{
					newItemTitleEffects.localScale = TinyScale;
					TryRefreshEffects(newItemTitleEffects);
				}
				if (titleEffects != null)
				{
					titleEffects.localScale = Vector3.zero;
				}
				if (descriptionEffects != null)
				{
					descriptionEffects.localScale = Vector3.zero;
				}
				if (continueEffects != null)
				{
					continueEffects.localScale = Vector3.zero;
				}

				if (iconParticles != null)
				{
					for (int i = 0; i < iconParticles.Length; i++)
					{
						ParticleSystem particle = iconParticles[i];
						if (particle == null)
						{
							continue;
						}
						particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
						particle.Clear(false);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);
				return false;
			}
		}

		private void ApplyTutorialContent(GenericItemTutorialDataItem item)
		{
			if (item == null)
			{
				return;
			}

			SetTextWithEffects(itemTitle, item.title);
			if (itemIcon != null)
			{
				itemIcon.sprite = item.itemSprite;
				itemIcon.enabled = item.itemSprite != null;
				itemIcon.SetAllDirty();
			}
			SetTextWithEffects(descriptionText, (item.description ?? string.Empty).Replace("\\n", "\n"));
			Canvas.ForceUpdateCanvases();
		}

		private IEnumerator RefreshTutorialContentNextFrame()
		{
			yield return null;
			ApplyTutorialContent(_activeItemData);
		}

		private void TryMarkTutorialCompleted(GenericItemTutorialType typeToPlay)
		{
			try
			{
				TutorialProgressData tutorialProgress = SaveService.Data?.TutorialProgress;
				if (tutorialProgress == null)
				{
					return;
				}

				if (tutorialProgress.genericItemTutorialsDone == null)
				{
					tutorialProgress.genericItemTutorialsDone = new List<GenericItemTutorialType>();
				}
				if (!tutorialProgress.genericItemTutorialsDone.Contains(typeToPlay))
				{
					tutorialProgress.genericItemTutorialsDone.Add(typeToPlay);
				}
				SaveService.Save();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);
			}
		}

		private void SetTextWithEffects(TextMeshProUGUI textComponent, string value)
		{
			if (textComponent == null)
			{
				return;
			}

			TMPEffects effects = textComponent.GetComponent<TMPEffects>();
			if (effects != null)
			{
				effects.SetText(value ?? string.Empty);
				effects.ShowInstant();
				return;
			}

			textComponent.SetText(value ?? string.Empty);
			textComponent.ForceMeshUpdate();
		}

		private void TryRefreshEffects(Transform target)
		{
			if (target == null)
			{
				return;
			}

			try
			{
				target.GetComponent<TMPEffects>()?.RefreshMeshes();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);
			}
		}

		private bool TryStartIntroSequence(float iconStartTime, float descriptionStartTime)
		{
			try
			{
				KillIntroSequence();
				_activeIntroSequence = DOTween.Sequence();
				_activeIntroSequence.InsertCallback(0f, () => PlayScaleTween(newItemTitleEffects, newItemTitleScaleCurve, newItemTitleScaleTime));
				_activeIntroSequence.InsertCallback(newItemTitleScaleTime * 0.5f, () => PlayScaleTween(titleEffects, titleScaleCurve, titleScaleTime));
				if (itemIcon != null)
				{
					_activeIntroSequence.Insert(iconStartTime, itemIcon.transform.DOScale(Vector3.one, iconScaleTime).SetEase(iconScaleCurve));
				}
				_activeIntroSequence.InsertCallback(iconStartTime + iconScaleDuration, PlayIconParticles);
				_activeIntroSequence.InsertCallback(descriptionStartTime, () => PlayScaleTween(descriptionEffects, descriptionScaleCurve, descriptionScaleTime));
				_activeIntroSequence.InsertCallback(descriptionStartTime + descriptionScaleTime * 0.5f, () => PlayScaleTween(continueEffects, continueScaleCurve, continueScaleTime));
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);
				return false;
			}
		}

		private void RevealTutorialInstant()
		{
			if (itemIcon != null)
			{
				itemIcon.transform.localScale = Vector3.one;
			}
			if (newItemTitleEffects != null)
			{
				newItemTitleEffects.localScale = Vector3.one;
			}
			if (titleEffects != null)
			{
				titleEffects.localScale = Vector3.one;
			}
			if (descriptionEffects != null)
			{
				descriptionEffects.localScale = Vector3.one;
			}
			if (continueEffects != null)
			{
				continueEffects.localScale = Vector3.one;
			}
			PlayIconParticles();
		}

		private void PlayScaleTween(Transform target, AnimationCurve curve, float duration)
		{
			if (target == null)
			{
				return;
			}
			target.DOScale(Vector3.one, duration).SetEase(curve);
		}

		private void PlayIconParticles()
		{
			if (iconParticles == null)
			{
				return;
			}
			for (int i = 0; i < iconParticles.Length; i++)
			{
				iconParticles[i]?.Play(false);
			}
		}

		private void OnDisable()
		{
			CleanupActiveEffects();
		}

		private void OnDestroy()
		{
			CleanupActiveEffects();
		}

		private void CleanupActiveEffects()
		{
			StopAllCoroutines();
			_activeItemData = null;
			tint?.ClearOnClick();
			KillIntroSequence();
			itemIcon?.transform.DOKill();
			newItemTitleEffects?.DOKill();
			titleEffects?.DOKill();
			descriptionEffects?.DOKill();
			continueEffects?.DOKill();
		}

		private void KillIntroSequence()
		{
			if (_activeIntroSequence != null)
			{
				_activeIntroSequence.Kill();
				_activeIntroSequence = null;
			}
		}
	}
}
