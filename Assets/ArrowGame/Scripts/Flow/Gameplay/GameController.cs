using Cysharp.Threading.Tasks;
using Core;
using DG.Tweening;
using GameFramework;
using Gameplay.Collisions;
using Gameplay.GameplayTutorial;
using Gameplay.Level;
using Gameplay.Objects;
using Gameplay.Obstacle;
using Gameplay.Particles;
using Inventory;
using Level;
using LocalSave;
using Menu;
using Scene;
using Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Util;

namespace Gameplay
{
	public class GameController : ServiceMonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass40_0
		{
			public bool step1Complete;

			internal void _003CPlayStagedLevelStartAnimationRoutine_003Eb__0(int step, int total)
			{
			}

			internal bool _003CPlayStagedLevelStartAnimationRoutine_003Eb__1()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass41_0
		{
			public GameController _003C_003E4__this;

			public bool poolReady;

			public int rocketCount;

			public List<BaseObject> sharedPool;

			public List<(GameObject go, OpeningRocket rocket)> instances;

			internal void _003CPlayOpeningRocketRoutine_003Eb__0()
			{
			}

			internal bool _003CPlayOpeningRocketRoutine_003Eb__1()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003CFinishStageTransitionAfterRocket_003Ed__43 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public GameController _003C_003E4__this;

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
			public _003CFinishStageTransitionAfterRocket_003Ed__43(int _003C_003E1__state)
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
		private sealed class _003CGetOwnersTouchingTable_003Ed__55 : IEnumerable<IContactNodeOwner>, IEnumerable, IEnumerator<IContactNodeOwner>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private IContactNodeOwner _003C_003E2__current;

			private int _003C_003El__initialThreadId;

			private Table table;

			public Table _003C_003E3__table;

			public GameController _003C_003E4__this;

			private List<BaseObject> _003CliveObjects_003E5__2;

			private int _003Ci_003E5__3;

			IContactNodeOwner IEnumerator<IContactNodeOwner>.Current
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
			public _003CGetOwnersTouchingTable_003Ed__55(int _003C_003E1__state)
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

			[DebuggerHidden]
			IEnumerator<IContactNodeOwner> IEnumerable<IContactNodeOwner>.GetEnumerator()
			{
				return null;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return null;
			}
		}

		[CompilerGenerated]
		private sealed class _003CPlayOpeningRocketRoutine_003Ed__41 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public GameController _003C_003E4__this;

			private _003C_003Ec__DisplayClass41_0 _003C_003E8__1;

			private int _003Ci_003E5__2;

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
			public _003CPlayOpeningRocketRoutine_003Ed__41(int _003C_003E1__state)
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
		private sealed class _003CPlayStagedLevelStartAnimationRoutine_003Ed__40 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public GameController _003C_003E4__this;

			private _003C_003Ec__DisplayClass40_0 _003C_003E8__1;

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
			public _003CPlayStagedLevelStartAnimationRoutine_003Ed__40(int _003C_003E1__state)
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
		private sealed class _003CRunPostLoadingGameplayReveal_003Ed__38 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public GameController _003C_003E4__this;

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
			public _003CRunPostLoadingGameplayReveal_003Ed__38(int _003C_003E1__state)
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
		private sealed class _003CTransitToNextStage_003Ed__66 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public GameController _003C_003E4__this;

			public Action onComplete;

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
			public _003CTransitToNextStage_003Ed__66(int _003C_003E1__state)
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
		private sealed class _003CWaitForActiveTutorialsToFinish_003Ed__39 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

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
			public _003CWaitForActiveTutorialsToFinish_003Ed__39(int _003C_003E1__state)
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


		[SerializeField, Min(0.01f), Tooltip("美元奖励首次反馈和后续差额更新间隔（秒），不会清空累计数量。")]
		private float eliminateRewardQuietTime = 0.5f;
		[SerializeField, Min(0.01f), Tooltip("连续多久没有射击或打碎后结束本轮累计（秒）。")]
		private float eliminateRewardRoundIdleTime = 8f;
		private readonly List<EliminateReward> _eliminateRewardTiers = new List<EliminateReward>();
		private int _minEliminateRewardCount = int.MaxValue;
		private int _eliminateCount;
		private float _lastEliminateActivityTime;
		private float _eliminateRewardPendingSince;
		private float _eliminateRewardSample;
		private float _eliminateRewardAwarded;
		private bool _eliminateRewardDirty;

		private const int RocketPerPrelevel = 3;

		[SerializeField]
		private Cannon cannon;

		[SerializeField]
		private GameObject backgroundPrefab;

		[SerializeField]
		private GameObject ground;

		[SerializeField]
		private Transform stagesRoot;

		[SerializeField]
		private Animator stageSignAnimator;

		[SerializeField]
		private TextMeshPro stageSignText;

		[SerializeField]
		private AnimationCurve stageScaleDownCurve;

		[SerializeField]
		private AnimationCurve stageScaleUpCurve;

		[SerializeField]
		private GameObject openingRocketPrefab;

		[SerializeField]
		private Image boosterTint;

		private Coroutine _pendingWinPopupRoutine;

		private const int BallPoolSize = 5;

		private const float FrontLayerDepth = 0.5f;

		public const float OpeningRocketSpawnInterval = 0.1f;

		private const int SkipMenuLevelLimit = 19;

		private const float OutOfMovesForceFailDelay = 15f;

		private readonly List<Ball> _liveBalls = new List<Ball>();

		private LevelStage _currentStage;

		private LevelData _levelData;

		private int _remainingMoves;


		private bool _gameplayLocked = true;

		private bool _initializationStarted;

		private bool _gameplayPointerStarted;

		private int _levelIndex;

		private int _endBlockerCount;

		private int _externalEndBlocker;

		private bool _levelStarted;

		private bool _prelevelRocketSelected;

		private int _activeStreakCount;

		private int _currentStageIndex;

		private GameplayAnalyticsHelper _analyticsHelper;

		private string _collectionName;

		private int _collectionVersion;

		private ParticleData _particleData;

		private GameplayAssetCache.Lease _gameplayAssets;

		public ObjectData LoadedObjectData { get; private set; }

		public PhysicsMaterial LoadedPhysicsMaterial { get; private set; }

		[NonSerialized]
		public float TableEndLineZ;

		private async void Start()
		{
			// The loading form owns normal entry; retain direct scene play in the editor.
			if (PlayLoadingUIForm.IsLoading || _initializationStarted) return;
			try
			{
				await PrepareGameplayAsync(this.GetCancellationTokenOnDestroy());
				ExposeGameplay();
			}
			catch (OperationCanceledException) { }
			catch (Exception exception) { UnityEngine.Debug.LogException(exception, this); }
		}

		public async UniTask PrepareGameplayAsync(System.Threading.CancellationToken cancellationToken)
		{
			_initializationStarted = true;
			_gameplayLocked = true;
			GameplayAssetCache.CancelBackground();
			cancellationToken.ThrowIfCancellationRequested();
			_analyticsHelper = new GameplayAnalyticsHelper();
			InitializeEliminateRewards();
			_levelIndex = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
			double loadStart = Time.realtimeSinceStartupAsDouble;
			await LoadLevelAsync(_levelIndex, cancellationToken);
			double jsonReady = Time.realtimeSinceStartupAsDouble;
			cancellationToken.ThrowIfCancellationRequested();
			_gameplayAssets = await GameplayAssetCache.PrepareAsync(_levelData, cancellationToken);
			LoadedObjectData = _gameplayAssets.Objects;
			_particleData = _gameplayAssets.Particles;
			LoadedPhysicsMaterial = _gameplayAssets.Physics;
			double assetsReady = Time.realtimeSinceStartupAsDouble;
			_currentStage = LoadStage(_currentStageIndex);
			UnityEngine.Debug.Log($"[GameplayLoad] Level {_levelIndex}: JSON={(jsonReady - loadStart) * 1000:F0}ms, assets={(assetsReady - jsonReady) * 1000:F0}ms, instantiate={(Time.realtimeSinceStartupAsDouble - assetsReady) * 1000:F0}ms");
			UpdateRemainingMoves();
			cannon?.InitializeBallPool(5);
			cannon?.PrewarmBalls();
			ServiceLocator.Get<ParticleController>()?.Initialize(_particleData);
			AfterStageSetup();
		}

		public void ExposeGameplay()
		{
			PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
			_activeStreakCount = StreakHelper.GetStreakCount();
			StreakHelper.OnGameplayLevelStarted(_activeStreakCount);
			_prelevelRocketSelected = playerData.PendingRocketUse;
			playerData.PendingRocketUse = false;
			PlayUIForm.SetRocketUseCount(_prelevelRocketSelected ? 1 : 0);
			PrelevelBoosterHelper.ClearSelection();
			AfterLevelSetup();
			OnGameplayExposed();
			GameplayAssetCache.PreloadNext(_levelIndex + 1, this.GetCancellationTokenOnDestroy(),
				() => this != null && isActiveAndEnabled && !_gameplayLocked && !_gameplayPointerStarted && _liveBalls.Count == 0);
		}

		private void OnGameplayExposed()
		{
			SceneHandler.OnLoadingDisappear -= OnGameplayExposed;
			_gameplayLocked = true;
			StartCoroutine(RunPostLoadingGameplayReveal());
		}

		[IteratorStateMachine(typeof(_003CRunPostLoadingGameplayReveal_003Ed__38))]
		private IEnumerator RunPostLoadingGameplayReveal()
		{
			return RunPostLoadingGameplayRevealInternal();
		}

		[IteratorStateMachine(typeof(_003CWaitForActiveTutorialsToFinish_003Ed__39))]
		private IEnumerator WaitForActiveTutorialsToFinish()
		{
			return WaitForActiveTutorialsToFinishInternal();
		}

		[IteratorStateMachine(typeof(_003CPlayStagedLevelStartAnimationRoutine_003Ed__40))]
		private IEnumerator PlayStagedLevelStartAnimationRoutine()
		{
			return PlayStagedLevelStartAnimationRoutineInternal();
		}

		[IteratorStateMachine(typeof(_003CPlayOpeningRocketRoutine_003Ed__41))]
		private IEnumerator PlayOpeningRocketRoutine()
		{
			int rocketCount = _activeStreakCount + (_prelevelRocketSelected ? RocketPerPrelevel : 0);
			if (rocketCount < 1 || openingRocketPrefab == null || _currentStage?.LiveObjects == null || _currentStage.LiveObjects.Count < 1)
			{
				yield break;
			}
			if (boosterTint != null)
			{
				boosterTint.DOKill();
				boosterTint.gameObject.SetActive(true);
				Color color = boosterTint.color;
				color.a = 0f;
				boosterTint.color = color;
				boosterTint.DOFade(0.6f, 0.2f).SetDelay(0.3f);
				boosterTint.DOFade(0f, 0.2f).SetDelay(1.1f).OnComplete(delegate
				{
					if (boosterTint != null)
					{
						boosterTint.gameObject.SetActive(false);
					}
				});
			}
			List<OpeningRocket> activeRockets = new List<OpeningRocket>();
			List<BaseObject> sharedPool = null;
			bool sharedPoolInitialized = false;
			Func<BaseObject> targetSelector = delegate
			{
				if (!sharedPoolInitialized)
				{
					sharedPoolInitialized = true;
					List<BaseObject> candidates = BuildFrontLayerObjectCandidates(_currentStage?.LiveObjects);
					if (candidates != null && candidates.Count >= rocketCount)
					{
						sharedPool = new List<BaseObject>(candidates);
					}
				}
				if (sharedPool != null)
				{
					if (sharedPool.Count < 1)
					{
						return null;
					}
					int index = UnityEngine.Random.Range(0, sharedPool.Count);
					BaseObject item = sharedPool[index];
					sharedPool.RemoveAt(index);
					return item;
				}
				List<BaseObject> liveCandidates = BuildFrontLayerObjectCandidates(_currentStage?.LiveObjects);
				if (liveCandidates == null || liveCandidates.Count < 1)
				{
					return null;
				}
				return liveCandidates[UnityEngine.Random.Range(0, liveCandidates.Count)];
			};
			for (int i = 0; i < rocketCount; i++)
			{
				if (i >= 1)
				{
					yield return new WaitForSeconds(OpeningRocketSpawnInterval);
				}
				if (TrySpawnOpeningRocket(i, rocketCount, targetSelector, out GameObject _, out OpeningRocket rocket) && rocket != null)
				{
					activeRockets.Add(rocket);
				}
			}
			if (activeRockets.Count > 0)
			{
				yield return new WaitUntil(delegate
				{
					for (int j = 0; j < activeRockets.Count; j++)
					{
						OpeningRocket openingRocket = activeRockets[j];
						if (openingRocket != null && !openingRocket.IsComplete)
						{
							return false;
						}
					}
					return true;
				});
			}
			if (boosterTint != null && boosterTint.gameObject.activeSelf)
			{
				boosterTint.DOKill();
				Color color2 = boosterTint.color;
				color2.a = 0f;
				boosterTint.color = color2;
				boosterTint.gameObject.SetActive(false);
			}
		}

		private bool TrySpawnOpeningRocket(int index, int totalCount, Func<BaseObject> targetSelector, out GameObject rocketGo, out OpeningRocket rocket)
		{
			rocketGo = null;
			rocket = null;
			if (openingRocketPrefab == null || targetSelector == null)
			{
				return false;
			}
			Vector3 spawnPosition = Vector3.back * 50f;
			rocketGo = UnityEngine.Object.Instantiate(openingRocketPrefab, spawnPosition, Quaternion.identity);
			if (rocketGo == null || !rocketGo.TryGetComponent(out rocket))
			{
				if (rocketGo != null)
				{
					UnityEngine.Object.Destroy(rocketGo);
				}
				rocketGo = null;
				rocket = null;
				return false;
			}
			rocket.BeginFlight(index, totalCount, targetSelector);
			return true;
		}

		[IteratorStateMachine(typeof(_003CFinishStageTransitionAfterRocket_003Ed__43))]
		private IEnumerator FinishStageTransitionAfterRocket()
		{
			yield return WaitForActiveTutorialsToFinish();
			if (_activeStreakCount > 0 || _prelevelRocketSelected)
			{
				yield return PlayOpeningRocketRoutine();
			}
			_levelStarted = true;
			_gameplayLocked = false;
		}

		private static List<BaseObject> BuildFrontLayerObjectCandidates(List<BaseObject> live)
		{
			List<BaseObject> result = new List<BaseObject>();
			if (live == null || live.Count == 0)
			{
				return result;
			}
			float minZ = float.MaxValue;
			for (int i = 0; i < live.Count; i++)
			{
				BaseObject baseObject = live[i];
				if (baseObject == null || baseObject.BeingDestroyed)
				{
					continue;
				}
				minZ = Mathf.Min(minZ, baseObject.transform.position.z);
			}
			for (int j = 0; j < live.Count; j++)
			{
				BaseObject baseObject2 = live[j];
				if (baseObject2 != null && !baseObject2.BeingDestroyed && baseObject2.transform.position.z <= minZ + 0.5f)
				{
					result.Add(baseObject2);
				}
			}
			if (result.Count == 0)
			{
				for (int k = 0; k < live.Count; k++)
				{
					BaseObject baseObject3 = live[k];
					if (baseObject3 != null && !baseObject3.BeingDestroyed)
					{
						result.Add(baseObject3);
					}
				}
			}
			return result;
		}

		private IEnumerator RunPostLoadingGameplayRevealInternal()
		{
			if (_levelData != null && _levelData.GetStageCount() > 1)
			{
				yield return PlayStagedLevelStartAnimationRoutine();
			}
			else
			{
				yield return WaitForActiveTutorialsToFinish();
			}
			if (_activeStreakCount > 0 || _prelevelRocketSelected)
			{
				yield return PlayOpeningRocketRoutine();
			}
			_levelStarted = true;
			_gameplayLocked = false;
			if (_currentStage?.LiveObjects != null)
			{
				foreach (BaseObject target in _currentStage.LiveObjects)
				{
					if (target == null) continue;
					PlayUIForm.ShowFirstLevelGuides(target.transform);
					break;
				}
			}
		}

		private IEnumerator WaitForActiveTutorialsToFinishInternal()
		{
			TutorialManager tutorialManager = ServiceLocator.Get<TutorialManager>();
			tutorialManager?.CheckTutorial();
			yield return new WaitUntil(() => tutorialManager == null || !tutorialManager.AnyActiveTutorial());
		}

		private IEnumerator PlayStagedLevelStartAnimationRoutineInternal()
		{
			if (stageSignText != null)
			{
				stageSignText.text = string.Format("Stage {0}/{1}", _currentStageIndex + 1, _levelData != null ? _levelData.GetStageCount() : 1);
			}
			yield return new WaitForSeconds(0.4f);
			stageSignAnimator?.Play("StageSignOpenAnim");
			yield return new WaitForSeconds(0.7f);
			yield return new WaitForSeconds(0.15f);
			bool step1Complete = false;
			TutorialManager tutorialManager = ServiceLocator.Get<TutorialManager>();
			if (tutorialManager != null && tutorialManager.CheckStagedLevelTutorial(delegate(int step, int total)
			{
				if (step == 1)
				{
					step1Complete = true;
				}
			}))
			{
				yield return new WaitUntil(() => step1Complete);
			}
			yield return new WaitForSeconds(0.15f);
			stageSignAnimator?.Play("StageSignCloseAnim");
			yield return new WaitForSeconds(0.5f);
		}

		private void AfterLevelSetup()
		{
			LifeHelper.LoseLife();
			List<bool> prelevelBoostersUsed = new List<bool>
			{
				_prelevelRocketSelected
			};
			_analyticsHelper?.LevelStart(_levelIndex, _levelData, prelevelBoostersUsed, GetRemainingObjectCount(), GetRemainingObjectMass(), _collectionName, _collectionVersion);
		}

		private void AfterStageSetup()
		{
			SetObjectMasses();
			if (_currentStage == null)
			{
				return;
			}
			cannon?.SetPlaneBounds(_currentStage.LiveObjects, _currentStage.Tables);
			ServiceLocator.Get<PhysicsWarmup>()?.RunWarmup(_currentStage.LiveObjects);
			PositionBackGroundCollider();
		}

		private void Update()
		{
			// Merge physics destruction callbacks before checking stage completion.
			ProcessEliminateRewards();
			CheckForClick();
			CheckForComplete();
			CheckForFail();
		}

		private void FixedUpdate()
		{
			if (_currentStage == null)
			{
				return;
			}
			if (_currentStage.LiveObjects != null)
			{
				for (int i = 0; i < _currentStage.LiveObjects.Count; i++)
				{
					BaseObject liveObject = _currentStage.LiveObjects[i];
					if (liveObject != null)
					{
						liveObject.TouchingTable = null;
					}
				}
			}
			for (int j = 0; j < _liveBalls.Count; j++)
			{
				Ball liveBall = _liveBalls[j];
				if (liveBall != null)
				{
					liveBall.TouchingTable = null;
				}
			}
			if (_currentStage.Tables == null)
			{
				return;
			}
			for (int k = 0; k < _currentStage.Tables.Count; k++)
			{
				Table table = _currentStage.Tables[k];
				if (table == null)
				{
					continue;
				}
				table.UpdateContactGraph();
				foreach (IContactNodeOwner owner in GetOwnersTouchingTable(table))
				{
					if (owner != null)
					{
						owner.TouchingTable = table;
					}
				}
			}
		}

		public async UniTask LoadLevelAsync(int levelIndex, System.Threading.CancellationToken cancellationToken = default)
		{
			_levelIndex = levelIndex;
			_levelData = await LevelLoader.GetLevelAsync(levelIndex).AttachExternalCancellation(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			if (_levelData?.stages == null || _levelData.stages.Count == 0 || _levelData.stages[0] == null)
				throw new InvalidOperationException("Level has no playable stages.");
			_remainingMoves = _levelData != null ? _levelData.moveCount : 0;
			_currentStageIndex = 0;
			_collectionName = await LevelCollectionHelper.GetCollectionToUseAsync().AttachExternalCancellation(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			_collectionVersion = (await LevelCollectionHelper.GetMetaDataForCollectionAsync(_collectionName).AttachExternalCancellation(cancellationToken))?.version ?? 0;
		}

		public LevelStage LoadStage(int stageIndex)
		{
			LevelStage levelStage = new LevelStage();
			GameObject stageRootObject = new GameObject(string.Format("Stage{0}", stageIndex));
			if (stagesRoot != null)
			{
				stageRootObject.transform.SetParent(stagesRoot, false);
			}
			levelStage.RootTransform = stageRootObject.transform;
			ObjectData objectData = LoadedObjectData;
			if (_levelData?.stages == null || stageIndex < 0 || stageIndex >= _levelData.stages.Count)
			{
				return levelStage;
			}
			LevelStageData stageData = _levelData.stages[stageIndex];
			if (stageData == null)
			{
				return levelStage;
			}
			Dictionary<int, Table> tablesById = new Dictionary<int, Table>();
			if (stageData.tables != null && objectData?.tablePrefab != null)
			{
				for (int i = 0; i < stageData.tables.Count; i++)
				{
					LevelTableData tableData = stageData.tables[i];
					GameObject tableObject = UnityEngine.Object.Instantiate(objectData.tablePrefab, stageRootObject.transform, false);
					tableObject.name = tableData != null ? string.Format("Table{0}", tableData.id) : string.Format("Table{0}", i);
					Table table = tableObject.GetComponent<Table>();
					if (tableData != null)
					{
						tableObject.transform.localPosition = new Vector3(tableData.pos.x, tableData.pos.y, tableData.pos.z);
						tableObject.transform.localRotation = new Quaternion(tableData.rot.x, tableData.rot.y, tableData.rot.z, tableData.rot.w);
						tableObject.transform.localScale = new Vector3(tableData.scl.x, tableData.scl.y, tableData.scl.z);
					}
					if (table == null)
					{
						continue;
					}
					if (tableData != null)
					{
						table.Id = tableData.id;
						table.SetId(tableData.id);
						table.LoadDataFromLevel(tableData);
						if (!tablesById.ContainsKey(tableData.id))
						{
							tablesById.Add(tableData.id, table);
						}
					}
					levelStage.Tables.Add(table);
				}
			}
			if (stageData.objects != null)
			{
				for (int j = 0; j < stageData.objects.Count; j++)
				{
					LevelObjectData levelObjectData = stageData.objects[j];
					if (levelObjectData == null)
					{
						continue;
					}
					GameObject objectPrefab = objectData?.Get(levelObjectData.type, levelObjectData.size);
					if (objectPrefab == null)
					{
						continue;
					}
					Transform parent = stageRootObject.transform;
					if (tablesById.TryGetValue(levelObjectData.tableId, out Table table) && table != null)
					{
						parent = table.transform;
					}
					GameObject objectInstance = UnityEngine.Object.Instantiate(objectPrefab, parent, false);
					objectInstance.name = string.Format("{0}:{1}", objectPrefab.name, j);
					objectInstance.transform.localPosition = new Vector3(levelObjectData.pos.x, levelObjectData.pos.y, levelObjectData.pos.z);
					objectInstance.transform.localRotation = new Quaternion(levelObjectData.rot.x, levelObjectData.rot.y, levelObjectData.rot.z, levelObjectData.rot.w);
					BaseObject baseObject = objectInstance.GetComponent<BaseObject>();
					if (baseObject == null)
					{
						continue;
					}
					baseObject.Initialize(levelObjectData);
					levelStage.LiveObjects.Add(baseObject);
				}
			}
			if (stageData.blockers != null && objectData?.blockerPrefab != null)
			{
				for (int k = 0; k < stageData.blockers.Count; k++)
				{
					LevelBlockerData levelBlockerData = stageData.blockers[k];
					if (levelBlockerData == null)
					{
						continue;
					}
					GameObject blockerObject = UnityEngine.Object.Instantiate(objectData.blockerPrefab, stageRootObject.transform, false);
					blockerObject.name = string.Format("Blocker{0}", k);
                    Gameplay.Obstacle
.Blocker blocker = blockerObject.GetComponent< Gameplay.Obstacle
.Blocker >();
					if (blocker == null)
					{
						continue;
					}
					blocker.SetData(levelBlockerData);
					blocker.Init();
					levelStage.Blockers.Add(blocker);
				}
			}
			return levelStage;
		}

		public void ClearBuiltStages()
		{
			if (stagesRoot == null)
			{
				return;
			}
			for (int i = stagesRoot.childCount - 1; i >= 0; i--)
			{
				Destroy(stagesRoot.GetChild(i).gameObject);
			}
		}

		public int GetStageCount()
		{
			return _levelData != null ? _levelData.GetStageCount() : 0;
		}

		public int GetLevelIndex()
		{
			return _levelIndex;
		}

		public LevelData GetLevelData()
		{
			return _levelData;
		}

		[IteratorStateMachine(typeof(_003CGetOwnersTouchingTable_003Ed__55))]
		public IEnumerable<IContactNodeOwner> GetOwnersTouchingTable(Table table)
		{
			if (table == null || _currentStage == null)
			{
				yield break;
			}
			HashSet<ContactNode> cluster = table.GetContactCluster();
			if (cluster == null || cluster.Count == 0)
			{
				yield break;
			}
			List<BaseObject> liveObjects = _currentStage.LiveObjects;
			if (liveObjects != null)
			{
				for (int i = 0; i < liveObjects.Count; i++)
				{
					BaseObject liveObject = liveObjects[i];
					if (liveObject?.ContactNode != null && cluster.Contains(liveObject.ContactNode))
					{
						yield return liveObject;
					}
				}
			}
			for (int j = 0; j < _liveBalls.Count; j++)
			{
				Ball liveBall = _liveBalls[j];
				if (liveBall?.ContactNode != null && cluster.Contains(liveBall.ContactNode))
				{
					yield return liveBall;
				}
			}
		}

		private List<BaseObject> GetLiveObjects()
		{
			return _currentStage != null ? _currentStage.LiveObjects : null;
		}

		private int GetRemainingObjectCount()
		{
			int count = _currentStage?.LiveObjects?.Count ?? 0;
			if (_levelData?.stages == null)
			{
				return count;
			}
			for (int i = _currentStageIndex + 1; i < _levelData.stages.Count; i++)
			{
				count += _levelData.stages[i]?.objects?.Count ?? 0;
			}
			return count;
		}

		private float GetRemainingObjectMass()
		{
			float totalMass = 0f;
			if (_currentStage?.LiveObjects != null)
			{
				for (int i = 0; i < _currentStage.LiveObjects.Count; i++)
				{
					BaseObject liveObject = _currentStage.LiveObjects[i];
					if (liveObject != null)
					{
						totalMass += BaseObject.GetMass(liveObject.objectType, liveObject.size.GetVolume());
					}
				}
			}
			if (_levelData?.stages != null)
			{
				for (int i = _currentStageIndex + 1; i < _levelData.stages.Count; i++)
				{
					List<LevelObjectData> objects = _levelData.stages[i]?.objects;
					if (objects == null)
					{
						continue;
					}
					for (int j = 0; j < objects.Count; j++)
					{
						LevelObjectData levelObjectData = objects[j];
						if (levelObjectData != null)
						{
							totalMass += BaseObject.GetMass(levelObjectData.type, levelObjectData.size.GetVolume());
						}
					}
				}
			}
			return totalMass;
		}

		private List<Table> GetTables()
		{
			return _currentStage != null ? _currentStage.Tables : null;
		}

		public void DestroyObject(BaseObject objectToDestroy)
		{
			if (objectToDestroy == null)
			{
				return;
			}
			if (_currentStage?.LiveObjects?.Remove(objectToDestroy) == true)
			{
				RecordElimination();
			}
			Destroy(objectToDestroy.gameObject);
		}


		private void InitializeEliminateRewards()
		{
			ResetEliminateRewards();
			_eliminateRewardTiers.Clear();
			_minEliminateRewardCount = int.MaxValue;
			var table = GF.DataTable.GetDataTable<EliminateReward>();
			if (table == null)
			{
				Log.Warning("EliminateReward table is not loaded; elimination rewards are disabled.");
				return;
			}
			foreach (var row in table.GetAllDataRows())
			{
				if (row.Count == null || row.Count.Length != 2 || row.Count[0] < 2
					|| row.Count[1] <= row.Count[0] || row.Reward == null || row.Reward.Length != 2
					|| float.IsNaN(row.Reward[0]) || float.IsInfinity(row.Reward[0])
					|| float.IsNaN(row.Reward[1]) || float.IsInfinity(row.Reward[1])
					|| row.Reward[0] <= 0f || row.Reward[1] < row.Reward[0])
				{
					Log.Warning("Invalid EliminateReward row: {0}", row.Id);
					continue;
				}
				_eliminateRewardTiers.Add(row);
				_minEliminateRewardCount = Mathf.Min(_minEliminateRewardCount, row.Count[0]);
			}
			_eliminateRewardTiers.Sort((a, b) => a.Count[0].CompareTo(b.Count[0]));
		}

		private void RecordElimination()
		{
			if (_eliminateRewardTiers.Count == 0) return;
			ProcessEliminateRewards();
			if (_eliminateCount == 0)
				_eliminateRewardSample = UnityEngine.Random.value;
			_eliminateCount++;
			_lastEliminateActivityTime = Time.time;
			if (!_eliminateRewardDirty) _eliminateRewardPendingSince = Time.time;
			_eliminateRewardDirty = true;
		}

		private void RecordEliminateRewardShot()
		{
			ProcessEliminateRewards();
			if (_eliminateRewardAwarded > 0f)
			{
				// Flush any remaining top-up, then let the new shot earn a fresh reward.
				ProcessEliminateRewards(true);
			}
			else if (_eliminateCount > 0)
			{
				// Shots below the threshold may still accumulate together.
				_lastEliminateActivityTime = Time.time;
			}
		}

		private void ProcessEliminateRewards(bool force = false)
		{
			if (_eliminateCount == 0) return;
			bool expired = Time.time - _lastEliminateActivityTime >= Mathf.Max(0.01f, eliminateRewardRoundIdleTime);
			// Feedback cadence does not close the round or discard small counts.
			if (_eliminateRewardDirty && (force || expired
				|| Time.time - _eliminateRewardPendingSince >= Mathf.Max(0.01f, eliminateRewardQuietTime)))
			{
				_eliminateRewardDirty = false;
				if (_eliminateCount > 1 && _eliminateCount >= _minEliminateRewardCount)
				{
					for (int i = _eliminateRewardTiers.Count - 1; i >= 0; i--)
					{
						var tier = _eliminateRewardTiers[i];
						if (_eliminateCount < tier.Count[0]
							|| (i < _eliminateRewardTiers.Count - 1 && _eliminateCount >= tier.Count[1])) continue;
						// Reuse one random percentile for this round; never reroll or reduce its reward.
						float total = (float)Math.Round(Mathf.Lerp(tier.Reward[0], tier.Reward[1], _eliminateRewardSample),
							3, MidpointRounding.AwayFromZero);
						total = Mathf.Max(total, _eliminateRewardAwarded);
						float delta = (float)Math.Round(total - _eliminateRewardAwarded, 3, MidpointRounding.AwayFromZero);
						if (delta > 0f)
						{
							bool playFlyAnimation = _eliminateRewardAwarded <= 0f;
							_eliminateRewardAwarded = total;
							GF.Event.Fire(this, RewardCollectedEventArgs.Create(delta, Vector3.zero,
								PlayerDataType.Diamond, playFlyAnimation));
						}
						break;
					}
				}
			}
			if (force || expired) ResetEliminateRewards();
		}

		private void ResetEliminateRewards()
		{
			_eliminateCount = 0;
			_lastEliminateActivityTime = 0f;
			_eliminateRewardPendingSince = 0f;
			_eliminateRewardSample = 0f;
			_eliminateRewardAwarded = 0f;
			_eliminateRewardDirty = false;
		}

		private void SetObjectMasses()
		{
			_currentStage?.LiveObjects?.ForEach(delegate(BaseObject x)
			{
				x?.SetObjectMass();
			});
		}

		private void PositionBackGroundCollider()
		{
			if (_currentStage?.LiveObjects == null || _currentStage.LiveObjects.Count == 0)
			{
				return;
			}
			float maxZ = float.MinValue;
			for (int i = 0; i < _currentStage.LiveObjects.Count; i++)
			{
				BaseObject liveObject = _currentStage.LiveObjects[i];
				if (liveObject != null && liveObject.transform.position.z > maxZ)
				{
					maxZ = liveObject.transform.position.z;
				}
			}
			if (maxZ == float.MinValue)
			{
				maxZ = 0f;
			}
			if (ground != null)
			{
				Vector3 groundPosition = ground.transform.position;
				groundPosition.z = maxZ;
				ground.transform.position = groundPosition;
				TableEndLineZ = groundPosition.z;
			}
			else
			{
				TableEndLineZ = maxZ;
			}
			if (backgroundPrefab != null)
			{
				Vector3 backgroundPosition = backgroundPrefab.transform.position;
				backgroundPosition.z = maxZ;
				backgroundPrefab.transform.position = backgroundPosition;
			}
		}

		public void UpdateRemainingMoves()
		{
			PlayUIForm.SetRemainingBallCount(_remainingMoves);
		}

		private void CheckForClick()
		{
			bool pointerHeld = Input.GetMouseButton(0);
			bool pointerReleased = Input.GetMouseButtonUp(0);
			if (!pointerHeld && !pointerReleased)
			{
				_gameplayPointerStarted = false;
				return;
			}
			if (_gameplayLocked || _remainingMoves < 1
				|| (ServiceLocator.Get<TutorialManager>()?.AnyActiveTutorial() ?? false)
				|| UIUtils.IsPointerOverUIElement()
				|| ScreenUtils.IsInTopUnsafeArea(Input.mousePosition))
			{
				_gameplayPointerStarted = false;
				return;
			}
			// A UI click must not become a shot when its popup closes on release.
			if (Input.GetMouseButtonDown(0))
			{
				_gameplayPointerStarted = true;
			}
			if (!_gameplayPointerStarted)
			{
				return;
			}
			if (pointerReleased)
			{
				_gameplayPointerStarted = false;
			}
			OnClick();
		}

		public void OnClick()
		{
			if (cannon == null)
			{
				return;
			}
			if (Input.GetMouseButton(0))
			{
				cannon.LookAtTarget();
			}
			if (Input.GetMouseButtonUp(0) && cannon.TryShoot(out Ball _))
			{
				RecordEliminateRewardShot();
				_remainingMoves--;
				UpdateRemainingMoves();
			}
		}

		[IteratorStateMachine(typeof(_003CTransitToNextStage_003Ed__66))]
		private IEnumerator TransitToNextStage(Action onComplete)
		{
			LevelStage previousStage = _currentStage;
			if (stageSignText != null)
			{
				stageSignText.text = string.Format("Stage {0}/{1}", _currentStageIndex + 1, _levelData != null ? _levelData.GetStageCount() : 1);
			}
			stageSignAnimator?.Play("StageSignOpenAnim");
			yield return new WaitForSeconds(0.7f);
			Transform previousRoot = previousStage?.RootTransform;
			if (previousRoot != null)
			{
				previousRoot.DOScale(Vector3.one * 0.02f, 0.3f).SetEase(stageScaleDownCurve);
			}
			yield return new WaitForSeconds(0.3f);
			previousStage?.Deactivate();
			// Returning a ball removes it from _liveBalls, so traverse backwards.
			for (int i = _liveBalls.Count - 1; i >= 0; i--)
			{
				cannon.OnBallDestroyed(_liveBalls[i]);
			}
			_liveBalls.Clear();
			if (previousRoot != null)
			{
				UnityEngine.Object.Destroy(previousRoot.gameObject);
			}
			_currentStage = LoadStage(_currentStageIndex);
			_currentStage?.Activate();
			if (_currentStage?.RootTransform != null)
			{
				_currentStage.RootTransform.localScale = Vector3.one * 0.02f;
				_currentStage.RootTransform.DOScale(Vector3.one, 0.6f).SetEase(stageScaleUpCurve);
			}
			_levelStarted = false;
			yield return new WaitForSeconds(0.6f);
			stageSignAnimator?.Play("StageSignCloseAnim");
			yield return new WaitForSeconds(0.5f);
			onComplete?.Invoke();
		}

		private void CheckForComplete()
		{
			if (_gameplayLocked)
			{
				return;
			}
			if (ServiceLocator.Get<TutorialManager>()?.AnyActiveTutorial() ?? false)
			{
				return;
			}
			if (_endBlockerCount > 0)
			{
				return;
			}
			if (_currentStage?.LiveObjects == null || _currentStage.LiveObjects.Count > 0)
			{
				return;
			}
			CompleteStage();
		}

		public void CompleteStage()
		{
			// All stage objects are gone: settle without waiting for the quiet interval.
			ProcessEliminateRewards(true);
			ResetEliminateRewards();
			_gameplayLocked = true;
			_currentStageIndex++;
			if (_currentStageIndex < GetStageCount())
			{
				StartCoroutine(TransitToNextStage(delegate
				{
					AfterStageSetup();
					StartCoroutine(FinishStageTransitionAfterRocket());
				}));
				return;
			}
			WinLevel();
		}

		private void WinLevel()
		{
			_gameplayLocked = true;
			CancelPendingWinPopup();
			_analyticsHelper?.LevelWin(_remainingMoves, _currentStageIndex);
			MenuController.IsAfterWin = true;
			StreakHelper.OnLevelWon(_levelIndex, _activeStreakCount);
			PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
			playerData.LevelId = Mathf.Max(playerData.LevelId, _levelIndex + 1);
			playerData.Save();
			if (SaveService.Data != null)
			{
				SaveService.Save();
			}
			LifeHelper.AddLife(1);
			_pendingWinPopupRoutine = DelayedWorker.CallAfter(0.5f, delegate
			{
				_pendingWinPopupRoutine = null;
				if (!this || !isActiveAndEnabled)
				{
					return;
				}

				System.Action showLevelComplete = () =>
				{
					if (!this || !isActiveAndEnabled) return;
					UIParams uiParams = UIParams.Create();
					uiParams.Set<VarInt32>(LevelCompleteUIForm.P_LevelId, _levelIndex);
					VarAction onNext = ReferencePool.Acquire<VarAction>();
					onNext.Value = () => SceneHandler.LoadScene(SceneType.Gameplay, false);
					uiParams.Set<VarAction>(LevelCompleteUIForm.P_OnNext, onNext);
					GF.UI.OpenUIForm(UIViews.LevelCompleteUIForm, uiParams);
				};
				if (PcardGiftRewardSession.Count > 0)
				{
					UIParams giftParams = UIParams.Create();
					giftParams.CloseCallback = _ => showLevelComplete();
					if (GF.UI.OpenUIForm(UIViews.PcardGiftUIForm, giftParams) >= 0) return;
				}
				showLevelComplete();
			});
		}

		private void CheckForFail()
		{
			if (_gameplayLocked)
			{
				return;
			}
			if (_remainingMoves >= 1)
			{
				return;
			}
			if (_endBlockerCount > 0)
			{
				return;
			}
			if (AreTableObjectsSettledForFail())
			{
				Fail();
			}
		}

		private bool AreTableObjectsSettledForFail()
		{
			if (_currentStage?.LiveObjects == null)
			{
				return false;
			}
			bool hasTableObjects = false;
			for (int i = 0; i < _currentStage.LiveObjects.Count; i++)
			{
				BaseObject baseObject = _currentStage.LiveObjects[i];
				// Table contact includes objects supported by other objects in the stack.
				if (baseObject == null || baseObject.BeingDestroyed || baseObject.TouchingTable == null)
				{
					continue;
				}
				hasTableObjects = true;
				if (baseObject.HasSignificantMovement())
				{
					return false;
				}
			}
			// An empty table may be awaiting the last falling targets and stage completion.
			return hasTableObjects;
		}

		public void Fail()
		{
			if (_gameplayLocked || _currentStage?.LiveObjects == null || _currentStage.LiveObjects.Count == 0)
			{
				return;
			}
			_gameplayLocked = true;
			MenuController.IsAfterWin = false;
			UIParams uiParams = UIParams.Create(allowEscape: false);
			VarAction onRefill = ReferencePool.Acquire<VarAction>();
			onRefill.Value = () =>
			{
				if (this && isActiveAndEnabled) OnEgoBought(5);
			};
			uiParams.Set<VarAction>(AddBallsUIForm.P_OnRefill, onRefill);
			VarAction onQuit = ReferencePool.Acquire<VarAction>();
			onQuit.Value = () =>
			{
				if (this && isActiveAndEnabled) OnEgoRejected();
			};
			uiParams.Set<VarAction>(AddBallsUIForm.P_OnQuit, onQuit);
			GF.UI.OpenUIForm(UIViews.AddBallsUIForm, uiParams);
			_analyticsHelper?.LevelEgoShow(GetRemainingObjectCount(), GetRemainingObjectMass(), _currentStageIndex);
		}

		public void OnEgoRejected()
		{
			ProcessEliminateRewards(true);
			StreakHelper.OnLevelLostOrAbandoned();
			_analyticsHelper?.LevelFail(GetRemainingObjectCount(), GetRemainingObjectMass(), _remainingMoves, FailReason.OutOfMoves, _currentStageIndex);
			_gameplayLocked = true;
			MenuController.IsAfterWin = false;
		}

		public void AddToLiveBalls(Ball ball)
		{
			if (ball != null && !_liveBalls.Contains(ball))
			{
				_liveBalls.Add(ball);
			}
		}

		public void RemoveFromLiveBalls(Ball ball)
		{
			if (ball != null)
			{
				_liveBalls.Remove(ball);
			}
		}

		public void IncreaseEndBlocker()
		{
			_endBlockerCount++;
		}

		public void DecreaseEndBlocker()
		{
			_endBlockerCount--;
		}

		public void OnEgoBought(int moves)
		{
			_gameplayLocked = false;
			_remainingMoves += moves;
			UpdateRemainingMoves();
			_analyticsHelper?.OnEgoBought(moves);
		}

		public void OnGameQuit()
		{
			ProcessEliminateRewards(true);
			StreakHelper.OnLevelLostOrAbandoned();
			_analyticsHelper?.LevelFail(GetRemainingObjectCount(), GetRemainingObjectMass(), _remainingMoves, FailReason.Quit, _currentStageIndex);
			_gameplayLocked = true;
			MenuController.IsAfterWin = false;
		}

		public bool IsFinished()
		{
			return _gameplayLocked;
		}

		public bool ShouldSkipMenuAfterWin()
		{
			return _levelIndex < 19;
		}

		public int GetActiveStreakCount()
		{
			return _activeStreakCount;
		}

		public int GetCoinRewardForLevel()
		{
			if (_levelData == null)
			{
				return 0;
			}
			return _levelData.difficulty switch
			{
				LevelDifficulty.Normal => 20,
				LevelDifficulty.Hard => 50,
				LevelDifficulty.SuperHard => 100,
				_ => 0
			};
		}

		protected virtual void OnDisable()
		{
			GameplayAssetCache.CancelBackground();
			CleanupTransientState();
		}

		protected override void OnDestroy()
		{
			GameplayAssetCache.CancelBackground();
			_gameplayAssets?.Dispose();
			_gameplayAssets = null;
			CleanupTransientState();
			base.OnDestroy();
		}

		private void CleanupTransientState()
		{
			ResetEliminateRewards();
			_gameplayPointerStarted = false;
			StopAllCoroutines();
			CancelPendingWinPopup();
			boosterTint?.DOKill();
			SceneHandler.OnLoadingDisappear -= OnGameplayExposed;
		}

		private void CancelPendingWinPopup()
		{
			if (_pendingWinPopupRoutine == null)
			{
				return;
			}
			DelayedWorker.Kill(_pendingWinPopupRoutine);
			_pendingWinPopupRoutine = null;
		}
	}
}
