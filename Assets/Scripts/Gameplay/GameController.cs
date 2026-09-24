using ABTesting;
using Core;
using DG.Tweening;
using Gameplay.Collisions;
using Gameplay.GameplayTutorial;
using Gameplay.Level;
using Gameplay.Objects;
using Gameplay.Obstacle;
using Gameplay.Particles;
using Gameplay.UI;
using Inventory;
using Inventory.TimedInventory;
using Level;
using Life;
using LocalSave;
using Menu;
using Popups;
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

		private const int RocketPerPrelevel = 3;

		[SerializeField]
		private Cannon cannon;

		[SerializeField]
		private RectTransform topPanelRootTransform;

		[SerializeField]
		private BallSignController ballSignController;

		[SerializeField]
		private GameplaySettingsController gameplaySettingsController;

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

		private float _outOfMovesTime = -1f;

		private bool _gameplayLocked;

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

		[NonSerialized]
		public float TableEndLineZ;

		private void Start()
		{
			_analyticsHelper = new GameplayAnalyticsHelper();
			_levelIndex = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
			_currentStageIndex = 0;
			LoadLevel(_levelIndex);
			_currentStage = LoadStage(_currentStageIndex);
			_activeStreakCount = StreakHelper.GetStreakCount();
			StreakHelper.OnGameplayLevelStarted(_activeStreakCount);
			ballSignController?.Init(_levelData);
			gameplaySettingsController?.Init(_levelData);
			List<PrelevelBoosterType> selection = PrelevelBoosterHelper.GetSelection();
			if (selection != null)
			{
				for (int i = 0; i < selection.Count; i++)
				{
					if (selection[i] != PrelevelBoosterType.Rocket)
					{
						continue;
					}
					if (_levelIndex != PrelevelBoosterHelper.GetUnlockLevelForType(PrelevelBoosterType.Rocket))
					{
						InventoryHelper.TrySpend(new InventoryPayload(InventoryItemType.PrelevelRocket, 1), InventorySpendSource.Prelevel);
					}
					_prelevelRocketSelected = true;
				}
			}
			PrelevelBoosterHelper.ClearSelection();
			AfterLevelSetup();
			AfterStageSetup();
			if (!SceneHandler.IsInLoading)
			{
				OnGameplayExposed();
				return;
			}
			_gameplayLocked = true;
			SceneHandler.OnLoadingDisappear += OnGameplayExposed;
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
			UpdateRemainingMoves();
			cannon?.InitializeBallPool(5);
			cannon?.PrewarmBalls();
			ServiceLocator.Get<ParticleController>()?.Initialize();
			if (!TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife))
			{
				ServiceLocator.Get<LifeHelper>()?.LoseLife();
			}
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

		public void LoadLevel(int levelIndex)
		{
			_levelIndex = levelIndex;
			_levelData = LevelLoader.GetLevel(levelIndex);
			_remainingMoves = _levelData != null ? _levelData.moveCount : 0;
			_currentStageIndex = 0;
			_collectionName = LevelCollectionHelper.GetCollectionToUse();
			_collectionVersion = LevelCollectionHelper.GetMetaDataForCollection(_collectionName)?.version ?? 0;
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
			ObjectData objectData = Resources.Load<ObjectData>("ObjectData");
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
			_currentStage?.LiveObjects?.Remove(objectToDestroy);
			Destroy(objectToDestroy.gameObject);
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
			if (ballSignController != null)
			{
				ballSignController.UpdateRemainingMoves(_remainingMoves);
			}
		}

		private void CheckForClick()
		{
			if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
			{
				return;
			}
			if (ServiceLocator.Get<TutorialManager>()?.AnyActiveTutorial() ?? false)
			{
				return;
			}
			if (UIUtils.IsPointerOverUIElement())
			{
				return;
			}
			if (ServiceLocator.Get<PopupController>()?.AreThereOpenPopups() ?? false)
			{
				return;
			}
			if (ScreenUtils.IsInTopUnsafeArea(Input.mousePosition))
			{
				return;
			}
			if (_remainingMoves < 1)
			{
				return;
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
			ballSignController?.Fill(_currentStageIndex, 0.7f);
			yield return new WaitForSeconds(0.7f);
			Transform previousRoot = previousStage?.RootTransform;
			if (previousRoot != null)
			{
				previousRoot.DOScale(Vector3.one * 0.02f, 0.3f).SetEase(stageScaleDownCurve);
			}
			yield return new WaitForSeconds(0.3f);
			previousStage?.Deactivate();
			for (int i = 0; i < _liveBalls.Count; i++)
			{
				_liveBalls[i]?.Recycle();
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
			_outOfMovesTime = -1f;
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
			ServiceLocator.Get<LifeHelper>()?.AddLife(1);
			int reward = GetCoinRewardForLevel();
			MenuController.CoinToCollect = reward;
			if (reward > 0)
			{
				InventoryHelper.AddAmount(new InventoryPayload(InventoryItemType.Coin, reward), true, InventoryEarnSource.LevelWin);
			}
			if (topPanelRootTransform != null)
			{
				topPanelRootTransform.DOKill();
				topPanelRootTransform.DOAnchorPosY(500f, 0.4f, false).SetEase(Ease.InBack).SetDelay(0.3f);
			}
			_pendingWinPopupRoutine = DelayedWorker.CallAfter(0.5f, delegate
			{
				_pendingWinPopupRoutine = null;
				if (!this || !isActiveAndEnabled)
				{
					return;
				}
				ServiceLocator.Get<PopupController>()?.Open(PopupType.Win, null);
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
				_outOfMovesTime = -1f;
				return;
			}
			if (_outOfMovesTime < 0f)
			{
				_outOfMovesTime = Time.time;
			}
			if (_endBlockerCount > 0)
			{
				return;
			}
			if (Time.time - _outOfMovesTime >= 15f || AreAllBodiesSettledForFail())
			{
				Fail();
			}
		}

		private bool AreAllBodiesSettledForFail()
		{
			if (_currentStage?.LiveObjects == null)
			{
				return false;
			}
			for (int i = 0; i < _currentStage.LiveObjects.Count; i++)
			{
				BaseObject baseObject = _currentStage.LiveObjects[i];
				if (baseObject != null && baseObject.HasSignificantMovement())
				{
					return false;
				}
			}
			if (_liveBalls != null)
			{
				for (int j = 0; j < _liveBalls.Count; j++)
				{
					Ball ball = _liveBalls[j];
					if (ball != null && ball.HasSignificantMovement())
					{
						return false;
					}
				}
			}
			return true;
		}

		public void Fail()
		{
			_gameplayLocked = true;
			MenuController.IsAfterWin = false;
			MenuController.CoinToCollect = 0;
			ServiceLocator.Get<PopupController>()?.Open(PopupType.Ego, new Dictionary<string, object>
			{
				["streakCount"] = _activeStreakCount
			});
			_analyticsHelper?.LevelEgoShow(GetRemainingObjectCount(), GetRemainingObjectMass(), _currentStageIndex);
		}

		public void OnEgoRejected()
		{
			StreakHelper.OnLevelLostOrAbandoned();
			_analyticsHelper?.LevelFail(GetRemainingObjectCount(), GetRemainingObjectMass(), _remainingMoves, FailReason.OutOfMoves, _currentStageIndex);
			_gameplayLocked = true;
			MenuController.IsAfterWin = false;
			MenuController.CoinToCollect = 0;
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
			_outOfMovesTime = -1f;
			UpdateRemainingMoves();
			_analyticsHelper?.OnEgoBought(moves);
		}

		public void OnGameQuit()
		{
			StreakHelper.OnLevelLostOrAbandoned();
			_analyticsHelper?.LevelFail(GetRemainingObjectCount(), GetRemainingObjectMass(), _remainingMoves, FailReason.Quit, _currentStageIndex);
			_gameplayLocked = true;
			MenuController.IsAfterWin = false;
			MenuController.CoinToCollect = 0;
		}

		public bool IsFinished()
		{
			return _gameplayLocked;
		}

		public bool ShouldSkipMenuAfterWin()
		{
			return _levelIndex < 19;
		}

		public bool ShouldTryShowInterAfterWin()
		{
			return ControlledKeyHelper.GetBool(ControlledKeys.InterEnabled);
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
			CleanupTransientState();
		}

		protected override void OnDestroy()
		{
			CleanupTransientState();
			base.OnDestroy();
		}

		private void CleanupTransientState()
		{
			StopAllCoroutines();
			CancelPendingWinPopup();
			topPanelRootTransform?.DOKill();
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
