using System;
using System.Collections;
using DG.Tweening;
using Level;
using LocalSave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Gameplay.GameplayTutorial
{
	public class TutorialHorizontalTable : TutorialBase
	{
		private Sequence _activeIntroSequence;

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

		public override void Play(TutorialManager manager, Action<int, int> onStageComplete)
		{
			CleanupActiveEffects();
			TutorialProgressData tutorialProgress = SaveService.Data?.TutorialProgress;
			if (tutorialProgress != null)
			{
				tutorialProgress.tutorialHorizontalTableDone = true;
				SaveService.Save();
			}

			tint?.BecomeTransparentInstant();
			gameObject.SetActive(true);
			StartCoroutine(TutorialRoutine(manager));
		}

		public override bool ShouldPlay(LevelData levelData)
		{
			if (levelData == null || (SaveService.Data?.TutorialProgress?.tutorialHorizontalTableDone ?? false))
			{
				return false;
			}

			if (levelData.stages == null)
			{
				return false;
			}

			for (int i = 0; i < levelData.stages.Count; i++)
			{
				LevelStageData stage = levelData.stages[i];
				if (stage?.tables == null)
				{
					continue;
				}
				for (int j = 0; j < stage.tables.Count; j++)
				{
					LevelTableData table = stage.tables[j];
					if (table != null && table.movH)
					{
						return true;
					}
				}
			}

			return false;
		}

		private IEnumerator TutorialRoutine(TutorialManager manager)
		{
			bool step1Clicked = false;

			if (tint != null)
			{
				tint.SetColor(TintColor);
				tint.Set(TintCenterX, TintCenterY, 0f, 0f, 0f);
			}

			if (newItemTitleEffects != null)
			{
				newItemTitleEffects.localScale = TinyScale;
				newItemTitleEffects.GetComponent<TMPEffects>()?.RefreshMeshes();
			}
			if (itemIcon != null)
			{
				itemIcon.transform.localScale = Vector3.zero;
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

			float iconStartTime = newItemTitleScaleTime * 0.5f + titleScaleTime * 0.5f;
			float descriptionStartTime = iconStartTime + iconScaleDuration * 0.5f;
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

			yield return new WaitForSeconds(ClickEnableDelay);

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
