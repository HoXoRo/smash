using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework;
using System;

namespace ArrowMaze
{
    /// <summary>
    /// 关卡管理器 - 管理关卡加载、箭头生成、游戏状态、倒计时与复活
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        private ArrowLevelData m_CurrentLevelData;
        private PlayerDataModel m_PlayerData;
        private List<int> m_ArrowLineEntityIds = new List<int>(); // 存储箭头实体的ID
        private int m_CurrentLevelId; // 当前关卡ID，用于查表
        private HashSet<int> m_RewardArrowIndices = new HashSet<int>(); // 本关随机选中的奖励箭头索引
        private int m_RewardArrowEliminateCount; // 本关已消除的特殊奖励箭头累计次数
        private int m_EliminationComboCount; // 本关连续消除计数（用于 Combo UI，达阈值后归零）
        private readonly HashSet<int> m_RewardGrantedArrowIds = new HashSet<int>();

        private int m_Lives = 3; // 生命数
        private bool m_IsGameOver = false;
        private bool m_IsGameWon = false;
        private bool m_DeferCheckGameWin;
        private bool m_BlockGameplayInput;

        public static bool IS_NEED_COUNTDOWN = false; // 是否需要倒计时
        private float m_RemainingTime; // 剩余倒计时（秒）
        private bool m_CountdownActive; // 倒计时是否运行中
        private bool m_CountdownPaused; // 应用暂停时（如看广告）不继续计时

        private float INITIAL_COUNTDOWN = 60f; // 初始5分钟
        private float REVIVE_TIME_BONUS = 30f; // 看广告复活增加30秒

        // ArrowLine预制体资源路径（需要在资源系统中配置）
        private const string ARROW_LINE_PREFAB_PATH = "ArrowLine";
        private const string ARROW_LINE_ENTITY_GROUP = "ArrowLine"; // Entity组名称
        private const string FINGER_PREFAB_PATH = "Finger";
        private const int DefaultEliminationRewardCount = 10;
        
        [SerializeField]
        private GameObject m_DianjiPrefab;

        Transform m_ClickEffectRoot;
        const float ClickEffectMinDuration = 0.6f;
        const float ClickEffectDestroyTimeout = 3f;
        const int ClickEffectSortingOrder = 50;
        /// <summary> 与 ArrowLine / GameCamera CullingMask 一致（WorldUI = Layer 3）。 </summary>
        const int GameplayLayer = 3;

        /// <summary>
        /// 连续消除弹出奖励弹窗的次数阈值（读 GameConfig.EliminationRewardCount）。
        /// </summary>
        private int GetEliminationRewardCount()
        {
            int count = GF.Config.GetInt("EliminationRewardCount", DefaultEliminationRewardCount);
            return count > 0 ? count : DefaultEliminationRewardCount;
        }

        static readonly Vector3 FingerHeadWorldOffset = new Vector3(-0.08f, 0f, 0f);

        int m_FingerEntityId;
        Transform m_FingerTransform;
        ArrowLineEntity m_FingerGuideTarget;
        bool m_FingerGuideVisible;
        bool m_FingerCreating;

        [SerializeField] private ArrowAdvancedSkinDatabase m_ArrowAdvancedSkinDatabase;

        private WatermelonPathExtension m_WatermelonPathExtension;

        /// <summary> 本关已开始移动的箭头数，用于循环播放 do/re/mi/fa/so/la/si 音效（clean1～clean7） </summary>
        private int m_MoveStartCount;

        const float AutoHintDelaySec = 8f;
        float m_TimeWithoutSuccessfulRemove;

        /// <summary>
        /// 当前生命数
        /// </summary>
        public int Lives => m_Lives;

        /// <summary>
        /// 剩余倒计时（秒）
        /// </summary>
        public float RemainingTime => m_RemainingTime;

        /// <summary>
        /// 是否游戏结束
        /// </summary>
        public bool IsGameOver => m_IsGameOver;

        /// <summary>
        /// 是否胜利
        /// </summary>
        public bool IsGameWon => m_IsGameWon;
        public bool IsGameplayInputBlocked => m_BlockGameplayInput || m_IsGameOver || m_IsGameWon;

        /// <summary>
        /// 箭头数量
        /// </summary>
        public int ArrowCount => m_ArrowLineEntityIds.Count;

        public int AllArrowCount => m_CurrentLevelData?.arrows?.Count ?? 0;

        public void SetGameplayInputBlocked(bool blocked)
        {
            m_BlockGameplayInput = blocked;
        }

        public void RequestCheckGameWin()
        {
            CheckGameWin();
        }

        private async void InitArrowAdvancedSkinDatabase()
        {
            m_ArrowAdvancedSkinDatabase = await ArrowAdvancedSkinDatabase.GetInstanceSync();
        }

        private void Start()
        {
            InitArrowAdvancedSkinDatabase();
        }

        // 游戏进行中，玩家没有成功移除箭头的时间累计15s，显示手指引导
        public void AutoHint()
        {
            foreach (var e in GetArrowEntities())
            {
                if (e != null && e.CanAdvance())
                {
                    ShowFingerGuideAt(e);
                    break;
                }
            }
        }

        void HideAllFingerGuides()
        {
            HideFingerGuide();
        }

        void EnsureFingerGuide()
        {
            if (m_FingerTransform != null || m_FingerCreating) return;
            if (m_FingerEntityId != 0 && GF.Entity.HasEntity(m_FingerEntityId))
            {
                var entity = GF.Entity.GetEntity(m_FingerEntityId);
                m_FingerTransform = entity?.Logic?.CachedTransform;
                return;
            }

            m_FingerCreating = true;
            var eParams = EntityParams.Create();
            eParams.ParentTransform = transform;
            eParams.OnShowCallback = entity =>
            {
                m_FingerCreating = false;
                m_FingerTransform = entity.CachedTransform;
                entity.gameObject.SetActive(false);
                if (m_FingerGuideVisible)
                    UpdateFingerGuidePosition();
            };
            m_FingerEntityId = GF.Entity.ShowEntity<FingerGuideEntity>(
                FINGER_PREFAB_PATH,
                Const.EntityGroup.ArrowLine,
                eParams);
        }

        void ShowFingerGuideAt(ArrowLineEntity arrow)
        {
            if (arrow == null) return;
            EnsureFingerGuide();
            m_FingerGuideTarget = arrow;
            m_FingerGuideVisible = true;
            UpdateFingerGuidePosition();
            if (m_FingerTransform != null)
            {
                ApplyFingerDepth(m_FingerTransform);
                m_FingerTransform.gameObject.SetActive(true);
            }
        }

        public void HideFingerGuide()
        {
            m_FingerGuideVisible = false;
            m_FingerGuideTarget = null;
            if (m_FingerTransform != null)
                m_FingerTransform.gameObject.SetActive(false);
        }

        void UpdateFingerGuidePosition()
        {
            if (!m_FingerGuideVisible || m_FingerGuideTarget == null || m_FingerTransform == null) return;
            Transform head = m_FingerGuideTarget.m_HeadVisual != null
                ? m_FingerGuideTarget.m_HeadVisual
                : m_FingerGuideTarget.transform;
            m_FingerTransform.position = head.position + FingerHeadWorldOffset;
            ApplyFingerDepth(m_FingerTransform);
        }

        static void ApplyFingerDepth(Transform fingerTransform)
        {
            if (fingerTransform == null) return;
            Vector3 pos = fingerTransform.position;
            fingerTransform.position = new Vector3(pos.x, pos.y, -0.05f);
        }

        void DestroyFingerGuide()
        {
            HideFingerGuide();
            if (m_FingerEntityId != 0)
            {
                GF.Entity.HideEntitySafe(m_FingerEntityId);
                m_FingerEntityId = 0;
            }
            m_FingerTransform = null;
            m_FingerCreating = false;
        }

        void PauseWatermelonPathOnGameOver()
        {
            m_WatermelonPathExtension?.PauseForGameOver();
        }

        void ResumeWatermelonPathAfterRevive()
        {
            m_WatermelonPathExtension?.ResumeAfterGameOver();
        }

        void ResetAutoHintTimer()
        {
            m_TimeWithoutSuccessfulRemove = 0f;
            HideAllFingerGuides();
        }

        /// <summary>
        /// 是否处于引导 1 待完成阶段（与 CheckGuide 条件一致）。
        /// </summary>
        bool IsGuide1Pending()
        {
            if (m_PlayerData?.CompleteGuideIds == null || m_PlayerData.CompleteGuideIds.Contains(1))
                return false;
            if (m_PlayerData.CompleteGuideIds.Contains(4))
                return false;
            // 引导3改到第一关过关后，第一关玩法引导不再等待提现引导
            return m_PlayerData.LevelId == 1;
        }

        bool HasAdvanceableArrow()
        {
            foreach (var e in GetArrowEntities())
            {
                if (e != null && e.CanAdvance())
                    return true;
            }

            return false;
        }

        void UpdateAutoHintTimer()
        {
            if (m_IsGameOver || m_IsGameWon) return;
            if (m_ArrowLineEntityIds.Count == 0) return;
            if (m_CountdownPaused) return;

            var cameraController = ArrowMazeManager.Instance != null ? ArrowMazeManager.Instance.CameraController : null;
            if (cameraController != null && cameraController.IsEntranceAnimating) return;

            bool skipCd = IsGuide1Pending();
            if (!skipCd)
            {
                m_TimeWithoutSuccessfulRemove += Time.deltaTime;
                if (m_TimeWithoutSuccessfulRemove < AutoHintDelaySec) return;
            }
            else if (m_FingerGuideVisible)
                return;

            m_TimeWithoutSuccessfulRemove = 0f;
            if (HasAdvanceableArrow())
                AutoHint();
        }

        /// <summary>
        /// 获取当前仍存在的所有箭头实体（用于自动模拟点击等）
        /// </summary>
        public List<ArrowLineEntity> GetArrowEntities()
        {
            var list = new List<ArrowLineEntity>();
            foreach (int id in m_ArrowLineEntityIds)
            {
                if (!GF.Entity.HasEntity(id)) continue;
                var entity = GF.Entity.GetEntity(id);
                var arrow = entity?.Logic as ArrowLineEntity;
                if (arrow != null)
                    list.Add(arrow);
            }
            return list;
        }

        /// <summary>
        /// 是否还有箭头正在移动（用于自动模拟时等待一步完成）
        /// </summary>
        public bool AnyArrowMoving()
        {
            foreach (int id in m_ArrowLineEntityIds)
            {
                if (!GF.Entity.HasEntity(id)) continue;
                var entity = GF.Entity.GetEntity(id);
                var arrow = entity?.Logic as ArrowLineEntity;
                if (arrow != null && arrow.IsMoving)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 道具1：自动移除一个箭头。在场景内找一个可直接移除的箭头并模拟点击触发其移动逻辑。
        /// </summary>
        /// <returns>是否成功移除（若无可移除箭头则返回 false，调用方可提示道具使用失败）</returns>
        public bool TryUseItemAutoRemoveArrow()
        {
            var entities = GetArrowEntities();
            foreach (var e in entities)
            {
                if (e != null && e.CanAdvance())
                {
                    e.TryAdvance();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 道具2：显示所有引导线。将所有未移动的箭头的引导线显示出来，点击任意箭头后隐藏全部。
        /// </summary>
        public void ActivateShowAllGuideLines()
        {
            ArrowLineEntity.ShowAllGuideLines = true;
        }

        /// <summary>
        /// 道具3：强制消除箭头。使用后下一次点击的箭头将被无视规则强制擦除（不触发移动，直接释放网格，1 秒内从尾到头消失）。
        /// </summary>
        public void ActivateForceEliminateNextClick()
        {
            ArrowLineEntity.ForceEliminateNextClick = true;
        }

        /// <summary>
        /// 应用箭头彩色/默认色模式：设置全局开关并对当前场景中所有非特殊货币箭头重新上色。
        /// 默认皮肤下：夜间模式用白色，日间用黑色；彩色皮肤不受日/夜影响。
        /// </summary>
        /// <param name="useColorful">true=彩色（随机色板），false=默认皮肤</param>
        /// <param name="isNightMode">true=夜间（默认皮肤用白），false=日间（默认皮肤用黑）</param>
        public void ApplyArrowColorMode(bool useColorful, bool isNightMode)
        {
            ArrowLineEntity.UseColorfulArrows = useColorful;
            ArrowLineEntity.IsNightMode = isNightMode;
            var entities = GetArrowEntities();
            foreach (var arrow in entities)
            {
                if (arrow == null || arrow.IsRewardArrow) continue;
                arrow.SetArrowColor(useColorful ? ArrowLineEntity.GetRandomColorfulColor() : isNightMode ? Color.white : Color.black);
            }
        }

        /// <summary>
        /// 加载关卡
        /// </summary>
        public void LoadLevel(ArrowLevelData levelData)
        {
            if (levelData == null)
            {
                Log.Error("关卡数据为空");
                return;
            }
            INITIAL_COUNTDOWN = GF.Config.GetFloat("GameTimeOut");
            REVIVE_TIME_BONUS = GF.Config.GetFloat("ReviveAddTime");
            m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
            ArrowLineEntity.UseColorfulArrows = m_PlayerData.UseColorfulArrows;
            ArrowLineEntity.IsNightMode = m_PlayerData.IsNightMode;
            m_CurrentLevelData = levelData;
            m_Lives = 3;
            m_IsGameOver = false;
            m_IsGameWon = false;
            m_DeferCheckGameWin = false;
            m_BlockGameplayInput = false;
            m_RemainingTime = INITIAL_COUNTDOWN;
            m_CountdownActive = true;
            m_RewardArrowEliminateCount = 0;
            m_EliminationComboCount = 0;
            m_RewardGrantedArrowIds.Clear();

            // 解析关卡ID并分配奖励箭头，不分配奖励箭头
            // ParseLevelIdAndAssignRewardArrows(levelData);

            // 清理旧箭头
            ClearAllArrows();
            DestroyFingerGuide();
            m_MoveStartCount = 0;
            m_TimeWithoutSuccessfulRemove = 0f;
            ArrowLineEntity.ShowAllGuideLines = false;
            ArrowLineEntity.ForceEliminateNextClick = false;
            ClearClickEffects();

            // 重建网格
            if (ArrowMazeManager.Instance != null && ArrowMazeManager.Instance.GridManager != null)
            {
                ArrowMazeManager.Instance.GridManager.RebuildGrid(levelData);
            }

            SetupWatermelonPathExtension();

            // 生成所有箭头（异步）
            SpawnAllArrows();

            // 设置相机初始缩放与范围，规则见 CameraController.SetupForLevel
            if (ArrowMazeManager.Instance != null && ArrowMazeManager.Instance.CameraController != null)
            {
                if (ArrowMazeManager.Instance.GridManager != null)
                {
                    GridManager gm = ArrowMazeManager.Instance.GridManager;
                    Bounds gridBounds = gm.GetGridBounds();
                    if (m_WatermelonPathExtension != null
                        && m_WatermelonPathExtension.TryGetLoopWorldBounds(out Bounds loopBounds))
                    {
                        gridBounds.Encapsulate(loopBounds);
                    }
                    GridDef gridDef = gm.GridDef;
                    int gridCols = gridDef != null ? gridDef.width : -1;
                    int gridRows = gridDef != null ? gridDef.height : -1;
                    ArrowMazeManager.Instance.CameraController.SetupForLevel(levelData.levelName, gridBounds, gridCols, gridRows);
                }
            }

            EnsureFingerGuide();

            // 箭头为异步生成，延迟至实体就绪后再检查引导
            StartCoroutine(CheckGuideWhenArrowsReady());

            Log.Info($"关卡加载完成: {levelData.levelName}, 箭头数量: {m_ArrowLineEntityIds.Count}");
        }

        private void SetupWatermelonPathExtension()
        {
            if (!WatermelonPathExtension.IsExtensionEnabled())
            {
                if (m_WatermelonPathExtension != null)
                    m_WatermelonPathExtension.EndLevel();
                return;
            }

            if (m_WatermelonPathExtension == null)
                m_WatermelonPathExtension = gameObject.GetComponent<WatermelonPathExtension>();
            if (m_WatermelonPathExtension == null)
                m_WatermelonPathExtension = gameObject.AddComponent<WatermelonPathExtension>();
            m_WatermelonPathExtension.BeginLevel(this);
        }

        /// <summary>
        /// 等待箭头实体异步生成完成后再检查引导
        /// </summary>
        private IEnumerator CheckGuideWhenArrowsReady()
        {
            int expectedCount = m_CurrentLevelData?.arrows?.Count ?? 0;
            if (expectedCount == 0) yield break;

            float timeout = 3f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                int readyCount = 0;
                foreach (int id in m_ArrowLineEntityIds)
                {
                    if (GF.Entity.HasEntity(id)) readyCount++;
                }
                if (readyCount >= expectedCount ) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 再等待相机入场动画结束，避免引导位置因相机仍在缩放/移动而出现偏差
            var cameraController = ArrowMazeManager.Instance != null ? ArrowMazeManager.Instance.CameraController : null;
            if (cameraController != null)
            {
                while (cameraController.IsEntranceAnimating)
                {
                    yield return null;
                }
            }

            CheckGuide();
        }

        /// <summary>
        /// 第一步引导：随机找一个可直接移除的箭头，将其位置传给 GuideManager
        /// </summary>
        public void CheckGuide()
        {
            if (m_PlayerData?.CompleteGuideIds == null || m_PlayerData.CompleteGuideIds.Contains(4))
                return;
            if (m_PlayerData.LevelId == 1 && !m_PlayerData.CompleteGuideIds.Contains(1))
            {
                var entities = GetArrowEntities();
                var unblocked = new List<ArrowLineEntity>();
                foreach (var e in entities)
                {
                    if (e != null && e.CanAdvance())
                        unblocked.Add(e);
                }
                if (unblocked.Count == 0) return;

                int idx = UnityEngine.Random.Range(0, unblocked.Count);
                ArrowLineEntity targetArrow = unblocked[idx];
                // 引导指向箭头头部（可点击处），非尾部
                Transform headTransform = targetArrow.m_HeadVisual != null ? targetArrow.m_HeadVisual : targetArrow.transform;
                ShowFingerGuideAt(targetArrow);
                GuideManager.Instance.ShowGuide(1, headTransform, true, null);
            }

        }
        /// <summary>
        /// 西瓜满血进洞且场上仍有箭头时触发失败。
        /// </summary>
        public void NotifyWatermelonEnteredHoleWithRemainingArrows()
        {
            if (m_IsGameOver || m_IsGameWon) return;
            if (m_ArrowLineEntityIds.Count <= 0) return;

            m_CountdownActive = false;
            m_IsGameOver = true;
            PauseWatermelonPathOnGameOver();
            Log.Info("西瓜进洞且场上仍有箭头，游戏失败！");
            GF.Event.FireNow(this, ArrowMazeGameOverEventArgs.Create(ArrowMazeFailureType.WatermelonEscaped));
        }

        /// <summary>
        /// 应用暂停时（如看广告进入后台）暂停倒计时
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            m_CountdownPaused = pauseStatus;
        }

        /// <summary>
        /// 失去焦点时（如广告覆盖层）也暂停倒计时，与 OnApplicationPause 配合覆盖各平台
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                m_CountdownPaused = true;
            else
                m_CountdownPaused = false;
        }

        private void Update()
        {
            UpdateMapClickEffect();
            UpdateAutoHintTimer();
            if (m_FingerGuideVisible)
                UpdateFingerGuidePosition();

            if (!m_CountdownActive || m_IsGameOver || m_IsGameWon || m_CountdownPaused || m_CurrentLevelId == 1 || !IS_NEED_COUNTDOWN) return;
            m_RemainingTime -= Time.deltaTime;
            if (m_RemainingTime <= 0f)
            {
                m_RemainingTime = 0f;
                m_CountdownActive = false;
                m_IsGameOver = true;
                PauseWatermelonPathOnGameOver();
                Log.Info("时间到！游戏失败");
                GF.Event.FireNow(this, ArrowMazeGameOverEventArgs.Create(ArrowMazeFailureType.TimeOver));
            }
        }

        /// <summary>
        /// 看广告复活：增加30秒继续游戏（时间耗尽时）
        /// </summary>
        public void ReviveByTime()
        {
            if (m_IsGameOver && m_RemainingTime <= 0f)
            {
                m_RemainingTime = REVIVE_TIME_BONUS;
                m_CountdownActive = true;
                m_IsGameOver = false;
                Log.Info($"复活成功，增加{REVIVE_TIME_BONUS}秒");
            }
        }

        /// <summary>
        /// 看广告复活：恢复1点生命值继续游戏（生命值归零时）
        /// </summary>
        public void ReviveByLife()
        {
            if (m_IsGameOver && m_Lives <= 0)
            {
                m_Lives = 3;
                m_IsGameOver = false;
                m_CountdownActive = true;
                ResumeWatermelonPathAfterRevive();
                Log.Info("复活成功，恢复1点生命");
                GF.Event.FireNow(this, ArrowMazeArrowHitEventArgs.Create(m_Lives));
            }
        }

        /// <summary>
        /// 解析关卡ID并从 ArrowLevelTable 分配奖励箭头（绿色）
        /// </summary>
        private void ParseLevelIdAndAssignRewardArrows(ArrowLevelData levelData)
        {
            m_RewardArrowIndices.Clear();
            m_CurrentLevelId = 1;
            if (string.IsNullOrEmpty(levelData?.levelName)) return;

            // 解析 levelName 如 "Level4" -> 4, "Level01" -> 1
            string numPart = levelData.levelName.Replace("Level", "").Trim();
            if (!int.TryParse(numPart, out int parsedId) || parsedId < 1) return;
            m_CurrentLevelId = parsedId;

            var levelTable = GF.DataTable.GetDataTable<ArrowLevelTable>();
            if (!levelTable.HasDataRow(m_CurrentLevelId)) return;

            var row = levelTable.GetDataRow(m_CurrentLevelId);
            if (row?.RewardArrowCount == null || row.RewardArrowCount.Length < 2) return;

            int minCount = Mathf.Max(0, row.RewardArrowCount[0]);
            int maxCount = Mathf.Max(minCount, row.RewardArrowCount[1]);
            int arrowCount = levelData.arrows?.Count ?? 0;
            if (arrowCount == 0) return;

            int n = UnityEngine.Random.Range(minCount, maxCount + 1);
            n = Mathf.Clamp(n, 0, arrowCount);

            var indices = new List<int>();
            for (int i = 0; i < arrowCount; i++) indices.Add(i);
            for (int i = 0; i < n; i++)
            {
                int swap = UnityEngine.Random.Range(i, arrowCount);
                (indices[i], indices[swap]) = (indices[swap], indices[i]);
                m_RewardArrowIndices.Add(indices[i]);
            }
        }

        /// <summary>
        /// 生成所有箭头
        /// </summary>
        private void SpawnAllArrows()
        {
            if (m_CurrentLevelData == null || m_CurrentLevelData.arrows == null)
            {
                return;
            }

            GridManager gridManager = ArrowMazeManager.Instance?.GridManager;
            if (gridManager == null)
            {
                Log.Error("GridManager未找到");
                return;
            }

            for (int i = 0; i < m_CurrentLevelData.arrows.Count; i++)
            {
                var arrowDef = m_CurrentLevelData.arrows[i];
                arrowDef.isRewardArrow = m_RewardArrowIndices.Contains(i);
                SpawnArrowLine(arrowDef, gridManager);
            }
        }

        /// <summary>
        /// 生成单个箭头线
        /// </summary>
        private void SpawnArrowLine(ArrowLineDef arrowDef, GridManager gridManager)
        {
            if (arrowDef.path == null || arrowDef.path.Count < 2)
            {
                Log.Warning("箭头路径无效，跳过");
                return;
            }

            // 将Vector2Int路径转换为Dot列表
            List<Dot> dotPath = new List<Dot>();
            foreach (var gridPos in arrowDef.path)
            {
                Dot dot = gridManager.GetDotByGrid(gridPos);
                if (dot == null)
                {
                    Log.Warning($"找不到网格点: {gridPos}");
                    return; // 路径无效，跳过
                }
                dotPath.Add(dot);
            }

            // 使用Entity系统创建箭头实体
            var entityParams = ArrowLineEntityParams.Create(
                arrowDef,
                dotPath,
                OnArrowExited,
                OnArrowHit,
                dotPath.Count > 0 ? (Vector3?)dotPath[0].transform.position : null
            );
            if (m_ArrowAdvancedSkinDatabase != null && m_PlayerData != null && m_PlayerData.ArrowAdvancedSkinId != 0)
            {
                var skinConfig = m_ArrowAdvancedSkinDatabase.GetConfig(m_PlayerData.ArrowAdvancedSkinId);
                if (skinConfig != null)
                    entityParams.AdvancedSkinConfig = skinConfig;
            }
            // 设置父节点为LevelManager的Transform
            entityParams.ParentTransform = transform;

            // 使用Entity系统创建实体（使用字符串组名，如果不存在会自动创建）
            int entityId = GF.Entity.ShowEntity<ArrowLineEntity>(
                ARROW_LINE_PREFAB_PATH,
                Const.EntityGroup.ArrowLine,
                entityParams
            );

            m_ArrowLineEntityIds.Add(entityId);
        }

        /// <summary>
        /// 箭头开始移动时由 ArrowLineEntity 调用，按顺序循环播放 do/re/mi/fa/so/la/si 音效（clean1～clean7）
        /// </summary>
        public void NotifyArrowStartedMoving(ArrowLineEntity arrow)
        {
            HideFingerGuide();
            m_MoveStartCount++;
            int noteIndex = (m_MoveStartCount - 1) % 7 + 1;
            GF.Sound.PlayEffect(Utility.Text.Format("arrowPlay/clean{0}.mp3", noteIndex));
            NotifyArrowEliminatedForCombo(arrow);
            GrantArrowEliminationReward(arrow);
        }

        /// <summary>
        /// 箭头确定可消除时更新 Combo 计数并通知 UI；达到 EliminationRewardCount 后归零。
        /// </summary>
        public void NotifyArrowEliminatedForCombo(ArrowLineEntity arrow)
        {
            if (m_IsGameOver) return;
            int threshold = GetEliminationRewardCount();
            m_EliminationComboCount++;
            Vector3 worldPosition = arrow != null ? arrow.HeadWorldPosition : Vector3.zero;
            GF.Event.Fire(this, ArrowMazeArrowEliminatedEventArgs.Create(m_EliminationComboCount, worldPosition));
            if (m_EliminationComboCount >= threshold)
                m_EliminationComboCount = 0;
        }

        /// <summary>
        /// 箭头退出回调
        /// </summary>
        private void OnArrowExited(ArrowLineEntity arrow)
        {
            if (arrow != null && arrow.Available)
            {
                int entityId = arrow.Id;

                if (m_ArrowLineEntityIds.Contains(entityId))
                {
                    m_ArrowLineEntityIds.Remove(entityId);
                    ResetAutoHintTimer();
                }

                CheckGameWin();
            }
        }
        
        /// <summary>
        /// 普通箭头消除：产出 金币，金币数值随机
        /// </summary>
        private void OnArrowEliminated(ArrowLineEntity arrow)
        {
            if (m_IsGameOver) return;

            int baseValue = UnityEngine.Random.Range(2, 5);
            if (baseValue <= 0f) return;
            
            // 第1、2次直接发放（通过事件由 Topbar 处理飞币动画与数值更新）
            // 箭头在玩法场景坐标系，需转换为 UI 场景世界坐标（玩法相机与 UI 相机原点不同）
            Vector3 worldPos = GetRewardWorldPositionForUI(arrow);
            GF.Event.FireNow(this, RewardCollectedEventArgs.Create(baseValue, worldPos, PlayerDataType.Coins));
            CheckGameWin();
        }

        /// <summary>
        /// 将箭头的玩法场景世界坐标转换为 UI 场景世界坐标，供 Topbar 飞币动画使用。
        /// 玩法相机与 UI 相机原点不同，UI 根节点随 UI 相机创建，需在发射事件前完成转换。
        /// </summary>
        private Vector3 GetRewardWorldPositionForUI(ArrowLineEntity arrow)
        {
            if (arrow == null || arrow.transform == null) return Vector3.zero;
            Transform headTransform = arrow.transform;//arrow.m_HeadVisual != null ? arrow.m_HeadVisual :

            Camera gameCam = Camera.main;
            Camera uiCam = GFBuiltin.UICamera;
            Canvas rootCanvas = GFBuiltin.RootCanvas;
            if (gameCam == null || uiCam == null || rootCanvas == null) return headTransform.position;

            Vector3 sceneWorldPos = headTransform.position;
            Vector3 screenPoint = gameCam.WorldToScreenPoint(sceneWorldPos);
            float planeDist = rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.planeDistance : 100f;
            return uiCam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, planeDist));
        }

        /// <summary>
        /// 箭头确定可消除时发放奖励（开始移动或强制消除时调用，非消除完成后）。
        /// 产出 Diamond，每累计 N 次弹出 ArrowRewardDialogUIForm（N 读 GameConfig.EliminationRewardDialogCount）。
        /// </summary>
        public void GrantArrowEliminationReward(ArrowLineEntity arrow)
        {
            if (!CommonHelper.IsSpec()) return;
            if (arrow == null || m_IsGameOver) return;
            if (!m_RewardGrantedArrowIds.Add(arrow.Id)) return;

            OnRewardArrowEliminated(arrow);
        }

        /// <summary>
        /// 特殊奖励箭头消除：产出 Diamond，每累计 N 次弹出 ArrowRewardDialogUIForm（N 读 GameConfig.EliminationRewardDialogCount）
        /// </summary>
        private void OnRewardArrowEliminated(ArrowLineEntity arrow)
        {
            if (m_IsGameOver) return;

            m_RewardArrowEliminateCount++;
            int count = GetEliminationRewardCount();
            bool isMilestoneReward = m_RewardArrowEliminateCount % count == 0;

            // 达到连消里程碑但场上剩余箭头少于 EliminationRewardCount 的 1/2 时，不弹窗也不发放连消奖励。
            if (isMilestoneReward && ArrowCount * 2 < count)
                return;

            float baseValue = isMilestoneReward
                ? GenerateRandomReward((int)ArrowRewardType.Combo)
                : GenerateRandomReward((int)ArrowRewardType.Normal);

            if (baseValue <= 0f) return;

            if (isMilestoneReward)
            {
                m_DeferCheckGameWin = true;
                var rewardParams = RewardDialogParams.Create();
                rewardParams.rewardSource = RewardSourceConst.arrowReward;
                rewardParams.Rewards.Add(new RewardData(PlayerDataType.Diamond, baseValue));
                rewardParams.isADDouble = true;
                rewardParams.OnDialogClosed = OnEliminationRewardDialogClosed;

                var uiParams = UIParams.CreateWithReward(rewardParams);
                GF.UI.OpenUIForm(UIViews.ArrowRewardDialogUIForm, uiParams);
            }
            else
            {
                // 箭头在玩法场景坐标系，需转换为 UI 场景世界坐标（玩法相机与 UI 相机原点不同）
                Vector3 worldPos = GetRewardWorldPositionForUI(arrow);
                GF.Event.FireNow(this, RewardCollectedEventArgs.Create(baseValue, worldPos, PlayerDataType.Diamond));
            }
        }

        void OnEliminationRewardDialogClosed()
        {
            m_DeferCheckGameWin = false;
            GF.Event.Fire(this, ArrowMazeEliminationRewardDialogClosedEventArgs.Create());
        }

        /// <summary>
        /// 场上是否只剩最后一个箭头（或已无箭头），用于刮刮卡通关延后等逻辑。
        /// </summary>
        public bool IsLastArrowOnField()
        {
            return m_ArrowLineEntityIds.Count <= 1;
        }
        /// <summary>
        /// 胜利体条件检测
        /// </summary>
        private void CheckGameWin()
        {
            if (m_DeferCheckGameWin) return;
            if (m_ArrowLineEntityIds.Count == 0 && !m_IsGameOver)
            {
                m_CountdownActive = false;
                m_IsGameWon = true;
                m_IsGameOver = true;
                Log.Info("游戏胜利！");
                // 触发胜利事件,特殊奖励箭头不触发，领取奖励后再检测胜利条件
                GF.Event.FireNow(this, ArrowMazeGameWinEventArgs.Create());
            }
        }
        /// <summary>
        /// 箭头碰撞回调
        /// </summary>
        private void OnArrowHit(ArrowLineEntity arrow)
        {
            if (m_CurrentLevelId == 1) return;
            m_Lives--;
            Log.Info($"箭头碰撞！剩余生命: {m_Lives}");

            // 触发碰撞事件
            GF.Event.FireNow(this, ArrowMazeArrowHitEventArgs.Create(m_Lives));

            if (m_Lives <= 0 && !m_IsGameOver)
            {
                m_CountdownActive = false;
                m_IsGameOver = true;
                PauseWatermelonPathOnGameOver();
                Log.Info("生命值归零，游戏失败！");
                GF.Event.FireNow(this, ArrowMazeGameOverEventArgs.Create(ArrowMazeFailureType.LivesOver));
            }
        }

        /// <summary>
        /// 清理所有箭头
        /// </summary>
        public void ClearAllArrows()
        {
            // 使用Entity系统回收所有箭头
            foreach (var entityId in m_ArrowLineEntityIds)
            {
                if (GF.Entity.HasEntity(entityId))
                {
                    GF.Entity.HideEntitySafe(entityId);
                }
            }
            m_ArrowLineEntityIds.Clear();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            m_WatermelonPathExtension?.EndLevel();
            ClearAllArrows();
            ClearClickEffects();
            DestroyFingerGuide();
        }

        void EnsureClickEffectRoot()
        {
            if (m_ClickEffectRoot != null) return;
            var root = new GameObject("ClickEffects");
            root.layer = GameplayLayer;
            root.transform.SetParent(transform, false);
            m_ClickEffectRoot = root.transform;
        }

        void ClearClickEffects()
        {
            if (m_ClickEffectRoot == null) return;
            for (int i = m_ClickEffectRoot.childCount - 1; i >= 0; i--)
                Destroy(m_ClickEffectRoot.GetChild(i).gameObject);
        }

        void UpdateMapClickEffect()
        {
            if (m_DianjiPrefab == null || m_IsGameOver) return;

            var cameraController = ArrowMazeManager.Instance != null ? ArrowMazeManager.Instance.CameraController : null;
            if (cameraController != null && cameraController.IsEntranceAnimating) return;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                        TrySpawnClickEffect(touch.position);
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
                TrySpawnClickEffect(Input.mousePosition);
        }

        void TrySpawnClickEffect(Vector2 screenPosition)
        {
            var cameraController = ArrowMazeManager.Instance != null ? ArrowMazeManager.Instance.CameraController : null;
            if (cameraController != null && !cameraController.IsPointerInCameraView(screenPosition))
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float planeZ = 0f;
            Vector3 worldPos = ScreenToWorldOnPlane(screenPosition, cam, planeZ);
            SpawnClickEffect(worldPos);
        }

        static Vector3 ScreenToWorldOnPlane(Vector2 screenPosition, Camera cam, float planeZ)
        {
            float distance = Mathf.Abs(cam.transform.position.z - planeZ);
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
            worldPos.z = planeZ;
            return worldPos;
        }

        void SpawnClickEffect(Vector3 worldPosition)
        {
            EnsureClickEffectRoot();
            GameObject instance = Instantiate(m_DianjiPrefab, m_ClickEffectRoot);
            PrepareClickEffectInstance(instance, worldPosition);
            StartCoroutine(DestroyClickEffectWhenFinished(instance));
        }

        void PrepareClickEffectInstance(GameObject instance, Vector3 worldPosition)
        {
            SetLayerRecursively(instance, GameplayLayer);
            worldPosition.z = -0.05f;
            instance.transform.position = worldPosition;

            Camera cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                float scale = Mathf.Clamp(cam.orthographicSize / 12f, 0.8f, 3f);
                instance.transform.localScale = Vector3.one * scale;
            }

            foreach (Transform t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<ParticleSystem>() != null)
                    t.localRotation = Quaternion.identity;
            }

            foreach (ParticleSystemRenderer renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (renderer == null) continue;
                renderer.enabled = true;
                renderer.sortingOrder = ClickEffectSortingOrder;
                renderer.maxParticleSize = 100f;
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        static IEnumerator DestroyClickEffectWhenFinished(GameObject effect)
        {
            if (effect == null) yield break;

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in systems)
            {
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }

            yield return null;

            foreach (ParticleSystem ps in systems)
            {
                if (ps != null && ps.main.loop)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            float waitDuration = Mathf.Max(ClickEffectMinDuration, GetParticleEffectDuration(systems));
            waitDuration = Mathf.Min(waitDuration, ClickEffectDestroyTimeout);
            yield return new WaitForSeconds(waitDuration);

            if (effect != null)
                Destroy(effect);
        }

        static float GetParticleEffectDuration(ParticleSystem[] systems)
        {
            float maxDuration = ClickEffectMinDuration;
            foreach (ParticleSystem ps in systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                float lifetime = main.startLifetime.constantMax;
                if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                    lifetime = main.startLifetime.constant;
                float duration = main.loop ? 0.6f : main.duration + lifetime;
                maxDuration = Mathf.Max(maxDuration, duration);
            }

            return maxDuration;
        }

        /// <summary>
        /// 生成随机奖励，rewardType 1:普通奖励，2：通关奖励，3：气泡奖励
        /// </summary>
        public float GenerateRandomReward(int rewardType = 0)
        {
            if (!Enum.IsDefined(typeof(ArrowRewardType), rewardType))
                return 0f;

            return ArrowRewardCalculator.CalculateReward((ArrowRewardType)rewardType);
        }
    }
}
