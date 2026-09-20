using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;
using System;
using GameFramework;

namespace ArrowMaze
{
    /// <summary>
    /// 箭头线实体 - 管理箭头的移动、碰撞、视觉
    /// </summary>
    public class ArrowLineEntity : EntityBase
    {
        // 路径数据
        private List<Dot> m_Nodes = new List<Dot>(); // 箭头经过的所有节点（有序列表）
        private int m_StartIndex = 0; // 当前头部在nodes中的索引
        private float m_LineWidth = 0.36f;
        /// <summary> 是否使用重复纹理 body（高级皮肤）：按当前线段总长度动态设置 tiling，使移动时纹理均匀。 </summary>
        [SerializeField] private bool m_UseRepeatingTextureBody = false;
        /// <summary> 重复纹理时，每段纹理占用的世界长度（用于 tiling 计算）。 </summary>
        [SerializeField] private float m_TextureRepeatWorldLength = 0.25f;
        // 视觉组件
        private LineRenderer m_LineRenderer;
        private EdgeCollider2D m_EdgeCollider;
        public Transform m_HeadVisual; // 头部视觉对象
        Transform m_HeadTrailVisual; // Head 子节点 tuowei 拖尾特效（内部缓存）
        bool m_HeadTrailHiddenForBlockBump; // 阻挡回弹期间是否由本逻辑隐藏了拖尾
        private Material m_LineMaterial; // LineRenderer的材质
        private float m_HeadInitialRotationOffset = 0f; // Head的初始旋转偏移（如果预制体中Head初始旋转为0时指向右边，则偏移为0）

        // 运动状态
        private bool m_IsMoving = false; // 是否正在移动
        /// <summary> 点击后等待移出（停顿期间箭头保持静止）。 </summary>
        private bool m_IsWaitingToMove = false;
        private Coroutine m_ClickMoveDelayCoroutine;
        /// <summary> 可移出箭头点击后、开始移出前的停顿时间（秒）。 </summary>
        [SerializeField] private float m_ClickMoveDelay = 0.05f;

        /// <summary>
        /// 不可移出箭头的「伸出→碰障→退回」动画阶段。
        /// 与正常离场移动共用 m_IsMoving，但不释放网格占用、不触发成功移出回调。
        /// </summary>
        enum BlockBumpPhase { None, Advancing, Retreating }
        BlockBumpPhase m_BlockBumpPhase = BlockBumpPhase.None;
        /// <summary> 点击被挡瞬间的路径快照（尾→头），退回结束时还原。 </summary>
        List<Vector3> m_BlockBumpOriginalPositions;
        /// <summary> 伸出/退回时头部前进的世界方向。 </summary>
        Vector3 m_BlockBumpHeadDirection;
        /// <summary> 头部最多可伸出的世界距离（碰到障碍格前停下）。 </summary>
        float m_BlockBumpAdvanceLimit;
        /// <summary> 当前已伸出距离；伸出阶段 0→Limit，退回阶段 Limit→0。 </summary>
        float m_BlockBumpEffectiveAdvance;
        /// <summary> 相邻阻挡时至少伸出的格距比例，保证可见反馈。 </summary>
        const float BlockBumpMinAdvanceRatio = 0.35f;
        /// <summary> 在阻挡格中心前停下的格距比例，避免头部穿入障碍。 </summary>
        const float BlockBumpStopBeforeBlockRatio = 0.55f;
        /// <summary> 从原始形状重建伸出姿态时的模拟步长（占格距比例）。 </summary>
        const float BlockBumpRebuildStepRatio = 0.04f;
        /// <summary> 退回速度倍率（相对伸出速度，可在 ArrowLine 预制体 Inspector 调整）。 </summary>
        [SerializeField] private float m_BlockBumpRetreatSpeedRatio = 1f;

        // 连续移动相关（基于连续插值而非格子跳动）
        private List<Vector3> m_CurrentPositions; // 当前点位（尾→头），移动时头前进、尾逐段缩短
        private List<Vector3> m_PathPoints;       // 网格路径点（尾→头），仅用于初始化
        private List<Vector3> m_SegmentDirections; // 每段方向（Head 旋转等）
        [SerializeField]
        private float m_MoveSpeed = 38;               // 移动速度（世界单位/秒），由关卡数据中的 stepTime 与网格间距推导
        /// <summary> 移速倍数（如急速验证时设为 2） </summary>
        public static float SpeedMultiplier { get; set; } = 1f;
        private float m_GridSpacing;              // 网格间距
        private Vector3 m_LastHeadDirection;     // 最后一帧的头部方向，仅剩 1 点时用于继续前移
        /// <summary> <see cref="WatermelonPathExtension.IsExtensionEnabled"/> 且头部已进入外周环路时的顺时针跟随状态。 </summary>
        private bool m_FollowingWatermelonLoop;
        private int m_WatermelonLoopSegIdx;
        private float m_WatermelonLoopSegT;
        /// <summary> 沿环路顺时针已累计行进弧长（世界单位）。 </summary>
        private float m_ArrowLoopTravelAccumulated;
        /// <summary> 头部已到达洞口：隐藏头部，身体向洞口缩短直至离场。 </summary>
        private bool m_ShrinkingIntoLoopHole;
        
        // 皮肤属性
        /// <summary> 默认头部精灵缩放比例（非高级皮肤时使用） </summary>
        private const float DefaultHeadSpriteScale = 2.3f;
        /// <summary> 默认头部移速倍数（非高级皮肤时使用） </summary>
        private const float DefaultHeadSpeedRatio = 1.3f;
        /// <summary> 默认尾部移速倍数（非高级皮肤时使用） </summary>
        private const float DefaultTailSpeedRatio = 1.25f;
        /// <summary> 头部移速倍数；高级皮肤下与 TailSpeedRatio 相同（头尾同速） </summary>
        private float m_HeadSpeedRatio = DefaultHeadSpeedRatio;
        /// <summary> 尾部移速倍数；高级皮肤下与 HeadSpeedRatio 相同 </summary>
        private float m_TailSpeedRatio = DefaultTailSpeedRatio;

        // 配置参数
        private bool m_OccupyAllNodes = true;
        private Color m_LineColor = Color.white;
        private Color m_HitColor = Color.red;
        /// <summary> 当前阻挡状态下是否已扣过生命（仅本箭头、仅点击时判定）。 </summary>
        bool m_HasConsumedLifeForCurrentBlock;
        /// <summary> 本箭头是否因被点击阻挡而保持红色。 </summary>
        bool m_IsShowingBlockedColor;

        /// <summary> 是否使用彩色箭头（非特殊货币箭头随机彩色，排除绿/黑/白）。默认 false 为默认皮肤。 </summary>
        public static bool UseColorfulArrows { get; set; }
        /// <summary> 是否夜间模式。默认箭头皮肤下：夜间用白色、日间用黑色；彩色皮肤下不受影响。 </summary>
        public static bool IsNightMode { get; set; }
        private static readonly Color[] s_ColorfulPalette = new Color[]
        {
            new Color(0.9f, 0.2f, 0.2f),   // 红
            new Color(0.2f, 0.4f, 0.95f),  // 蓝
            new Color(0.95f, 0.5f, 0.1f),  // 橙
            new Color(0.7f, 0.2f, 0.85f),  // 紫
            new Color(0.95f, 0.85f, 0.2f), // 黄
            new Color(0.2f, 0.8f, 0.9f),   // 青
            new Color(0.95f, 0.35f, 0.6f), // 粉/品红
            new Color(0.55f, 0.45f, 0.25f), // 棕/金（排除绿/黑/白）
        };

        /// <summary> 从彩色色板中随机取一色（排除绿/黑/白），供非特殊货币箭头使用。 </summary>
        public static Color GetRandomColorfulColor()
        {
            return s_ColorfulPalette[UnityEngine.Random.Range(0, s_ColorfulPalette.Length)];
        }

        // 回调
        private Action<ArrowLineEntity> m_OnExitedCallback;
        private Action<ArrowLineEntity> m_OnHitCallback;

        // 初始化数据
        private ArrowLineDef m_ArrowDef;
        private List<Dot> m_DotPath;
        /// <summary> 当前使用的高级皮肤配置，非 null 时已应用 body/head/速度/头尾同速等。 </summary>
        private ArrowAdvancedSkinConfig m_AdvancedSkinConfig;

        // 相机引用（用于点击检测）
        private Camera m_Camera;

        // 点击检测相关
        private float m_ClickDetectionRadius = 0.15f; // 点击检测半径（基于线条宽度）

        // 辅助线：长按显示前端延长线（由设置开关控制）
        /// <summary> 是否开启长按辅助线，可由设置界面设置 </summary>
        public static bool AssistLineEnabled = true;
        /// <summary> 超过该时长才视为长按（出辅助线）；略放宽，避免普通点击被当成长按而不移出。 </summary>
        private const float LongPressTime = 0.85f;
        /// <summary> 按下与抬起之间指针移动超过此像素视为拖拽（如平移场景），不触发箭头点击。 </summary>
        private const float ClickDragThresholdPx = 36f;
        /// <summary> 点击判定半径相对线宽的倍数。 </summary>
        private const float ClickRadiusWidthFactor = 1.2f;
        /// <summary> 屏幕空间最小点击半径（像素），缩小地图时仍保持可点。 </summary>
        private const float MinClickRadiusScreenPx = 28f;
        private static ArrowLineEntity s_PressedArrow;
        private static float s_PressTime;
        private static Vector2 s_PressPosition;
        /// <summary> 同一按下帧只做一次最近箭头解析，避免多实体 Update 顺序抢占。 </summary>
        private static int s_PointerPickFrame = -1;
        public LineRenderer m_AssistLineRenderer;
        private bool m_DidLongPressThisTouch;
        private static readonly Color AssistLineColor = new Color(0.4f, 0.65f, 1f, 0.55f);

        /// <summary> 道具：显示所有引导线。为 true 时所有未移动的箭头显示辅助线，点击任意箭头后清除。 </summary>
        public static bool ShowAllGuideLines { get; set; }
        /// <summary> 道具：强制消除下一次点击的箭头（无视规则直接消除，不触发移动）。 </summary>
        public static bool ForceEliminateNextClick { get; set; }
        private bool m_IsForceEliminating = false;
        private const float ForceEliminateDuration = 0.5f;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 从EntityParams中获取初始化数据
            if (userData is ArrowLineEntityParams arrowParams)
            {
                m_ArrowDef = arrowParams.ArrowDef;
                m_DotPath = arrowParams.DotPath;
                m_OnExitedCallback = arrowParams.OnExited;
                m_OnHitCallback = arrowParams.OnHit;

                // 设置实体名称（如果有的话）
                if (arrowParams.ArrowDef != null)
                {
                    // 使用路径的第一个点作为标识
                    if (m_DotPath != null && m_DotPath.Count > 0)
                    {
                        var firstDot = m_DotPath[0];
                        if (firstDot != null)
                        {
                            gameObject.name = $"ArrowLine_{firstDot.G.x}_{firstDot.G.y}";
                        }
                    }
                }

                m_AdvancedSkinConfig = arrowParams.AdvancedSkinConfig;
                Initialize(m_ArrowDef, m_DotPath, m_OnExitedCallback, m_OnHitCallback);
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            if (s_PressedArrow == this)
            {
                s_PressedArrow = null;
                HideAssistLine();
            }
            if (m_Nodes != null)
            {
                foreach (var node in m_Nodes)
                {
                    if (node != null && node.Occupant == this)
                        node.Occupant = null;
                }
            }
            m_FollowingWatermelonLoop = false;
            m_ArrowLoopTravelAccumulated = 0f;
            m_ShrinkingIntoLoopHole = false;
            ResetBlockedBumpState();
            CancelClickMoveDelay();
            m_IsMoving = false;
            StopAllCoroutines();
            if (m_LineMaterial != null)
            {
                Destroy(m_LineMaterial);
                m_LineMaterial = null;
            }
            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// 初始化箭头
        /// </summary>
        public void Initialize(ArrowLineDef arrowDef, List<Dot> dotPath,
            Action<ArrowLineEntity> onExited, Action<ArrowLineEntity> onHit)
        {
            m_Nodes = new List<Dot>(dotPath);
            m_StartIndex = arrowDef.startIndex;
            m_OccupyAllNodes = arrowDef.occupyAllNodes;
            // 特殊货币箭头固定绿色；其余：彩色模式下随机彩色（排除绿/黑/白），默认皮肤下夜间白、日间黑
            m_LineColor = arrowDef.isRewardArrow
                ? new Color(0f, 0.8f, 0.2f)
                : (UseColorfulArrows ? GetRandomColorfulColor() : (IsNightMode ? Color.white : Color.black));
            m_HitColor = arrowDef.hitColor;

            m_OnExitedCallback = onExited;
            m_OnHitCallback = onHit;

            // 应用高级皮肤配置（body 粗细、速度、重复纹理、头尾同速等）
            if (m_AdvancedSkinConfig != null)
            {
                m_LineWidth = m_AdvancedSkinConfig.bodyWidth;
                m_MoveSpeed = m_AdvancedSkinConfig.moveSpeed;
                m_UseRepeatingTextureBody = m_AdvancedSkinConfig.useRepeatingTextureBody;
                m_TextureRepeatWorldLength = m_AdvancedSkinConfig.textureRepeatWorldLength;
                m_HeadSpeedRatio = m_AdvancedSkinConfig.speedRatio;
                m_TailSpeedRatio = m_AdvancedSkinConfig.speedRatio;
            }
            else
            {
                m_HeadSpeedRatio = DefaultHeadSpeedRatio;
                m_TailSpeedRatio = DefaultTailSpeedRatio;
            }

            // 初始化视觉组件
            InitializeVisuals();

            // 获取相机引用（用于点击检测）
            m_Camera = Camera.main;
            if (m_Camera == null)
            {
                m_Camera = FindObjectOfType<Camera>();
            }

            // 设置点击检测半径（基于线条宽度；实际判定时还会按屏幕像素做下限）
            if (m_LineRenderer != null)
            {
                m_ClickDetectionRadius = Mathf.Max(m_LineRenderer.startWidth, m_LineRenderer.endWidth) * ClickRadiusWidthFactor;
            }

            // 占用节点
            ClaimNodes();

            // 初始化连续移动相关数据
            InitializeContinuousMovement();

            // 同步视觉
            SyncVisualImmediate();
        }

        /// <summary>
        /// 初始化连续移动相关数据：m_CurrentPositions = 网格路径点列表（尾→头），以转折点切分，每段单独参与移动计算。
        /// </summary>
        private void InitializeContinuousMovement()
        {
            GridManager gridManager = ArrowMazeManager.Instance?.GridManager;
            if (gridManager != null && gridManager.GridDef != null)
                m_GridSpacing = gridManager.GridDef.spacing;
            else
                m_GridSpacing = 1f;

            // 根据关卡配置的 stepTime 推导移动速度：每 stepTime 移动一个格距
            // 若关卡未配置或配置非法，则回退到 0.05s 一格的默认速度
            float stepTime = (m_ArrowDef != null && m_ArrowDef.stepTime > 0f) ? m_ArrowDef.stepTime : 0.05f;
            // m_MoveSpeed = m_GridSpacing / stepTime;

            m_FollowingWatermelonLoop = false;
            m_ArrowLoopTravelAccumulated = 0f;
            m_ShrinkingIntoLoopHole = false;
            ResetBlockedBumpState();
            SetHeadVisualActive(true);

            // 网格路径点：从尾到头的世界坐标，顺序与 m_Nodes 一致（A,B,C... C 为 Head）
            m_PathPoints = new List<Vector3>();
            for (int i = 0; i < m_Nodes.Count; i++)
            {
                if (m_Nodes[i] != null)
                    m_PathPoints.Add(m_Nodes[i].transform.position);
                else
                    m_PathPoints.Add(Vector3.zero);
            }

            // 当前点位 = 路径点的拷贝，移动时在此列表上做“头前进、尾逐段缩短”
            m_CurrentPositions = new List<Vector3>(m_PathPoints);
            m_SegmentDirections = new List<Vector3>();
            UpdateSegmentDirections();
        }

        /// <summary>
        /// 根据当前 m_CurrentPositions 更新各段方向（用于 Head 旋转等）
        /// </summary>
        private void UpdateSegmentDirections()
        {
            m_SegmentDirections.Clear();
            if (m_CurrentPositions == null || m_CurrentPositions.Count < 2) return;
            for (int i = 1; i < m_CurrentPositions.Count; i++)
            {
                Vector3 direction = m_CurrentPositions[i] - m_CurrentPositions[i - 1];
                if (direction.magnitude > 0.001f)
                    m_SegmentDirections.Add(direction.normalized);
                else if (m_SegmentDirections.Count > 0)
                    m_SegmentDirections.Add(m_SegmentDirections[m_SegmentDirections.Count - 1]);
                else
                    m_SegmentDirections.Add(Vector3.right);
            }
        }

        /// <summary>
        /// 初始化视觉组件
        /// </summary>
        private void InitializeVisuals()
        {
            // LineRenderer
            m_LineRenderer = GetComponent<LineRenderer>();
            if (m_LineRenderer == null)
            {
                m_LineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            if (m_AdvancedSkinConfig != null && m_AdvancedSkinConfig.bodyMaterial != null)
                m_LineRenderer.sharedMaterial = m_AdvancedSkinConfig.bodyMaterial;
            // 仅在使用重复纹理 body 时创建材质实例（需每帧改 tiling），否则用 sharedMaterial 以便合批
            if (m_UseRepeatingTextureBody)
            {
                if (m_LineMaterial != null) { Destroy(m_LineMaterial); m_LineMaterial = null; }
                m_LineMaterial = new Material(m_LineRenderer.sharedMaterial);
                m_LineMaterial.renderQueue = 3001;
                m_LineRenderer.material = m_LineMaterial;
            }
            else
            {
                if (m_LineMaterial != null) { Destroy(m_LineMaterial); m_LineMaterial = null; }
                // 不赋值 .material，保持 sharedMaterial，颜色仅通过 startColor/endColor 传递以支持合批
            }
            ApplyLineColor(m_LineColor);
            m_LineRenderer.startWidth = m_LineWidth;
            m_LineRenderer.endWidth = m_LineWidth;
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.sortingOrder = 1;

            // EdgeCollider2D（用于点击检测）
            m_EdgeCollider = GetComponent<EdgeCollider2D>();
            if (m_EdgeCollider == null)
            {
                m_EdgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            }
            m_EdgeCollider.isTrigger = true; // 设置为触发器，用于点击检测
            m_EdgeCollider.offset = Vector2.zero; // 确保offset为0，使用本地坐标
            m_EdgeCollider.edgeRadius = Mathf.Max(m_LineWidth * 0.55f, 0.12f);

            // 头部视觉对象（从子对象中查找，Head应该在预制体中已经创建）
            if (m_HeadVisual == null)
            {
                // 从子对象中查找Head
                Transform headTransform = transform.Find("Head");
                if (headTransform != null)
                {
                    m_HeadVisual = headTransform;
                }
                else
                {
                    // 如果找不到，尝试通过名称查找（备用方案）
                    foreach (Transform child in transform)
                    {
                        if (child.name == "Head" || child.name.Contains("Head"))
                        {
                            m_HeadVisual = child;
                            break;
                        }
                    }
                }

                if (m_HeadVisual != null)
                {
                    // 记录Head的初始旋转偏移（如果预制体中Head初始旋转为0时指向右边，则偏移为0）
                    // 如果Head预制体的初始旋转不是0，需要记录这个偏移量
                    m_HeadInitialRotationOffset = m_HeadVisual.localEulerAngles.z;

                    SpriteRenderer sr = m_HeadVisual.gameObject.GetOrAddComponent<SpriteRenderer>();
                    sr.color = m_LineColor;
                    sr.sortingOrder = 2;
                    if (m_AdvancedSkinConfig != null && m_AdvancedSkinConfig.headSprite != null)
                        sr.sprite = m_AdvancedSkinConfig.headSprite;
                    float headScale = m_AdvancedSkinConfig != null ? m_AdvancedSkinConfig.headSpriteScale : DefaultHeadSpriteScale;
                    m_HeadVisual.localScale = Vector3.one * headScale;
                    EnsureHeadTrailVisual();
                }
            }
        }

        void EnsureHeadTrailVisual()
        {
            if (m_HeadTrailVisual != null || m_HeadVisual == null) return;
            m_HeadTrailVisual = m_HeadVisual.Find("tuowei");
            if (m_HeadTrailVisual != null && !IsHeadTrailEnabled())
                SetHeadTrailVisible(false);
        }

        static bool IsHeadTrailEnabled()
        {
            return !CommonHelper.IsSpec();
        }

        /// <summary> 显示/隐藏头部拖尾，隐藏时清空粒子避免残留。 </summary>
        void SetHeadTrailVisible(bool visible)
        {
            EnsureHeadTrailVisual();
            if (m_HeadTrailVisual == null) return;

            if (visible && !IsHeadTrailEnabled())
                visible = false;

            if (!visible)
            {
                foreach (var ps in m_HeadTrailVisual.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            m_HeadTrailVisual.gameObject.SetActive(visible);
        }

        /// <summary> 阻挡回弹开始：记录拖尾原状态并隐藏。 </summary>
        void HideHeadTrailForBlockBump()
        {
            EnsureHeadTrailVisual();
            if (m_HeadTrailVisual == null) return;

            m_HeadTrailHiddenForBlockBump = m_HeadTrailVisual.gameObject.activeSelf;
            SetHeadTrailVisible(false);
        }

        /// <summary> 阻挡回弹结束：恢复拖尾（仅当本次由阻挡逻辑隐藏过）。 </summary>
        void RestoreHeadTrailAfterBlockBump()
        {
            if (!m_HeadTrailHiddenForBlockBump) return;

            m_HeadTrailHiddenForBlockBump = false;
            SetHeadTrailVisible(IsHeadTrailEnabled());
        }

        /// <summary>
        /// 占用节点
        /// </summary>
        private void ClaimNodes()
        {
            for (int i = m_StartIndex; i < m_Nodes.Count; i++)
            {
                if (m_Nodes[i] != null)
                {
                    m_Nodes[i].Occupant = this;
                }
            }
        }

        /// <summary>
        /// 立即同步视觉（不移动）
        /// </summary>
        private void SyncVisualImmediate()
        {
            // 使用UpdateVisuals来更新视觉
            UpdateVisuals();
        }

        /// <summary>
        /// 是否正在移动或处于点击后移出前的停顿（用于自动模拟时判断一步是否完成）
        /// </summary>
        public bool IsMoving => m_IsMoving || m_IsWaitingToMove;

        /// <summary> 箭头头部当前世界坐标（连续移动时与网格对齐判地用） </summary>
        public Vector3 HeadWorldPosition => GetHeadWorldPosition();

        /// <summary>
        /// 是否为特殊奖励箭头（消除时产出 Diamond 奖励）
        /// </summary>
        public bool IsRewardArrow => m_ArrowDef?.isRewardArrow ?? false;

        /// <summary>
        /// 当前是否可前进（未被阻挡且未在移动）。用于自动模拟前判断可点击的箭头。
        /// </summary>
        public bool CanAdvance()
        {
            if (IsMoving) return false;
            return !IsBlockedInForwardDirection(out _);
        }

        /// <summary>
        /// 尝试向前移动（仅点击时调用）。阻挡只在点击时检测一次：前进方向下一格无网格或非其他箭头占用即为无阻挡。
        /// </summary>
        public void TryAdvance()
        {
            if (IsMoving) return;

            // 调试日志：触发点击的箭头名字
            string arrowName = gameObject.name;
            Log.Info("[ArrowClick] 触发点击的箭头名字: {0}", arrowName);

            // 最终运动方向（上下左右）
            string directionStr = "无";
            if (m_Nodes != null && m_Nodes.Count >= 2)
            {
                Dot headNode = m_Nodes[m_Nodes.Count - 1];
                Dot prevNode = m_Nodes[m_Nodes.Count - 2];
                Vector2Int step = headNode.G - prevNode.G;
                if (step.x != 0) step.x = step.x > 0 ? 1 : -1;
                if (step.y != 0) step.y = step.y > 0 ? 1 : -1;

                // 注意：当前网格坐标系中，y 增大代表画面向下，所以这里做反向映射
                if (step == new Vector2Int(1, 0)) directionStr = "右";
                else if (step == new Vector2Int(-1, 0)) directionStr = "左";
                else if (step == new Vector2Int(0, 1)) directionStr = "下";
                else if (step == new Vector2Int(0, -1)) directionStr = "上";
            }
            Log.Info("[ArrowClick] 最终运动方向: {0}", directionStr);

            bool blocked = IsBlockedInForwardDirection(out Dot blockingDot);
            bool allowed = !blocked;
            Log.Info("[ArrowClick] 是否允许运动: {0}, 是否被阻挡: {1}", allowed, blocked);
            if (blocked && blockingDot != null)
            {
                string blockPointName = !string.IsNullOrEmpty(blockingDot.gameObject.name)
                    ? blockingDot.gameObject.name
                    : string.Format("Dot_{0}_{1}", blockingDot.G.x, blockingDot.G.y);
                Log.Info("[ArrowClick] 阻挡的点位名字: {0}", blockPointName);
            }

            if (blocked)
            {
                GF.Sound.PlayEffect("arrowPlay/error.mp3");
                // 不可移出：先向前伸出，碰到障碍后变红扣血并退回原位（不释放网格占用）
                StartBlockedBumpMovement(blockingDot);
                return;
            }

            ClearBlockedState();
            StartClickMoveDelay();
            // GF.Sound.PlayEffect("arrowPlay/show.mp3");
        }

        void StartClickMoveDelay()
        {
            if (m_IsWaitingToMove || m_IsMoving) return;
            m_IsWaitingToMove = true;
            if (m_ClickMoveDelayCoroutine != null)
                StopCoroutine(m_ClickMoveDelayCoroutine);
            m_ClickMoveDelayCoroutine = StartCoroutine(ClickMoveDelayCoroutine());
        }

        void CancelClickMoveDelay()
        {
            m_IsWaitingToMove = false;
            if (m_ClickMoveDelayCoroutine != null)
            {
                StopCoroutine(m_ClickMoveDelayCoroutine);
                m_ClickMoveDelayCoroutine = null;
            }
        }

        IEnumerator ClickMoveDelayCoroutine()
        {
            if (m_ClickMoveDelay > 0f)
                yield return new WaitForSeconds(m_ClickMoveDelay);
            m_IsWaitingToMove = false;
            m_ClickMoveDelayCoroutine = null;
            StartContinuousMovement();
        }

        /// <summary>
        /// 前进方向从头部到网格边缘之间是否被其他箭头占用（仅点击时调用）。
        /// 获取同行/同列所有有效格点后依次判定 Occupant，以支持不规则几何（有效格可能不连续）。
        /// </summary>
        /// <param name="blockingDot">若被阻挡，则为第一个阻挡的点位；否则为 null。</param>
        private bool IsBlockedInForwardDirection(out Dot blockingDot)
        {
            blockingDot = null;
            if (m_Nodes == null || m_Nodes.Count < 2) return true;
            GridManager gridManager = ArrowMazeManager.Instance?.GridManager;
            if (gridManager == null) return true;

            Dot headNode = m_Nodes[m_Nodes.Count - 1];
            Dot prevNode = m_Nodes[m_Nodes.Count - 2];
            Vector2Int step = headNode.G - prevNode.G;
            if (step.x != 0) step.x = step.x > 0 ? 1 : -1;
            if (step.y != 0) step.y = step.y > 0 ? 1 : -1;

            List<Dot> dotsInDirection = gridManager.GetDotsInDirection(headNode.G, step);
            foreach (Dot nextDot in dotsInDirection)
            {
                if (nextDot.Occupant != null && nextDot.Occupant != this)
                {
                    blockingDot = nextDot;
                    return true;  // 被其他箭头占用，阻挡
                }
            }
            return false;  // 该方向所有有效格均无其他箭头
        }

        /// <summary>
        /// 启动阻挡回弹：保存当前形状，按格距计算最大伸出距离，进入 Advancing 阶段。
        /// </summary>
        void StartBlockedBumpMovement(Dot blockingDot)
        {
            if (blockingDot == null || m_CurrentPositions == null || m_CurrentPositions.Count < 2) return;
            if (m_Nodes == null || m_Nodes.Count < 2) return;

            GridManager gridManager = ArrowMazeManager.Instance?.GridManager;
            if (gridManager == null) return;

            Dot headNode = m_Nodes[m_Nodes.Count - 1];
            Dot prevNode = m_Nodes[m_Nodes.Count - 2];
            Vector2Int step = headNode.G - prevNode.G;
            if (step.x != 0) step.x = step.x > 0 ? 1 : -1;
            if (step.y != 0) step.y = step.y > 0 ? 1 : -1;

            List<Dot> dotsInDirection = gridManager.GetDotsInDirection(headNode.G, step);
            int blockIndex = 0;
            for (int i = 0; i < dotsInDirection.Count; i++)
            {
                if (dotsInDirection[i] == blockingDot)
                {
                    blockIndex = i;
                    break;
                }
            }

            m_BlockBumpOriginalPositions = new List<Vector3>(m_CurrentPositions);
            m_BlockBumpHeadDirection = GetForwardDirectionWorld();
            m_BlockBumpEffectiveAdvance = 0f;

            float cellsToBlock = blockIndex + 1f;
            // 例如相邻阻挡：伸出约 (1 - 0.55) = 0.45 格；至少 BlockBumpMinAdvanceRatio 格
            m_BlockBumpAdvanceLimit = Mathf.Max(
                m_GridSpacing * BlockBumpMinAdvanceRatio,
                (cellsToBlock - BlockBumpStopBeforeBlockRatio) * m_GridSpacing);

            m_BlockBumpPhase = BlockBumpPhase.Advancing;
            m_IsMoving = true;
            HideHeadTrailForBlockBump();
        }

        /// <summary>
        /// 每帧更新阻挡回弹：通过 m_BlockBumpEffectiveAdvance 驱动伸出/退回，
        /// 每帧从原始快照重建形状，保证退回与伸出对称、拐弯箭头不变形。
        /// </summary>
        void UpdateBlockedBumpMovement()
        {
            if (m_BlockBumpOriginalPositions == null || m_BlockBumpOriginalPositions.Count < 1) return;

            const float Eps = 0.001f;
            float moveDist = m_MoveSpeed * SpeedMultiplier * Time.deltaTime * m_HeadSpeedRatio;

            if (m_BlockBumpPhase == BlockBumpPhase.Advancing)
            {
                m_BlockBumpEffectiveAdvance = Mathf.Min(m_BlockBumpAdvanceLimit, m_BlockBumpEffectiveAdvance + moveDist);
                if (m_BlockBumpEffectiveAdvance >= m_BlockBumpAdvanceLimit - Eps)
                    OnBlockedBumpHit();
            }
            else if (m_BlockBumpPhase == BlockBumpPhase.Retreating)
            {
                // 退回可单独调快/调慢，不影响伸出速度
                float retreatDist = moveDist * m_BlockBumpRetreatSpeedRatio;
                m_BlockBumpEffectiveAdvance = Mathf.Max(0f, m_BlockBumpEffectiveAdvance - retreatDist);
                if (m_BlockBumpEffectiveAdvance <= Eps)
                {
                    FinishBlockedBump();
                    return;
                }
            }

            RebuildBlockBumpPositions(m_BlockBumpEffectiveAdvance);
            UpdateSegmentDirections();
            UpdateVisuals();
        }

        /// <summary>
        /// 根据有效伸出距离，从点击时的原始路径分步模拟「头进尾缩」，得到当前帧形状。
        /// </summary>
        void RebuildBlockBumpPositions(float advanceDist)
        {
            const float Eps = 0.001f;
            m_CurrentPositions = new List<Vector3>(m_BlockBumpOriginalPositions);
            if (advanceDist <= Eps) return;

            float remaining = advanceDist;
            float stepSize = Mathf.Max(Eps, m_GridSpacing * BlockBumpRebuildStepRatio);
            int guard = 0;
            while (remaining > Eps && m_CurrentPositions.Count >= 1 && guard++ < 512)
            {
                float step = Mathf.Min(remaining, stepSize);
                float tailBudget = step * (m_TailSpeedRatio / Mathf.Max(Eps, m_HeadSpeedRatio));
                AdvanceHeadForBlockBump(step, tailBudget, Eps, m_BlockBumpHeadDirection);
                remaining -= step;
            }
        }

        /// <summary> 阻挡回弹的单步前进：逻辑与正常连续移动一致（头前进、尾缩短）。 </summary>
        void AdvanceHeadForBlockBump(float headMove, float tailBudget, float eps, Vector3 headDir)
        {
            int last = m_CurrentPositions.Count - 1;
            if (m_CurrentPositions.Count == 1)
            {
                m_CurrentPositions[0] += headDir * headMove;
                return;
            }

            m_CurrentPositions[last] += headDir * headMove;
            ShrinkTailAfterHeadAdvance(tailBudget, eps, headDir);
        }

        /// <summary>
        /// 伸出到上限时触发：吸附到最大伸出姿态、变红、扣生命，切换到 Retreating。
        /// </summary>
        void OnBlockedBumpHit()
        {
            if (m_BlockBumpPhase != BlockBumpPhase.Advancing) return;

            m_BlockBumpEffectiveAdvance = m_BlockBumpAdvanceLimit;
            RebuildBlockBumpPositions(m_BlockBumpEffectiveAdvance);
            m_BlockBumpPhase = BlockBumpPhase.Retreating;
            ApplyBlockedVisual();
            if (!m_HasConsumedLifeForCurrentBlock)
            {
                m_HasConsumedLifeForCurrentBlock = true;
                m_OnHitCallback?.Invoke(this);
            }
            UpdateSegmentDirections();
            UpdateVisuals();
        }

        /// <summary> 退回完成：还原路径快照，结束移动，恢复拖尾。 </summary>
        void FinishBlockedBump()
        {
            m_BlockBumpEffectiveAdvance = 0f;
            if (m_BlockBumpOriginalPositions != null)
                m_CurrentPositions = new List<Vector3>(m_BlockBumpOriginalPositions);

            m_BlockBumpPhase = BlockBumpPhase.None;
            m_BlockBumpOriginalPositions = null;
            m_IsMoving = false;
            RestoreHeadTrailAfterBlockBump();
            UpdateSegmentDirections();
            UpdateVisuals();
        }

        /// <summary> 清除阻挡回弹状态（关卡卸载、正常移出、实体隐藏时调用）。 </summary>
        void ResetBlockedBumpState()
        {
            RestoreHeadTrailAfterBlockBump();
            m_BlockBumpPhase = BlockBumpPhase.None;
            m_BlockBumpOriginalPositions = null;
            m_BlockBumpEffectiveAdvance = 0f;
        }

        /// <summary>
        /// 开始连续移动
        /// </summary>
        private void StartContinuousMovement()
        {
            ResetBlockedBumpState();
            if (ArrowMazeManager.Instance?.LevelManager != null)
                ArrowMazeManager.Instance.LevelManager.NotifyArrowStartedMoving(this);
            // 立即释放所有网格占用
            ReleaseAllNodes();

            m_IsMoving = true;
        }

        /// <summary>
        /// 释放所有节点占用
        /// </summary>
        private void ReleaseAllNodes()
        {
            foreach (var node in m_Nodes)
            {
                if (node != null && node.Occupant == this)
                {
                    node.Occupant = null;
                }
            }
        }


        /// <summary>
        /// 检查是否完全离开屏幕
        /// </summary>
        private bool IsCompletelyOffscreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return true;

            Bounds bounds = new Bounds();
            bool hasBounds = false;

            for (int i = 0; i < m_LineRenderer.positionCount; i++)
            {
                Vector3 pos = m_LineRenderer.GetPosition(i);
                if (!hasBounds)
                {
                    bounds = new Bounds(pos, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(pos);
                }
            }

            if (!hasBounds) return true;

            Vector3 min = cam.WorldToViewportPoint(bounds.min);
            Vector3 max = cam.WorldToViewportPoint(bounds.max);

            return (max.x < 0 || min.x > 1 || max.y < 0 || min.y > 1);
        }

        /// <summary>
        /// 红色闪烁效果
        /// </summary>
        private IEnumerator FlashHitColor()
        {
            ApplyLineColor(m_HitColor);
            if (m_HeadVisual != null)
            {
                SpriteRenderer sr = m_HeadVisual.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = m_HitColor;
            }
            yield return new WaitForSeconds(0.08f);
            ApplyLineColor(m_LineColor);
            if (m_HeadVisual != null)
            {
                SpriteRenderer sr = m_HeadVisual.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = m_LineColor;
            }
        }

        /// <summary>
        /// 应用线条颜色。使用 sharedMaterial 时仅设 startColor/endColor（顶点色）以支持合批；
        /// 仅在使用重复纹理 body 时有材质实例，才写材质颜色。
        /// </summary>
        private void ApplyLineColor(Color color)
        {
            if (m_LineRenderer != null)
            {
                m_LineRenderer.startColor = color;
                m_LineRenderer.endColor = color;
            }
            if (m_LineMaterial != null)
            {
                m_LineMaterial.color = color;
                if (m_LineMaterial.HasProperty("_BaseColor"))
                    m_LineMaterial.SetColor("_BaseColor", color);
            }
            if (m_HeadVisual != null)
            {
                var sr = m_HeadVisual.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
        }

        /// <summary>
        /// 设置箭头颜色（线条+头部），用于彩色/默认色切换时刷新显示。
        /// </summary>
        public void SetArrowColor(Color color)
        {
            m_LineColor = color;
            if (m_IsShowingBlockedColor)
                ApplyLineColor(m_HitColor);
            else
                ApplyLineColor(color);
        }

        /// <summary>
        /// 仅在被点击且被阻挡时调用：本箭头保持红色。
        /// </summary>
        void ApplyBlockedVisual()
        {
            if (m_IsShowingBlockedColor)
                return;

            m_IsShowingBlockedColor = true;
            ApplyLineColor(m_HitColor);
        }

        /// <summary>
        /// 本箭头不再被阻挡并开始移动时恢复颜色，并允许下次阻挡再次扣生命。
        /// </summary>
        void ClearBlockedState()
        {
            m_HasConsumedLifeForCurrentBlock = false;
            if (!m_IsShowingBlockedColor)
                return;

            m_IsShowingBlockedColor = false;
            ApplyLineColor(m_LineColor);
        }

        private void Update()
        {
            if (m_IsForceEliminating) return;
            if (m_IsMoving)
            {
                // 阻挡回弹与正常离场共用 m_IsMoving，在此分支
                if (m_BlockBumpPhase != BlockBumpPhase.None)
                    UpdateBlockedBumpMovement();
                else
                    UpdateContinuousMovement();
                return;
            }

            // 道具：显示所有引导线 - 未移动的箭头持续显示并更新辅助线
            if (ShowAllGuideLines)
            {
                ShowAssistLine();
                UpdateAssistLine();
            }
            else
            {
                HideAssistLine();
            }

            // 有触摸时只用触摸，避免 Android 上 Touch+Mouse 双通道互相干扰
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                    HandlePointerDown(t.position);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    HandlePointerUp(t.position);
                if ((t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Moved) && s_PressedArrow == this)
                    UpdateLongPress();
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                    HandlePointerDown(Input.mousePosition);
                if (Input.GetMouseButtonUp(0))
                    HandlePointerUp(Input.mousePosition);
                if (Input.GetMouseButton(0) && s_PressedArrow == this)
                    UpdateLongPress();
            }
        }

        float GetEffectiveClickRadius()
        {
            float radius = m_ClickDetectionRadius;
            if (m_Camera != null && m_Camera.orthographic && Screen.height > 0)
            {
                float worldPerPixel = (m_Camera.orthographicSize * 2f) / Screen.height;
                radius = Mathf.Max(radius, MinClickRadiusScreenPx * worldPerPixel);
            }
            return radius;
        }

        /// <summary>
        /// 若指针落在该箭头点击热区内，返回触点到线条（或头部）的最短世界距离。
        /// </summary>
        private bool TryGetClickDistance(Vector2 screenPosition, out float distance)
        {
            distance = float.MaxValue;
            if (m_Camera == null) return false;

            Vector3 screenPosWithZ = new Vector3(screenPosition.x, screenPosition.y, m_Camera.nearClipPlane);
            Vector3 worldPos = m_Camera.ScreenToWorldPoint(screenPosWithZ);
            worldPos.z = transform.position.z;

            float minDistance = float.MaxValue;
            if (m_LineRenderer != null && m_LineRenderer.positionCount >= 2)
            {
                for (int i = 0; i < m_LineRenderer.positionCount - 1; i++)
                {
                    Vector3 p1 = m_LineRenderer.GetPosition(i);
                    Vector3 p2 = m_LineRenderer.GetPosition(i + 1);
                    float d = DistanceToLineSegment(worldPos, p1, p2);
                    if (d < minDistance)
                        minDistance = d;
                }
            }
            else
            {
                Vector3 headPos = m_HeadVisual != null
                    ? m_HeadVisual.position
                    : (m_CurrentPositions != null && m_CurrentPositions.Count > 0
                        ? m_CurrentPositions[m_CurrentPositions.Count - 1]
                        : transform.position);
                minDistance = Vector2.Distance(
                    new Vector2(worldPos.x, worldPos.y),
                    new Vector2(headPos.x, headPos.y));
            }

            float clickRadius = GetEffectiveClickRadius();
            bool overlap = m_EdgeCollider != null && m_EdgeCollider.OverlapPoint(worldPos);
            if (!overlap && minDistance > clickRadius)
                return false;

            distance = minDistance;
            return true;
        }

        /// <summary> 在所有仍存在的箭头中，选出点击热区内且距触点最近的一条。 </summary>
        private static ArrowLineEntity FindNearestArrowUnderPointer(Vector2 screenPosition)
        {
            var levelManager = ArrowMazeManager.Instance?.LevelManager;
            if (levelManager == null) return null;

            ArrowLineEntity nearest = null;
            float bestDistance = float.MaxValue;
            var entities = levelManager.GetArrowEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                var arrow = entities[i];
                if (arrow == null || !arrow.Available) continue;
                if (!arrow.TryGetClickDistance(screenPosition, out float distance)) continue;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = arrow;
            }
            return nearest;
        }

        private void HandlePointerDown(Vector2 screenPosition)
        {
            if (EventSystem.current != null && UtilityEx.IsPointerOverUIObject(screenPosition)) return;
            if (ArrowMazeManager.Instance?.LevelManager != null
                && ArrowMazeManager.Instance.LevelManager.IsGameplayInputBlocked)
                return;
            if (s_PressedArrow != null) return;
            if (ArrowMazeManager.Instance?.CameraController != null && ArrowMazeManager.Instance.CameraController.IsEntranceAnimating)
                return;

            // 同一按下帧只解析一次：重叠时选距离触点最近的箭头，避免 Update 顺序误触
            if (s_PointerPickFrame == Time.frameCount)
                return;
            s_PointerPickFrame = Time.frameCount;

            var nearest = FindNearestArrowUnderPointer(screenPosition);
            if (nearest == null) return;

            s_PressedArrow = nearest;
            s_PressTime = Time.time;
            s_PressPosition = screenPosition;
            nearest.m_DidLongPressThisTouch = false;
        }

        private void HandlePointerUp(Vector2 currentPosition)
        {
            if (s_PressedArrow != this) return;
            // 按下后指针移动超过阈值视为拖拽（平移场景等），不当作点击，避免误触扣生命
            float movePx = Vector2.Distance(currentPosition, s_PressPosition);
            if (movePx > ClickDragThresholdPx)
            {
                HideAssistLine();
                s_PressedArrow = null;
                return;
            }
            // 本指针周期内发生过场景平移，则抬起不触发箭头点击（已通过按下抬起点间距判断是否为拖拽，无需相机托拽判定）
            // if (CameraController.DidPanThisTouch)
            // {
            //     HideAssistLine();
            //     s_PressedArrow = null;
            //     return;
            // }
            bool isClick = !m_DidLongPressThisTouch && (Time.time - s_PressTime) < LongPressTime;
            if (isClick)
            {
                if (ForceEliminateNextClick)
                {
                    ForceEliminateNextClick = false;
                    ForceEliminate();
                    HideAssistLine();
                    s_PressedArrow = null;
                    return;
                }
                if (ShowAllGuideLines)
                    ShowAllGuideLines = false;
                TryAdvance();
            }
            HideAssistLine();
            s_PressedArrow = null;
        }

        private void UpdateLongPress()
        {
            if ((Time.time - s_PressTime) < LongPressTime) return;
            m_DidLongPressThisTouch = true;
            if (!AssistLineEnabled) return;
            ShowAssistLine();
            UpdateAssistLine();
        }

        private Vector3 GetHeadWorldPosition()
        {
            if (m_CurrentPositions == null || m_CurrentPositions.Count < 1) return transform.position;
            return m_CurrentPositions[m_CurrentPositions.Count - 1];
        }

        private Vector3 GetForwardDirectionWorld()
        {
            if (m_CurrentPositions == null || m_CurrentPositions.Count < 2) return Vector3.right;
            Vector3 head = m_CurrentPositions[m_CurrentPositions.Count - 1];
            Vector3 prev = m_CurrentPositions[m_CurrentPositions.Count - 2];
            Vector3 dir = head - prev;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
        }

        /// <summary> 从头部沿方向到视口边缘的世界坐标终点（正交相机） </summary>
        private Vector3 GetAssistLineEndPoint(Vector3 head, Vector3 direction, Camera cam)
        {
            if (cam == null || !cam.orthographic) return head + direction * 100f;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float left = cam.transform.position.x - halfW;
            float right = cam.transform.position.x + halfW;
            float bottom = cam.transform.position.y - halfH;
            float top = cam.transform.position.y + halfH;
            Vector3 origin = head;
            Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
            float tMin = float.MaxValue;
            // 与四条边求交（射线 origin + t*dir）
            if (Mathf.Abs(dir.x) > 0.0001f) { float tx = (left - origin.x) / dir.x; if (tx > 0) tMin = Mathf.Min(tMin, tx); tx = (right - origin.x) / dir.x; if (tx > 0) tMin = Mathf.Min(tMin, tx); }
            if (Mathf.Abs(dir.y) > 0.0001f) { float ty = (bottom - origin.y) / dir.y; if (ty > 0) tMin = Mathf.Min(tMin, ty); ty = (top - origin.y) / dir.y; if (ty > 0) tMin = Mathf.Min(tMin, ty); }
            if (tMin < 0 || tMin > 1000f) return origin + dir * 50f;
            return origin + dir * tMin;
        }

        private void EnsureAssistLineRenderer()
        {
            if (m_AssistLineRenderer == null)
            {
                GameObject go = transform.Find("AssistLine")?.gameObject;
                if (go == null)
                {
                    go = new GameObject("AssistLine");
                    go.transform.SetParent(transform);
                }
                m_AssistLineRenderer = go.GetOrAddComponent<LineRenderer>();
            }
            m_AssistLineRenderer.useWorldSpace = true;
            m_AssistLineRenderer.positionCount = 2;
            m_AssistLineRenderer.startWidth = m_LineWidth * 0.8f;
            m_AssistLineRenderer.endWidth = m_LineWidth * 0.8f;
            // m_AssistLineRenderer.startColor = Color.blue;
            // m_AssistLineRenderer.endColor = Color.blue;
            // m_AssistLineRenderer.material = m_LineRenderer.material != null ? new Material(m_LineRenderer.material) : new Material(Shader.Find("Sprites/Default"));
            // m_AssistLineRenderer.sortingOrder = 2;
            m_AssistLineRenderer.enabled = false;
        }

        private void ShowAssistLine()
        {
            EnsureAssistLineRenderer();
            m_AssistLineRenderer.enabled = true;
        }

        private void HideAssistLine()
        {
            if (m_AssistLineRenderer != null)
                m_AssistLineRenderer.enabled = false;
        }

        private void UpdateAssistLine()
        {
            if (m_AssistLineRenderer == null || !m_AssistLineRenderer.enabled || m_Camera == null) return;
            Vector3 head = GetHeadWorldPosition();
            Vector3 dir = GetForwardDirectionWorld();
            Vector3 end = GetAssistLineEndPoint(head, dir, m_Camera);
            m_AssistLineRenderer.SetPosition(0, head);
            m_AssistLineRenderer.SetPosition(1, end);
        }



        /// <summary>
        /// 从 <paramref name="from"/> 到 <paramref name="to"/> 的轴对齐折线（L 形），步长约为 <paramref name="spacing"/>，用于接环时避免对角线捷径。
        /// 先走哪一轴由 <paramref name="incomingWorld"/>（身体入射方向）决定。
        /// </summary>
        private static List<Vector3> BuildOrthogonalWorldBridge(Vector3 from, Vector3 to, Vector3 incomingWorld, float spacing)
        {
            var pts = new List<Vector3>();
            float sp = Mathf.Max(1e-4f, spacing);
            if (incomingWorld.sqrMagnitude < 1e-10f)
                incomingWorld = Vector3.right;

            Vector3 cur = from;
            cur.z = to.z;
            bool horizFirst = Mathf.Abs(incomingWorld.x) >= Mathf.Abs(incomingWorld.y);

            void HorizLeg()
            {
                while (Mathf.Abs(cur.x - to.x) > 1e-4f)
                {
                    float dx = to.x - cur.x;
                    float mag = Mathf.Min(sp, Mathf.Abs(dx));
                    cur.x += Mathf.Sign(dx) * mag;
                    pts.Add(cur);
                }
            }

            void VertLeg()
            {
                while (Mathf.Abs(cur.y - to.y) > 1e-4f)
                {
                    float dy = to.y - cur.y;
                    float mag = Mathf.Min(sp, Mathf.Abs(dy));
                    cur.y += Mathf.Sign(dy) * mag;
                    pts.Add(cur);
                }
            }

            if (horizFirst)
            {
                HorizLeg();
                VertLeg();
            }
            else
            {
                VertLeg();
                HorizLeg();
            }

            if (pts.Count < 1 || (pts[pts.Count - 1] - to).sqrMagnitude > 1e-8f)
                pts.Add(to);
            else
                pts[pts.Count - 1] = new Vector3(to.x, to.y, to.z);

            for (int i = pts.Count - 1; i >= 1; i--)
            {
                if ((pts[i] - pts[i - 1]).sqrMagnitude < 1e-10f)
                    pts.RemoveAt(i);
            }

            return pts;
        }

        private bool TryBeginWatermelonLoopCapture(Vector3 prevHeadWorld, Vector3 proposedHeadWorld)
        {
            if (!WatermelonPathExtension.IsArrowLoopGuidanceAvailable())
                return false;
            WatermelonPathExtension loop = WatermelonPathExtension.ActiveInstance;
            float capR = loop.GetArrowLoopCaptureRadius(m_LineWidth);
            if (!loop.TryCaptureArrowHead(prevHeadWorld, proposedHeadWorld, capR,
                    out int seg, out float t, out Vector3 snapped, out Vector3 tan))
                return false;
            m_FollowingWatermelonLoop = true;
            m_WatermelonLoopSegIdx = seg;
            m_WatermelonLoopSegT = t;
            m_ArrowLoopTravelAccumulated = 0f;
            int hi = m_CurrentPositions.Count - 1;
            Vector3 prevHeadPos = m_CurrentPositions[hi];
            snapped.z = prevHeadPos.z;
            Vector3 tan3 = tan.sqrMagnitude > 1e-8f ? new Vector3(tan.x, tan.y, 0f).normalized : Vector3.up;
            if (hi == 0)
            {
                m_CurrentPositions[0] = snapped;
            }
            else
            {
                Vector3 incomingLeg = prevHeadWorld - m_CurrentPositions[hi - 1];
                if (incomingLeg.sqrMagnitude < 1e-10f)
                    incomingLeg = new Vector3(tan3.x, tan3.y, 0f);
                var bridge = BuildOrthogonalWorldBridge(m_CurrentPositions[hi - 1], snapped, incomingLeg, m_GridSpacing);
                m_CurrentPositions.RemoveRange(hi, m_CurrentPositions.Count - hi);
                foreach (Vector3 p in bridge)
                {
                    Vector3 q = new Vector3(p.x, p.y, snapped.z);
                    Vector3 prevPt = m_CurrentPositions[m_CurrentPositions.Count - 1];
                    if ((q - prevPt).sqrMagnitude < 1e-10f)
                        continue;
                    m_CurrentPositions.Add(q);
                }
            }
            m_LastHeadDirection = tan3;
            Vector3 headNow = m_CurrentPositions[m_CurrentPositions.Count - 1];
            if (TryBeginShrinkIntoLoopHoleIfTouching(loop, prevHeadWorld, headNow))
                return true;
            return true;
        }

        /// <summary> 本帧轨迹碰到洞口则进洞（吸附洞口、藏头、缩身）。 </summary>
        bool TryBeginShrinkIntoLoopHoleIfTouching(WatermelonPathExtension loop, Vector3 fromWorld, Vector3 toWorld,
            int arcSegBefore = -1, float arcSegTBefore = 0f, float arcAdvanceDist = 0f, float zPreserve = 0f)
        {
            if (loop == null || !loop.TouchesLoopHole(fromWorld, toWorld, arcSegBefore, arcSegTBefore, arcAdvanceDist, zPreserve))
                return false;
            Vector3 atHole = loop.TryGetLoopHoleWorld(out Vector3 hole) ? hole : toWorld;
            atHole.z = fromWorld.z;
            BeginShrinkIntoLoopHole(loop, atHole);
            return true;
        }

        private void SetHeadVisualActive(bool active)
        {
            if (m_HeadVisual != null)
                m_HeadVisual.gameObject.SetActive(active);
        }

        /// <summary> 头部碰到洞口：吸附到洞口、隐藏头部，随后仅缩短身体。 </summary>
        private void BeginShrinkIntoLoopHole(WatermelonPathExtension loop, Vector3 approxHead)
        {
            m_ShrinkingIntoLoopHole = true;
            m_FollowingWatermelonLoop = false;
            Vector3 hole = loop.TryGetLoopHoleWorld(out Vector3 hw) ? hw : approxHead;
            if (m_CurrentPositions != null && m_CurrentPositions.Count > 0)
            {
                int last = m_CurrentPositions.Count - 1;
                float z = m_CurrentPositions[last].z;
                m_CurrentPositions[last] = new Vector3(hole.x, hole.y, z);
            }
            SetHeadVisualActive(false);
            UpdateSegmentDirections();
            UpdateVisuals();
        }

        private void UpdateShrinkIntoLoopHole(float tailBudget, float eps)
        {
            WatermelonPathExtension loop = WatermelonPathExtension.ActiveInstance;
            if (loop != null && loop.TryGetLoopHoleWorld(out Vector3 hole) && m_CurrentPositions != null && m_CurrentPositions.Count > 0)
            {
                int last = m_CurrentPositions.Count - 1;
                float z = m_CurrentPositions[last].z;
                m_CurrentPositions[last] = new Vector3(hole.x, hole.y, z);
            }

            if (m_CurrentPositions == null || m_CurrentPositions.Count <= 1)
            {
                m_OnExitedCallback?.Invoke(this);
                if (Available) GF.Entity.HideEntitySafe(this);
                return;
            }

            ShrinkTailAfterHeadAdvance(tailBudget, eps, m_LastHeadDirection);
            UpdateSegmentDirections();
            UpdateVisuals();
        }

        private void ShrinkTailAfterHeadAdvance(float tailBudget, float eps, Vector3 headDirForDegenerateTail)
        {
            float remaining = tailBudget;
            while (remaining > eps && m_CurrentPositions.Count > 2)
            {
                Vector3 p0 = m_CurrentPositions[0];
                Vector3 p1 = m_CurrentPositions[1];
                float segLen = Vector3.Distance(p0, p1);
                Vector3 segDir = segLen > eps ? (p1 - p0) / segLen : Vector3.zero;
                if (segLen >= remaining)
                {
                    m_CurrentPositions[0] = p0 + segDir * remaining;
                    remaining = 0f;
                    break;
                }
                remaining -= segLen;
                m_CurrentPositions.RemoveAt(0);
            }

            if (m_CurrentPositions.Count == 2 && remaining > eps)
            {
                Vector3 p0 = m_CurrentPositions[0];
                Vector3 p1 = m_CurrentPositions[1];
                float segLen = Vector3.Distance(p0, p1);
                Vector3 segDir = segLen > eps ? (p1 - p0) / segLen : headDirForDegenerateTail;
                if (segLen <= remaining)
                {
                    m_CurrentPositions.RemoveAt(0);
                    remaining -= segLen;
                }
                else
                {
                    m_CurrentPositions[0] = p0 + segDir * remaining;
                }
            }

            while (m_CurrentPositions.Count > 1 && Vector3.Distance(m_CurrentPositions[0], m_CurrentPositions[1]) < eps)
                m_CurrentPositions.RemoveAt(0);
        }

        /// <summary>
        /// 更新连续移动：以转折点为界，每段单独计算。点击时已做阻挡检测，移动过程中不再检测。
        /// 每帧：Head 沿方向前进更多，尾部缩短更少，使箭头一边移动一边拉长；仅剩 1 点时沿用上一帧方向继续前移。
        /// <see cref="WatermelonPathExtension.IsExtensionEnabled"/> 且关卡环路就绪时：头部进入环路后顺时针行进，第一次碰到洞口则隐藏头部并向洞口缩短身体后离场。
        /// 直线飞出屏幕仍以视口离场判定收尾，不因西瓜扩展而把头部夹在屏幕边缘。
        /// </summary>
        private void UpdateContinuousMovement()
        {
            if (m_CurrentPositions == null || m_CurrentPositions.Count < 1) return;

            const float Eps = 0.001f;
            float moveDist = m_MoveSpeed * SpeedMultiplier * Time.deltaTime;
            float tailBudget = moveDist * m_TailSpeedRatio;

            if (m_ShrinkingIntoLoopHole)
            {
                UpdateShrinkIntoLoopHole(tailBudget, Eps);
                return;
            }

            if (m_FollowingWatermelonLoop)
            {
                if (!WatermelonPathExtension.IsArrowLoopGuidanceAvailable())
                    m_FollowingWatermelonLoop = false;
                else
                {
                    WatermelonPathExtension loop = WatermelonPathExtension.ActiveInstance;
                    int headListIdx = m_CurrentPositions.Count - 1;
                    float headAdvance = moveDist * (m_CurrentPositions.Count == 1 ? 1f : m_HeadSpeedRatio);
                    float zPreserve = m_CurrentPositions[headListIdx].z;
                    Vector3 oldHead = m_CurrentPositions[headListIdx];
                    int segBefore = m_WatermelonLoopSegIdx;
                    float tBefore = m_WatermelonLoopSegT;
                    loop.AdvanceArrowClockwiseOnLoop(ref m_WatermelonLoopSegIdx, ref m_WatermelonLoopSegT, headAdvance, zPreserve,
                        out Vector3 newHead, out Vector3 tan);
                    m_ArrowLoopTravelAccumulated += headAdvance;
                    if (TryBeginShrinkIntoLoopHoleIfTouching(loop, oldHead, newHead, segBefore, tBefore, headAdvance, zPreserve))
                        return;
                    float dx = newHead.x - oldHead.x;
                    float dy = newHead.y - oldHead.y;
                    float stepSq = dx * dx + dy * dy;
                    float minKeySq = Mathf.Max(1e-10f, (m_GridSpacing * 0.06f) * (m_GridSpacing * 0.06f));
                    if (stepSq > minKeySq)
                        m_CurrentPositions.Add(newHead);
                    else
                        m_CurrentPositions[headListIdx] = newHead;
                    m_LastHeadDirection = tan;
                    if (m_CurrentPositions.Count > 1)
                        ShrinkTailAfterHeadAdvance(tailBudget, Eps, tan);
                    UpdateSegmentDirections();
                    UpdateVisuals();
                    return;
                }
            }

            if (m_CurrentPositions.Count == 1)
            {
                Vector3 headBefore = m_CurrentPositions[0];
                Vector3 proposed = headBefore + m_LastHeadDirection * moveDist;
                if (WatermelonPathExtension.IsArrowLoopGuidanceAvailable())
                {
                    WatermelonPathExtension loopGuide = WatermelonPathExtension.ActiveInstance;
                    if (TryBeginShrinkIntoLoopHoleIfTouching(loopGuide, headBefore, proposed, zPreserve: headBefore.z))
                        return;
                }
                if (TryBeginWatermelonLoopCapture(headBefore, proposed))
                {
                    UpdateSegmentDirections();
                    UpdateVisuals();
                    return;
                }
                m_CurrentPositions[0] = proposed;
                UpdateVisuals();
                if (IsCompletelyOffscreen())
                {
                    m_OnExitedCallback?.Invoke(this);
                    if (Available) GF.Entity.HideEntitySafe(this);
                }
                return;
            }

            int last = m_CurrentPositions.Count - 1;
            Vector3 headDir = (m_CurrentPositions[last] - m_CurrentPositions[last - 1]).normalized;
            m_LastHeadDirection = headDir;

            float headMove = moveDist * m_HeadSpeedRatio;
            Vector3 headBefore2 = m_CurrentPositions[last];
            Vector3 proposedHead = headBefore2 + headDir * headMove;

            if (WatermelonPathExtension.IsArrowLoopGuidanceAvailable())
            {
                WatermelonPathExtension loopGuide = WatermelonPathExtension.ActiveInstance;
                if (TryBeginShrinkIntoLoopHoleIfTouching(loopGuide, headBefore2, proposedHead, zPreserve: headBefore2.z))
                    return;
            }

            if (TryBeginWatermelonLoopCapture(headBefore2, proposedHead))
            {
                ShrinkTailAfterHeadAdvance(tailBudget, Eps, m_LastHeadDirection);
                UpdateSegmentDirections();
                UpdateVisuals();
                return;
            }

            m_CurrentPositions[last] = proposedHead;

            ShrinkTailAfterHeadAdvance(tailBudget, Eps, headDir);

            UpdateSegmentDirections();
            UpdateVisuals();

            if (IsCompletelyOffscreen())
            {
                m_OnExitedCallback?.Invoke(this);
                if (Available) GF.Entity.HideEntitySafe(this);
            }
        }

        /// <summary>
        /// 更新视觉（LineRenderer、EdgeCollider、Head）。支持仅剩 1 点（仅 Head）时的显示。
        /// </summary>
        private void UpdateVisuals()
        {
            if (m_CurrentPositions == null || m_CurrentPositions.Count < 1) return;

            // 更新LineRenderer（至少 1 个点，以便仅剩 Head 时仍能显示）
            int visibleCount = Mathf.Max(1, m_CurrentPositions.Count - m_StartIndex);
            m_LineRenderer.positionCount = visibleCount;

            Vector3 basePosition = transform.position;

            for (int i = 0; i < visibleCount; i++)
            {
                int posIndex = m_StartIndex + i;
                if (posIndex < m_CurrentPositions.Count)
                {
                    m_LineRenderer.SetPosition(i, m_CurrentPositions[posIndex]);
                }
            }

            // 重复纹理 body：仅在有材质实例时按长度设置 tiling（此类箭头无法合批）
            if (m_UseRepeatingTextureBody && m_LineMaterial != null && m_TextureRepeatWorldLength > 0.001f && visibleCount >= 2)
            {
                float totalLength = 0f;
                for (int i = 0; i < visibleCount - 1; i++)
                {
                    Vector3 a = m_LineRenderer.GetPosition(i);
                    Vector3 b = m_LineRenderer.GetPosition(i + 1);
                    totalLength += Vector3.Distance(a, b);
                }
                float tileX = Mathf.Max(0.001f, totalLength / m_TextureRepeatWorldLength);
                if (m_LineMaterial.HasProperty("_MainTex"))
                    m_LineMaterial.SetTextureScale("_MainTex", new Vector2(tileX, 1f));
                if (m_LineMaterial.HasProperty("_BaseMap"))
                    m_LineMaterial.SetTextureScale("_BaseMap", new Vector2(tileX, 1f));
            }

            // 更新EdgeCollider
            if (visibleCount >= 2)
            {
                Vector2[] points = new Vector2[visibleCount];
                for (int i = 0; i < visibleCount; i++)
                {
                    int posIndex = m_StartIndex + i;
                    if (posIndex < m_CurrentPositions.Count)
                    {
                        Vector3 localPos = m_CurrentPositions[posIndex] - basePosition;
                        points[i] = new Vector2(localPos.x, localPos.y);
                    }
                }
                m_EdgeCollider.points = points;
            }

            // 更新Head位置和旋转（进洞缩短阶段头部已隐藏）
            if (m_HeadVisual != null && !m_ShrinkingIntoLoopHole && m_CurrentPositions.Count > 0)
            {
                Vector3 headPos = m_CurrentPositions[m_CurrentPositions.Count - 1];
                m_HeadVisual.position = headPos;

                // 已进入环路：头部朝向必须与环路切线一致（碰环当下即转向，不沿用蛇身段方向）。
                if (m_FollowingWatermelonLoop && m_LastHeadDirection.sqrMagnitude > 0.0001f)
                {
                    Vector3 direction = m_LastHeadDirection;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    m_HeadVisual.rotation = Quaternion.Euler(0, 0, angle + m_HeadInitialRotationOffset);
                }
                else if (m_CurrentPositions.Count >= 2)
                {
                    Vector3 direction = headPos - m_CurrentPositions[m_CurrentPositions.Count - 2];
                    if (direction.magnitude > 0.001f)
                    {
                        direction.Normalize();
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        m_HeadVisual.rotation = Quaternion.Euler(0, 0, angle + m_HeadInitialRotationOffset);
                    }
                }
                else if (m_LastHeadDirection.sqrMagnitude > 0.0001f)
                {
                    Vector3 direction = m_LastHeadDirection;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    m_HeadVisual.rotation = Quaternion.Euler(0, 0, angle + m_HeadInitialRotationOffset);
                }
            }
        }

        /// <summary>
        /// 道具：强制消除本箭头。无视规则释放网格占用，从尾部向头部逐渐消失（1 秒），不触发移动逻辑。
        /// </summary>
        public void ForceEliminate()
        {
            if (IsMoving || m_IsForceEliminating) return;
            CancelClickMoveDelay();
            if (ArrowMazeManager.Instance?.LevelManager != null)
            {
                ArrowMazeManager.Instance.LevelManager.HideFingerGuide();
                ArrowMazeManager.Instance.LevelManager.NotifyArrowEliminatedForCombo(this);
                ArrowMazeManager.Instance.LevelManager.GrantArrowEliminationReward(this);
            }
            GF.Event.Fire(this, ArrowMazeForceEliminateUsedEventArgs.Create());
            ReleaseAllNodes();
            StartCoroutine(ForceEliminateCoroutine());
        }

        private IEnumerator ForceEliminateCoroutine()
        {
            m_IsForceEliminating = true;
            if (m_CurrentPositions == null || m_CurrentPositions.Count <= 1)
            {
                m_OnExitedCallback?.Invoke(this);
                if (Available) GF.Entity.HideEntitySafe(this);
                m_IsForceEliminating = false;
                yield break;
            }
            float totalLength = 0f;
            for (int i = 0; i < m_CurrentPositions.Count - 1; i++)
                totalLength += Vector3.Distance(m_CurrentPositions[i], m_CurrentPositions[i + 1]);
            float shrinkSpeed = totalLength / ForceEliminateDuration;
            const float Eps = 0.001f;
            float elapsed = 0f;
            while (elapsed < ForceEliminateDuration && m_CurrentPositions != null && m_CurrentPositions.Count > 1)
            {
                float remaining = shrinkSpeed * Time.deltaTime;
                while (remaining > Eps && m_CurrentPositions.Count > 2)
                {
                    Vector3 p0 = m_CurrentPositions[0];
                    Vector3 p1 = m_CurrentPositions[1];
                    float segLen = Vector3.Distance(p0, p1);
                    Vector3 segDir = segLen > Eps ? (p1 - p0) / segLen : Vector3.zero;
                    if (segLen >= remaining)
                    {
                        m_CurrentPositions[0] = p0 + segDir * remaining;
                        remaining = 0f;
                        break;
                    }
                    remaining -= segLen;
                    m_CurrentPositions.RemoveAt(0);
                }
                if (m_CurrentPositions.Count == 2 && remaining > Eps)
                {
                    Vector3 p0 = m_CurrentPositions[0];
                    Vector3 p1 = m_CurrentPositions[1];
                    float segLen = Vector3.Distance(p0, p1);
                    Vector3 segDir = segLen > Eps ? (p1 - p0) / segLen : Vector3.zero;
                    if (segLen <= remaining)
                    {
                        m_CurrentPositions.RemoveAt(0);
                        remaining -= segLen;
                    }
                    else
                    {
                        m_CurrentPositions[0] = p0 + segDir * remaining;
                    }
                }
                while (m_CurrentPositions.Count > 1 && Vector3.Distance(m_CurrentPositions[0], m_CurrentPositions[1]) < Eps)
                    m_CurrentPositions.RemoveAt(0);
                UpdateSegmentDirections();
                UpdateVisuals();
                elapsed += Time.deltaTime;
                yield return null;
            }
            m_OnExitedCallback?.Invoke(this);
            if (Available) GF.Entity.HideEntitySafe(this);
            m_IsForceEliminating = false;
        }

        /// <summary>
        /// 计算点到线段的距离
        /// </summary>
        private float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float lineLength = line.magnitude;

            if (lineLength < 0.001f)
            {
                // 线段退化为点
                return Vector3.Distance(point, lineStart);
            }

            Vector3 lineNormalized = line / lineLength;
            Vector3 toPoint = point - lineStart;
            float projection = Vector3.Dot(toPoint, lineNormalized);

            // 限制投影在线段范围内
            projection = Mathf.Clamp(projection, 0f, lineLength);

            Vector3 closestPoint = lineStart + lineNormalized * projection;
            return Vector3.Distance(point, closestPoint);
        }

    }
}
