using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Audio;
using DG.Tweening;
using Gameplay;
using Inventory;
using Menu;
using Scene;
using Service;
using UI;
using UnityEngine;

namespace Popups
{
	public class WinPopup : BasePopup
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass7_0
		{
			public int amount;

			public bool coinCollectFinished;

			internal void _003CWinRoutine_003Eb__1()
			{
			}

			internal void _003CWinRoutine_003Eb__2()
			{
			}

			internal bool _003CWinRoutine_003Eb__3()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003CWinRoutine_003Ed__7 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public WinPopup _003C_003E4__this;

			private _003C_003Ec__DisplayClass7_0 _003C_003E8__1;

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
			public _003CWinRoutine_003Ed__7(int _003C_003E1__state)
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

		private const float AutoCloseTime = 1.8f;

		[SerializeField]
		private ParticleSystem winParticles;

		[SerializeField]
		private CoinCollectTarget coinTarget;

		[SerializeField]
		private GameObject coinPanel;

		private bool _willSkipMenu;

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			GameController gameController = ServiceLocator.Get<GameController>();
			_willSkipMenu = gameController != null && gameController.ShouldSkipMenuAfterWin();
		}

		public override void Open()
		{
			base.Open();
			StopAllCoroutines();
			StartCoroutine(WinRoutine());
		}

		public override void PlayPopupOpenAnimation()
		{
		}

		[IteratorStateMachine(typeof(_003CWinRoutine_003Ed__7))]
		private IEnumerator WinRoutine()
		{
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.Win);
			winParticles?.Play();

			RectTransform coinPanelTransform = coinPanel != null ? coinPanel.GetComponent<RectTransform>() : null;
			coinPanel?.SetActive(_willSkipMenu);

			if (_willSkipMenu && coinPanelTransform != null)
			{
				Vector2 anchoredPosition = coinPanelTransform.anchoredPosition;
				coinPanelTransform.anchoredPosition = anchoredPosition + Vector2.up * 500f;
				coinPanelTransform.DOAnchorPosY(anchoredPosition.y, 0.4f).SetEase(Ease.OutBack).SetDelay(0.2f);
			}

			if (_willSkipMenu && coinTarget != null)
			{
				int amount = MenuController.CoinToCollect;
				MenuController.CoinToCollect = 0;
				yield return new WaitForSeconds(0.8f);
				bool coinCollectFinished = false;
				int remainingAmount = amount;
				int remainingHits = 10;
				coinTarget.Play(10, 0.5f, amount, delegate
				{
					if (remainingAmount <= 0 || remainingHits <= 0)
					{
						return;
					}
					int hitAmount = Mathf.CeilToInt((float)remainingAmount / remainingHits);
					remainingAmount -= hitAmount;
					remainingHits--;
					InventoryHelper.ReduceDelayedAmount(new InventoryPayload(InventoryItemType.Coin, hitAmount));
				}, delegate
				{
					coinCollectFinished = true;
				});
				yield return new WaitUntil(() => coinCollectFinished);
			}
			else
			{
				yield return new WaitForSeconds(AutoCloseTime);
			}

			SceneHandler.LoadScene(_willSkipMenu ? SceneType.Gameplay : SceneType.Menu, false);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			CleanupPopupState();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			CleanupPopupState();
		}

		private void CleanupPopupState()
		{
			StopAllCoroutines();
			RectTransform coinPanelTransform = coinPanel != null ? coinPanel.GetComponent<RectTransform>() : null;
			coinPanelTransform?.DOKill();
			coinPanel?.SetActive(false);
		}
	}
}
