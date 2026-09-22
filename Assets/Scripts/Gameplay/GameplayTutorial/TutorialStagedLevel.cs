using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Level;
using LocalSave;
using UnityEngine;

namespace Gameplay.GameplayTutorial
{
	public class TutorialStagedLevel : TutorialBase
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass5_0
		{
			public bool step1Clicked;

			public bool step2Clicked;

			internal void _003CTutorialRoutine_003Eb__0()
			{
			}

			internal bool _003CTutorialRoutine_003Eb__1()
			{
				return false;
			}

			internal void _003CTutorialRoutine_003Eb__2()
			{
			}

			internal bool _003CTutorialRoutine_003Eb__3()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003CTutorialRoutine_003Ed__5 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public TutorialStagedLevel _003C_003E4__this;

			private _003C_003Ec__DisplayClass5_0 _003C_003E8__1;

			public Action<int, int> onStageComplete;

			public TutorialManager manager;

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
			public _003CTutorialRoutine_003Ed__5(int _003C_003E1__state)
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
		private GameObject savePanel;

		[SerializeField]
		private GameObject checkPanel;

		[SerializeField]
		private TutorialTint tint;

		private static readonly Color TintColor = new Color(0f, 0f, 0f, 0.9f);

		private const float HighlightAnimDuration = 0.25f;

		private const float SaveHolePosX = 0.5f;

		private const float SaveHolePosY = 0.82f;

		private const float SaveHoleInnerWidth = 0.11f;

		private const float SaveHoleOuterWidth = 0.13f;

		private const float CheckHolePosX = 0.36f;

		private const float CheckHolePosY = 0.9f;

		private const float CheckHoleInnerWidth = 0.065f;

		private const float CheckHoleOuterWidth = 0.095f;

		private const float StepSwapDelay = 0.5f;

		public override void Play(TutorialManager manager, Action<int, int> onStageComplete)
		{
			CleanupTutorialState();
			if (SaveService.Data?.TutorialProgress != null)
			{
				SaveService.Data.TutorialProgress.tutorialStagedLevelDone = true;
				SaveService.Save();
			}
			tint?.BecomeTransparentInstant();
			gameObject.SetActive(true);
			if (savePanel != null)
			{
				savePanel.SetActive(false);
			}
			if (checkPanel != null)
			{
				checkPanel.SetActive(false);
			}
			StartCoroutine(TutorialRoutineImpl(manager, onStageComplete));
		}

		public override bool ShouldPlay(LevelData levelData)
		{
			if (levelData == null || levelData.GetStageCount() < 2)
			{
				return false;
			}
			return !(SaveService.Data?.TutorialProgress?.tutorialStagedLevelDone ?? false);
		}

		[IteratorStateMachine(typeof(_003CTutorialRoutine_003Ed__5))]
		private IEnumerator TutorialRoutine(TutorialManager manager, Action<int, int> onStageComplete)
		{
			return TutorialRoutineImpl(manager, onStageComplete);
		}

		private IEnumerator TutorialRoutineImpl(TutorialManager manager, Action<int, int> onStageComplete)
		{
			bool step1Clicked = false;
			bool step2Clicked = false;
			if (tint != null)
			{
				tint.SetColor(TintColor);
			}
			if (savePanel != null)
			{
				savePanel.SetActive(true);
			}
			tint?.Set(SaveHolePosX, SaveHolePosY, SaveHoleInnerWidth, SaveHoleOuterWidth, HighlightAnimDuration);
			yield return new WaitForSeconds(HighlightAnimDuration);
			if (tint != null)
			{
				tint.SetOnClick(delegate
				{
					step1Clicked = true;
				});
			}
			yield return new WaitUntil(() => step1Clicked);
			if (savePanel != null)
			{
				savePanel.SetActive(false);
			}
			onStageComplete?.Invoke(1, 2);
			tint?.BecomeTransparentInstant();
			yield return new WaitForSeconds(StepSwapDelay);
			if (tint != null)
			{
				tint.SetColor(TintColor);
			}
			if (checkPanel != null)
			{
				checkPanel.SetActive(true);
			}
			tint?.Set(CheckHolePosX, CheckHolePosY, CheckHoleInnerWidth, CheckHoleOuterWidth, HighlightAnimDuration);
			yield return new WaitForSeconds(HighlightAnimDuration);
			if (tint != null)
			{
				tint.SetOnClick(delegate
				{
					step2Clicked = true;
				});
			}
			yield return new WaitUntil(() => step2Clicked);
			if (tint != null)
			{
				tint.ClearOnClick();
			}
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
			tint?.BecomeTransparentInstant();
			if (savePanel != null)
			{
				savePanel.SetActive(false);
			}
			if (checkPanel != null)
			{
				checkPanel.SetActive(false);
			}
		}
	}
}
