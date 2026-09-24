using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DG.Tweening;
using Gameplay.Level;
using Core;
using LocalSave;
using RemoteConfig;
using Scene;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Splash
{
	public class SplashController : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass8_0
		{
			public float warmupStartTime;

			internal bool _003CPlayIntroVisualsRoutine_003Eb__0()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003CPlayIntroVisualsRoutine_003Ed__8 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public SplashController _003C_003E4__this;

			private _003C_003Ec__DisplayClass8_0 _003C_003E8__1;

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
			public _003CPlayIntroVisualsRoutine_003Ed__8(int _003C_003E1__state)
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

		[CompilerGenerated]
		private sealed class _003CRunOperationsRoutine_003Ed__9 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public SplashController _003C_003E4__this;

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
			public _003CRunOperationsRoutine_003Ed__9(int _003C_003E1__state)
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

		[CompilerGenerated]
		private sealed class _003CStart_003Ed__7 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public SplashController _003C_003E4__this;

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
			public _003CStart_003Ed__7(int _003C_003E1__state)
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
		private Image flowSplash;

		[SerializeField]
		private Image gameSplash;

		[SerializeField]
		private GameObject wrongEnvironmentPopup;

		[SerializeField]
		private GameObject consentPopup;

		[SerializeField]
		private FlowButton consentButton;

		[SerializeField]
		private ShaderVariantCollection shaderCollection;

		private bool _adInitFinished;

		[IteratorStateMachine(typeof(_003CStart_003Ed__7))]
		private IEnumerator Start()
		{
			return StartRoutine();
		}

		[IteratorStateMachine(typeof(_003CPlayIntroVisualsRoutine_003Ed__8))]
		private IEnumerator PlayIntroVisualsRoutine()
		{
			return PlayIntroVisualsRoutineImpl();
		}

		[IteratorStateMachine(typeof(_003CRunOperationsRoutine_003Ed__9))]
		private IEnumerator RunOperationsRoutine()
		{
			return RunOperationsRoutineImpl();
		}

		private void LoadNextScene()
		{
			if (SaveService.Data != null && SaveService.Data.SessionId == 1 && GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId == 1)
			{
				SceneHandler.LoadScene(SceneType.Gameplay, true);
				return;
			}
			SceneHandler.LoadScene(SceneType.Menu);
		}

		private bool TryShowWrongEnvironmentPopup()
		{
			if (SaveService.Data == null)
			{
				return false;
			}
			string currentEnvironment = CommonUtils.GetBuildEnvironmentKey();
			if (string.IsNullOrEmpty(SaveService.Data.BuildEnvironment))
			{
				SaveService.Data.BuildEnvironment = currentEnvironment;
				SaveService.Save();
				return false;
			}
			bool wrongEnvironment = SaveService.Data.BuildEnvironment != currentEnvironment;
			if (wrongEnvironment && wrongEnvironmentPopup != null)
			{
				wrongEnvironmentPopup.SetActive(true);
			}
			return wrongEnvironment;
		}

		private IEnumerator StartRoutine()
		{
			if (wrongEnvironmentPopup != null)
			{
				wrongEnvironmentPopup.SetActive(false);
			}
			if (consentPopup != null)
			{
				consentPopup.SetActive(false);
			}
			if (TryShowWrongEnvironmentPopup())
			{
				yield break;
			}
			yield return PlayIntroVisualsRoutine();
			yield return RunOperationsRoutine();
			LoadNextScene();
		}

		private IEnumerator PlayIntroVisualsRoutineImpl()
		{
			if (flowSplash != null)
			{
				Color color = flowSplash.color;
				color.a = 1f;
				flowSplash.color = color;
				yield return flowSplash.DOFade(0f, 0.35f).WaitForCompletion();
			}
			if (gameSplash != null)
			{
				Color color = gameSplash.color;
				color.a = 0f;
				gameSplash.color = color;
				yield return gameSplash.DOFade(1f, 0.35f).WaitForCompletion();
			}
			float warmupStartTime = Time.realtimeSinceStartup;
			shaderCollection?.WarmUp();
			yield return new WaitUntil(() => Time.realtimeSinceStartup - warmupStartTime >= 0.1f);
		}

		private IEnumerator RunOperationsRoutineImpl()
		{
			RemoteConfigService.InitializeLocal();
			_adInitFinished = true;
			RoutineRunner.Instance.StartCoroutine(RemoteConfigUpdater.FetchCoroutine());
			RemoteCollectionUpdater.RequestCollectionUpdateIfNeeded();
			yield break;
		}

		private void OnDisable()
		{
			CleanupSplashTweens();
		}

		private void OnDestroy()
		{
			CleanupSplashTweens();
		}

		private void CleanupSplashTweens()
		{
			StopAllCoroutines();
			flowSplash?.DOKill();
			gameSplash?.DOKill();
		}
	}
}
