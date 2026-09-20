using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// <see cref="CommonHelper.IsSpec"/> 为真时启用（<see cref="IsExtensionEnabled"/>）：参数在 Inspector 上配置，
    /// 逐个从洞口生成西瓜，沿外周环路匀速移动，回洞消失；仅在与<strong>移动中箭头头部</strong>同格时西瓜生命 -1。
    /// 初始生命随出洞次序递增（第 1 个为 <see cref="m_WatermelonStartHp"/>，之后每个 +1）。
    /// <see cref="MelonPathWatermelonEntity.MelonImageNodeName"/> 子节点按弧长滚动，生命文本保持不旋转。
    /// 移动中箭头头部进入环路带状区（含格距外扩路径）后：头部约束在相机视口内，沿环路<strong>顺时针</strong>行进；头部第一次碰到洞口时隐藏头部、身体向洞口缩短后离场。
    /// </summary>
    public class WatermelonPathExtension : MonoBehaviour
    {
        const int DefaultMelonHp = 10;
        const float DefaultMoveInterval = 0.5f;
        const float DefaultSpawnInterval = 0.9f;
        const float DefaultPathPadScale = 1.3f;
        /// <summary> 与 MelonPathHole / MelonPathWatermelon 预制体 SpriteRenderer.m_Size 一致。 </summary>
        const float LoopVisualSpriteWorldSize = 2.56f;

        const string HolePrefabName = "MelonPathHole";
        const string WatermelonPrefabName = "MelonPathWatermelon";

        [Header("西瓜环路（仅组件内可调，已不从 GameConfig 读取）")]
        [SerializeField] int m_WatermelonStartHp = DefaultMelonHp;
        [SerializeField] float m_WatermelonSpawnIntervalSec = DefaultSpawnInterval;
        [SerializeField] float m_WatermelonPathPadding = DefaultPathPadScale;
        [Tooltip(">0 为匀速世界速度；0 则按 spacing / WatermelonMoveIntervalSec")]
        [SerializeField] float m_WatermelonMoveSpeed;
        [SerializeField] float m_WatermelonMoveIntervalSec = DefaultMoveInterval;

        sealed class ActiveMelon
        {
            public int EntityId = -1;
            public MelonPathWatermelonEntity Entity;
            public Transform Transform;
            public Transform MelonImageTransform;
            /// <summary> 当前段起点下标，段为 PathWorld[i]→PathWorld[(i+1)%n] </summary>
            public int SegmentIndex;
            /// <summary> 在当前段上的比例 [0,1) </summary>
            public float SegmentT;
            public bool HasLeftHole;
            public int Hp;
            /// <summary> 与移动箭头头部同格时已扣过血，离开时清零以便下次再碰再扣 </summary>
            public bool ArrowHeadOverlapLatch;
            /// <summary> 优先 TMP 世界文本，其次 legacy TextMesh </summary>
            public TMP_Text HpLabelTmp;
            public TextMesh HpLabelMesh;
            public float RollAngleDeg;
            public float RollRadiusWorld;
            public bool IsDefeated;
            public float DefeatFxRemainingSec;
        }

        GameObject m_Root;
        readonly List<Vector2Int> m_PathGrid = new List<Vector2Int>();
        readonly List<Vector3> m_PathWorld = new List<Vector3>();
        readonly List<ActiveMelon> m_ActiveMelons = new List<ActiveMelon>();
        float m_SpawnTimer;
        float m_SpawnInterval;
        float m_MoveSpeedWorld;
        float m_PadScale;
        int m_InitialHpPerMelon;
        /// <summary> 本关已从洞口生成的西瓜次数（用于初始生命递增：第 n 个为 StartHp + n - 1）。 </summary>
        int m_MelonSpawnCount;
        bool m_StreamActive;
        int m_HoleEntityId = -1;
        GridManager m_Grid;
        LevelManager m_LevelOwner;
        float m_LoopPerimeterWorld;
        float m_MelonRollRadiusWorld;

        public static bool IsExtensionEnabled()
        {
            return !CommonHelper.IsSpec();
        }

        /// <summary> 本关已激活的环路实例（用于箭头吸附行进，见 <see cref="BeginLevel"/> / <see cref="EndLevel"/>）。 </summary>
        public static WatermelonPathExtension ActiveInstance { get; private set; }

        /// <summary>
        /// 箭头环路引导是否可用：<see cref="IsExtensionEnabled"/> 为 true，且本关已成功构建外周路径。
        /// </summary>
        public static bool IsArrowLoopGuidanceAvailable()
        {
            return IsExtensionEnabled()
                && ActiveInstance != null
                && ActiveInstance.m_StreamActive
                && ActiveInstance.m_PathWorld != null
                && ActiveInstance.m_PathWorld.Count >= 2;
        }

        public float GetArrowLoopCaptureRadius(float arrowLineWidthWorld)
        {
            float spacing = m_Grid != null && m_Grid.GridDef != null ? m_Grid.GridDef.spacing : 1f;
            return Mathf.Max(0.08f, arrowLineWidthWorld * 0.55f + spacing * 0.34f);
        }

        public float GetLoopPerimeterWorld() => Mathf.Max(1e-4f, m_LoopPerimeterWorld);

        /// <summary>
        /// 获取本关西瓜环路在世界空间中的边界（含外扩路径与西瓜/洞口精灵尺寸），供相机适配时并入显示范围。
        /// </summary>
        public bool TryGetLoopWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (!m_StreamActive || m_PathWorld == null || m_PathWorld.Count < 1)
                return false;

            float spacing = m_Grid != null && m_Grid.GridDef != null ? m_Grid.GridDef.spacing : 1f;
            float halfW = LoopVisualSpriteWorldSize * 0.5f;
            // HpLabel 在预制体中约 +0.7y，扣血飘字再向上约 0.55y
            float halfH = halfW + spacing * 0.85f;

            bounds = new Bounds(m_PathWorld[0], Vector3.zero);
            for (int i = 0; i < m_PathWorld.Count; i++)
            {
                Vector3 p = m_PathWorld[i];
                bounds.Encapsulate(new Vector3(p.x - halfW, p.y - halfW, p.z));
                bounds.Encapsulate(new Vector3(p.x + halfW, p.y + halfH, p.z));
            }

            // varCameraView 边沿与 UI 遮挡额外留白
            bounds.Expand(Mathf.Max(0.25f, spacing * 0.35f));
            return true;
        }

        float HoleArriveEpsilonWorld()
        {
            float spacing = m_Grid != null && m_Grid.GridDef != null ? m_Grid.GridDef.spacing : 1f;
            return Mathf.Max(0.055f, spacing * 0.1f);
        }

        static float HorizontalDistSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        void RecomputeLoopPerimeter()
        {
            m_LoopPerimeterWorld = 0f;
            int n = m_PathWorld.Count;
            if (n < 2) return;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = m_PathWorld[i];
                Vector3 b = m_PathWorld[(i + 1) % n];
                m_LoopPerimeterWorld += Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
            }
        }

        float SegmentLenXY(int segIdx)
        {
            int n = m_PathWorld.Count;
            Vector3 a = m_PathWorld[segIdx];
            Vector3 b = m_PathWorld[(segIdx + 1) % n];
            return Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
        }

        /// <summary> 从洞口顶点起沿「西瓜同序」方向的弧长参数 s∈[0,Perimeter)。 </summary>
        float ForwardArcLength(int segIdx, float segT)
        {
            float acc = 0f;
            for (int i = 0; i < segIdx; i++)
                acc += SegmentLenXY(i);
            acc += Mathf.Clamp01(segT) * SegmentLenXY(segIdx);
            return acc;
        }

        void ForwardArcToSegment(float sUnnormalized, out int segIdx, out float segT)
        {
            segIdx = 0;
            segT = 0f;
            float perim = GetLoopPerimeterWorld();
            float s = sUnnormalized % perim;
            if (s < 0f) s += perim;
            int n = m_PathWorld.Count;
            float acc = 0f;
            for (int i = 0; i < n; i++)
            {
                float len = SegmentLenXY(i);
                if (len < 1e-8f)
                    continue;
                if (acc + len >= s - 1e-8f)
                {
                    segIdx = i;
                    segT = (s - acc) / len;
                    return;
                }
                acc += len;
            }
            segIdx = 0;
            segT = 0f;
        }

        Vector3 PositionFromForwardArc(float s, float zPreserve)
        {
            ForwardArcToSegment(s, out int seg, out float t);
            int n = m_PathWorld.Count;
            Vector3 a = m_PathWorld[seg];
            Vector3 b = m_PathWorld[(seg + 1) % n];
            Vector3 p = Vector3.Lerp(a, b, Mathf.Clamp01(t));
            p.z = zPreserve;
            return p;
        }

        /// <summary> 顺时针沿环路（相对西瓜前进方向的反向）移动 advanceDist；头部切线指向顺时针行进方向。 </summary>
        public void AdvanceArrowClockwiseOnLoop(ref int segIdx, ref float segT, float advanceDist, float zPreserve,
            out Vector3 headWorld, out Vector3 tangentClockwise)
        {
            float perim = GetLoopPerimeterWorld();
            float s = ForwardArcLength(segIdx, segT);
            float sNext = s - advanceDist;
            while (sNext < 0f) sNext += perim;
            ForwardArcToSegment(sNext, out segIdx, out segT);
            headWorld = PositionFromForwardArc(sNext, zPreserve);

            int n = m_PathWorld.Count;
            Vector3 a = m_PathWorld[segIdx];
            Vector3 b = m_PathWorld[(segIdx + 1) % n];
            Vector3 melonFwd = new Vector3(b.x - a.x, b.y - a.y, 0f);
            float sq = melonFwd.sqrMagnitude;
            tangentClockwise = sq > 1e-12f ? (-melonFwd / Mathf.Sqrt(sq)) : Vector3.right;
        }

        /// <summary> 头部轨迹是否进入环路带状区域（含外扩路径采样）。捕获后切线为顺时针方向。 </summary>
        public bool TryCaptureArrowHead(Vector3 prevHeadWorld, Vector3 proposedHeadWorld, float captureRadius,
            out int segIdx, out float segT, out Vector3 snappedHeadWorld, out Vector3 tangentClockwise)
        {
            segIdx = 0;
            segT = 0f;
            snappedHeadWorld = proposedHeadWorld;
            tangentClockwise = Vector3.right;
            if (!m_StreamActive || m_PathWorld == null || m_PathWorld.Count < 2)
                return false;

            float r2 = captureRadius * captureRadius;
            float zKeep = proposedHeadWorld.z;
            for (float u = 0f; u <= 1.001f; u += 0.25f)
            {
                Vector3 sample = Vector3.Lerp(prevHeadWorld, proposedHeadWorld, Mathf.Clamp01(u));
                ClosestPointOnLoopXY(sample, out _, out _, out Vector3 cp, out _);
                if (HorizontalDistSq(sample, cp) <= r2)
                {
                    ClosestPointOnLoopXY(proposedHeadWorld, out segIdx, out segT, out snappedHeadWorld, out Vector3 melonFwd);
                    snappedHeadWorld.z = zKeep;
                    Vector3 cw = new Vector3(-melonFwd.x, -melonFwd.y, 0f);
                    float tsq = cw.x * cw.x + cw.y * cw.y;
                    tangentClockwise = tsq > 1e-12f ? cw / Mathf.Sqrt(tsq) : Vector3.right;
                    return true;
                }
            }

            return false;
        }

        void ClosestPointOnLoopXY(Vector3 pWorld, out int segIdx, out float segT, out Vector3 closestWorld, out Vector3 tangentForwardMelonDir)
        {
            int n = m_PathWorld.Count;
            segIdx = 0;
            segT = 0f;
            closestWorld = m_PathWorld[0];
            tangentForwardMelonDir = Vector3.right;
            float best = float.MaxValue;
            var p2 = new Vector2(pWorld.x, pWorld.y);
            for (int i = 0; i < n; i++)
            {
                Vector3 a3 = m_PathWorld[i];
                Vector3 b3 = m_PathWorld[(i + 1) % n];
                var a = new Vector2(a3.x, a3.y);
                var b = new Vector2(b3.x, b3.y);
                Vector2 ab = b - a;
                float abSqr = ab.sqrMagnitude;
                float tLin = abSqr > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p2 - a, ab) / abSqr) : 0f;
                Vector2 c = a + ab * tLin;
                float d = (p2 - c).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    segIdx = i;
                    segT = tLin;
                    closestWorld = new Vector3(c.x, c.y, pWorld.z);
                    tangentForwardMelonDir = abSqr > 1e-12f ? new Vector3(ab.x, ab.y, 0f).normalized : Vector3.right;
                }
            }
        }

        /// <summary> 头部世界坐标是否进入洞口判定范围。 </summary>
        public bool IsNearLoopHole(Vector3 headWorld)
        {
            if (m_PathWorld == null || m_PathWorld.Count < 1) return false;
            float eps = HoleArriveEpsilonWorld();
            return HorizontalDistSq(headWorld, m_PathWorld[0]) <= eps * eps;
        }

        /// <summary>
        /// 本帧移动是否碰到洞口：端点在判定范围内、世界线段擦过洞口、或沿环路顺时针步进经过弧长 s=0。
        /// </summary>
        public bool TouchesLoopHole(Vector3 fromWorld, Vector3 toWorld, int arcSegBefore = -1, float arcSegTBefore = 0f,
            float arcAdvanceDist = 0f, float zPreserve = 0f)
        {
            if (m_PathWorld == null || m_PathWorld.Count < 1) return false;
            if (IsNearLoopHole(fromWorld) || IsNearLoopHole(toWorld)) return true;
            if (DoesWorldSegmentTouchLoopHole(fromWorld, toWorld)) return true;
            if (arcAdvanceDist > 0f && arcSegBefore >= 0
                && DoesClockwiseArcStepTouchLoopHole(arcSegBefore, arcSegTBefore, arcAdvanceDist, zPreserve))
                return true;
            return false;
        }

        /// <summary> 世界空间线段（含采样）是否经过洞口。 </summary>
        public bool DoesWorldSegmentTouchLoopHole(Vector3 fromWorld, Vector3 toWorld)
        {
            if (m_PathWorld == null || m_PathWorld.Count < 1) return false;
            float epsSq = HoleArriveEpsilonWorld() * HoleArriveEpsilonWorld();
            var hole = new Vector2(m_PathWorld[0].x, m_PathWorld[0].y);
            var a = new Vector2(fromWorld.x, fromWorld.y);
            var b = new Vector2(toWorld.x, toWorld.y);
            if (DistSqPointSegmentXY(hole, a, b) <= epsSq) return true;
            const int samples = 8;
            for (int i = 1; i < samples; i++)
            {
                float u = i / (float)samples;
                var p = Vector2.Lerp(a, b, u);
                if ((p - hole).sqrMagnitude <= epsSq) return true;
            }
            return false;
        }

        /// <summary> 沿环路顺时针步进（弧长减少）是否经过洞口 s=0。 </summary>
        public bool DoesClockwiseArcStepTouchLoopHole(int segIdxBefore, float segTBefore, float advanceDist, float zPreserve)
        {
            if (m_PathWorld == null || m_PathWorld.Count < 2 || advanceDist <= 0f) return false;
            float s0 = ForwardArcLength(segIdxBefore, segTBefore);
            if (IsNearLoopHole(PositionFromForwardArc(s0, zPreserve))) return true;
            if (s0 - advanceDist < 0f) return true;
            float s1 = s0 - advanceDist;
            if (IsNearLoopHole(PositionFromForwardArc(s1, zPreserve))) return true;
            const int samples = 8;
            for (int i = 1; i < samples; i++)
            {
                float u = i / (float)samples;
                float s = Mathf.Lerp(s0, s1, u);
                if (IsNearLoopHole(PositionFromForwardArc(s, zPreserve))) return true;
            }
            return false;
        }

        static float DistSqPointSegmentXY(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float abSqr = ab.sqrMagnitude;
            if (abSqr < 1e-12f) return (p - a).sqrMagnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abSqr);
            Vector2 c = a + ab * t;
            return (p - c).sqrMagnitude;
        }

        /// <summary> 环路洞口世界坐标（路径起点）。 </summary>
        public bool TryGetLoopHoleWorld(out Vector3 holeWorld)
        {
            if (m_PathWorld == null || m_PathWorld.Count < 1)
            {
                holeWorld = default;
                return false;
            }
            holeWorld = m_PathWorld[0];
            return true;
        }

        public void BeginLevel(LevelManager levelManager)
        {
            EndLevel();

            m_InitialHpPerMelon = Mathf.Max(1, m_WatermelonStartHp);
            float moveInterval = Mathf.Max(0.15f, m_WatermelonMoveIntervalSec);
            m_SpawnInterval = Mathf.Max(0.2f, m_WatermelonSpawnIntervalSec);
            m_PadScale = Mathf.Max(0.05f, m_WatermelonPathPadding);

            m_Grid = ArrowMazeManager.Instance?.GridManager;
            if (m_Grid == null || m_Grid.GridDef == null)
            {
                Log.Warning("WatermelonPathExtension: GridManager 未就绪");
                return;
            }

            BuildPerimeterPath(m_Grid.GridDef.width, m_Grid.GridDef.height, m_PathGrid);
            if (m_PathGrid.Count < 2)
            {
                Log.Warning("WatermelonPathExtension: 路径点过少");
                return;
            }

            m_PathWorld.Clear();
            foreach (var g in m_PathGrid)
                m_PathWorld.Add(MelonWorldAtGrid(g));

            RecomputeLoopPerimeter();

            float spacing = m_Grid.GridDef.spacing;
            m_MoveSpeedWorld = m_WatermelonMoveSpeed > 0f ? m_WatermelonMoveSpeed : spacing / moveInterval;
            m_MelonRollRadiusWorld = Mathf.Max(0.15f, spacing * 0.55f);

            m_Root = new GameObject("WatermelonPathExtensionRoot");
            m_Root.transform.SetParent(levelManager != null ? levelManager.transform : transform, false);

            float holeScale = 1f;
            Vector3 holeWorld = m_PathWorld[0];

            var holeParams = MelonPathHoleEntityParams.Create(
                holeWorld + Vector3.forward * (-0.02f),
                Vector3.one * holeScale,
                m_Root.transform);

            m_HoleEntityId = GF.Entity.ShowEntity<MelonPathHoleEntity>(
                HolePrefabName,
                Const.EntityGroup.ArrowLine,
                holeParams);

            m_LevelOwner = levelManager;
            m_SpawnTimer = 0f;
            m_MelonSpawnCount = 0;
            m_StreamActive = true;
            ActiveInstance = this;

            TrySpawnMelon();
        }

        void TrySpawnMelon()
        {
            if (!m_StreamActive || m_PathGrid.Count < 2 || m_Grid == null || m_Root == null) return;

            float spacing = m_Grid.GridDef.spacing;
            float melonScale = 1f;
            int spawnIndex = m_MelonSpawnCount;
            m_MelonSpawnCount++;
            var melon = new ActiveMelon
            {
                SegmentIndex = 0,
                SegmentT = 0f,
                HasLeftHole = false,
                Hp = m_InitialHpPerMelon + spawnIndex
            };

            var melonParams = MelonPathWatermelonEntityParams.Create(
                m_PathWorld[0] + Vector3.forward * (-0.05f),
                Vector3.one * melonScale,
                m_Root.transform);
            melonParams.OnWatermelonShown = e =>
            {
                melon.Entity = e;
                melon.Transform = e.CachedTransform;
                melon.MelonImageTransform = e.MelonImageTransform;
                if (melon.MelonImageTransform == null && melon.Transform != null)
                    melon.MelonImageTransform = melon.Transform.Find(MelonPathWatermelonEntity.MelonImageNodeName);
                BindMelonHpLabel(melon);
                RefreshMelonHpLabel(melon);
                ApplyMelonVisual(melon);
            };

            melon.EntityId = GF.Entity.ShowEntity<MelonPathWatermelonEntity>(
                WatermelonPrefabName,
                Const.EntityGroup.ArrowLine,
                melonParams);

            m_ActiveMelons.Add(melon);
        }

        static void BuildPerimeterPath(int width, int height, List<Vector2Int> outPath)
        {
            outPath.Clear();
            if (width < 1 || height < 1) return;

            if (width == 1 && height == 1)
            {
                outPath.Add(Vector2Int.zero);
                return;
            }

            if (width == 1)
            {
                for (int y = 0; y < height; y++)
                    outPath.Add(new Vector2Int(0, y));
                return;
            }

            if (height == 1)
            {
                for (int x = width - 1; x >= 0; x--)
                    outPath.Add(new Vector2Int(x, 0));
                return;
            }

            for (int x = width - 1; x >= 0; x--)
                outPath.Add(new Vector2Int(x, 0));
            for (int y = 1; y < height; y++)
                outPath.Add(new Vector2Int(0, y));
            for (int x = 1; x < width; x++)
                outPath.Add(new Vector2Int(x, height - 1));
            for (int y = height - 2; y >= 1; y--)
                outPath.Add(new Vector2Int(width - 1, y));
        }

        Vector3 MelonWorldAtGrid(Vector2Int g)
        {
            Vector3 basePos = m_Grid.GridToWorld(g);
            var def = m_Grid.GridDef;
            int w = def.width;
            int h = def.height;
            float pad = m_PadScale * def.spacing;
            Vector3 off = Vector3.zero;
            if (g.y == 0)
                off += Vector3.up * pad;
            if (g.y == h - 1)
                off -= Vector3.up * pad;
            if (g.x == 0)
                off -= Vector3.right * pad;
            if (g.x == w - 1)
                off += Vector3.right * pad;
            return basePos + off;
        }

        static Vector3 SampleMelonWorld(ActiveMelon m, List<Vector3> pathWorld)
        {
            int n = pathWorld.Count;
            int next = (m.SegmentIndex + 1) % n;
            return Vector3.Lerp(pathWorld[m.SegmentIndex], pathWorld[next], m.SegmentT);
        }

        Vector3 SampleMelonWorld(ActiveMelon m) => SampleMelonWorld(m, m_PathWorld);

        void ApplyMelonVisual(ActiveMelon melon)
        {
            if (melon.Transform == null) return;

            melon.Transform.position = SampleMelonWorld(melon) + Vector3.forward * (-0.05f);
            melon.Transform.rotation = Quaternion.identity;
            if (melon.MelonImageTransform == null) return;

            float arc = ForwardArcLength(melon.SegmentIndex, melon.SegmentT);
            float rollDeg = arc / m_MelonRollRadiusWorld * Mathf.Rad2Deg;
            melon.MelonImageTransform.localRotation = Quaternion.Euler(0f, 0f, rollDeg);
        }

        /// <summary> 与 <see cref="GridManager.GridToWorld"/> 互逆，用于箭头头部世界坐标对齐关卡格 </summary>
        Vector2Int WorldPosToLevelGrid(Vector3 world)
        {
            var def = m_Grid.GridDef;
            Vector3 local = world - def.origin;
            int x = Mathf.RoundToInt(local.x / def.spacing + (def.width - 1) * 0.5f);
            int y = Mathf.RoundToInt((def.height - 1) * 0.5f - local.y / def.spacing);
            return new Vector2Int(x, y);
        }

        /// <summary> 与离散路径格对齐：更靠近段终点则算下一格 </summary>
        int DiscretePathIndex(ActiveMelon m)
        {
            int n = m_PathWorld.Count;
            return m.SegmentT < 0.5f ? m.SegmentIndex : (m.SegmentIndex + 1) % n;
        }

        void BindMelonHpLabel(ActiveMelon melon)
        {
            melon.HpLabelTmp = null;
            melon.HpLabelMesh = null;
            if (melon.Transform == null) return;
            var hpTf = melon.Transform.Find(MelonPathWatermelonEntity.HpLabelNodeName);
            if (hpTf == null) return;

            var tmp = hpTf.GetComponent<TextMeshPro>();
            if (tmp != null)
                melon.HpLabelTmp = tmp;
            else
                melon.HpLabelMesh = hpTf.GetComponent<TextMesh>();

            if (melon.HpLabelTmp == null && melon.HpLabelMesh == null)
            {
                tmp = hpTf.gameObject.AddComponent<TextMeshPro>();
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                tmp.fontSize = 6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.8078f, 0.3843f, 0.1529f);
                tmp.sortingOrder = 10;
                melon.HpLabelTmp = tmp;
            }

            if (melon.HpLabelMesh != null)
            {
                melon.HpLabelMesh.anchor = TextAnchor.MiddleCenter;
                melon.HpLabelMesh.alignment = TextAlignment.Center;
                melon.HpLabelMesh.characterSize = 0.12f;
                melon.HpLabelMesh.fontSize = 32;
                melon.HpLabelMesh.fontStyle = FontStyle.Bold;
                melon.HpLabelMesh.color = Color.white;
            }
        }

        static string FormatMelonHpText(int hp) => Mathf.Max(0, hp).ToString();

        static void RefreshMelonHpLabel(ActiveMelon melon)
        {
            string s = FormatMelonHpText(melon.Hp);
            if (melon.HpLabelTmp != null)
                melon.HpLabelTmp.text = s;
            if (melon.HpLabelMesh != null)
                melon.HpLabelMesh.text = s;
        }

        void BeginMelonDefeat(ActiveMelon melon, int listIndex)
        {
            if (melon.IsDefeated) return;

            melon.IsDefeated = true;
            melon.ArrowHeadOverlapLatch = true;
            melon.Hp = 0;
            RefreshMelonHpLabel(melon);

            float fxDuration = melon.Entity != null
                ? melon.Entity.PlayDestroyFxAndHideVisuals()
                : DefaultMelonDestroyFxDurationSec;
            if (melon.Entity == null)
                HideMelonGameplayVisualsFallback(melon);

            melon.DefeatFxRemainingSec = Mathf.Max(0.2f, fxDuration);
        }

        void HideMelonGameplayVisualsFallback(ActiveMelon melon)
        {
            if (melon.MelonImageTransform != null)
                melon.MelonImageTransform.gameObject.SetActive(false);
            if (melon.Transform == null) return;
            var hpLabel = melon.Transform.Find(MelonPathWatermelonEntity.HpLabelNodeName);
            if (hpLabel != null)
                hpLabel.gameObject.SetActive(false);
            var hpFloat = melon.Transform.Find(MelonPathWatermelonEntity.HpFloatLabelNodeName);
            if (hpFloat != null)
                hpFloat.gameObject.SetActive(false);
        }

        void TickMelonDefeat(ActiveMelon melon, float dt, int listIndex)
        {
            melon.DefeatFxRemainingSec -= dt;
            if (melon.DefeatFxRemainingSec > 0f) return;
            RemoveMelonAt(listIndex);
        }

        const float DefaultMelonDestroyFxDurationSec = 1.5f;

        void ApplyArrowHeadHitsIfOnSameCell(ActiveMelon melon, int listIndex)
        {
            if (melon.IsDefeated || melon.Hp <= 0 || m_PathGrid.Count == 0) return;

            Vector2Int melonG = m_PathGrid[DiscretePathIndex(melon)];
            float spacing = Mathf.Max(0.01f, m_Grid.GridDef.spacing);
            float hitR = Mathf.Max(spacing * 0.42f, 0.2f);
            float hitRsq = hitR * hitR;

            Vector2 melonWXY;
            if (melon.Transform != null)
                melonWXY = new Vector2(melon.Transform.position.x, melon.Transform.position.y);
            else
            {
                var sm = SampleMelonWorld(melon);
                melonWXY = new Vector2(sm.x, sm.y);
            }

            bool headTouchesMelon = false;
            var level = m_LevelOwner != null ? m_LevelOwner : ArrowMazeManager.Instance?.LevelManager;
            if (level != null)
            {
                foreach (var arrow in level.GetArrowEntities())
                {
                    if (arrow == null || !arrow.IsMoving) continue;
                    Vector3 hp = arrow.HeadWorldPosition;
                    if (WorldPosToLevelGrid(hp) == melonG)
                    {
                        headTouchesMelon = true;
                        break;
                    }
                    float dx = hp.x - melonWXY.x, dy = hp.y - melonWXY.y;
                    if (dx * dx + dy * dy <= hitRsq)
                    {
                        headTouchesMelon = true;
                        break;
                    }
                }
            }

            if (headTouchesMelon)
            {
                if (!melon.ArrowHeadOverlapLatch)
                {
                    int damage = UnityEngine.Random.Range(1,30);
                    melon.Hp -= damage;
                    RefreshMelonHpLabel(melon);
                    melon.Entity?.PlayHpLossFloat(damage);
                    melon.ArrowHeadOverlapLatch = true;
                    if (melon.Hp <= 0)
                    {
                        BeginMelonDefeat(melon, listIndex);
                        Log.Info("西瓜被箭头击中，生命耗尽已消除");
                    }
                }
            }
            else
                melon.ArrowHeadOverlapLatch = false;
        }

        /// <summary>
        /// 仅在路径起点（洞口）世界坐标处回收：满血跑完一圈，或生命已耗尽走到洞口（含在洞口被击杀）。
        /// </summary>
        bool TryDespawnAtHole(ActiveMelon melon, int listIndex)
        {
            if (melon.SegmentIndex != 0 || melon.SegmentT > 1e-5f) return false;
            if (melon.Hp <= 0)
            {
                BeginMelonDefeat(melon, listIndex);
                Log.Info("西瓜已在洞口回收（生命耗尽）");
                return true;
            }

            if (!melon.HasLeftHole) return false;
            RemoveMelonAt(listIndex);
            Log.Info("西瓜跑完一圈，已在洞口消失");
            if (m_LevelOwner != null)
                m_LevelOwner.NotifyWatermelonEnteredHoleWithRemainingArrows();
            return true;
        }

        void TickMelon(ActiveMelon melon, float dt, int listIndex)
        {
            if (melon.IsDefeated)
            {
                TickMelonDefeat(melon, dt, listIndex);
                return;
            }

            int n = m_PathWorld.Count;
            float remaining = m_MoveSpeedWorld * dt;
            int guard = 0;
            while (remaining > 1e-6f && guard++ < n * 4)
            {
                int nextIdx = (melon.SegmentIndex + 1) % n;
                Vector3 a = m_PathWorld[melon.SegmentIndex];
                Vector3 b = m_PathWorld[nextIdx];
                float segLen = Vector3.Distance(a, b);
                if (segLen < 1e-6f)
                {
                    melon.SegmentT = 0f;
                    melon.SegmentIndex = nextIdx;
                    if (TryDespawnAtHole(melon, listIndex)) return;
                    continue;
                }

                float distOnSeg = (1f - melon.SegmentT) * segLen;
                if (remaining >= distOnSeg)
                {
                    remaining -= distOnSeg;
                    melon.SegmentT = 0f;
                    melon.SegmentIndex = nextIdx;
                    if (TryDespawnAtHole(melon, listIndex)) return;
                }
                else
                {
                    melon.SegmentT += remaining / segLen;
                    remaining = 0f;
                }
            }

            melon.HasLeftHole = melon.SegmentIndex != 0 || melon.SegmentT > 1e-4f;

            ApplyMelonVisual(melon);
        }

        public void PauseForGameOver()
        {
            m_StreamActive = false;
        }

        /// <summary> 生命值复活后继续生成与移动（需本关环路仍有效）。 </summary>
        public void ResumeAfterGameOver()
        {
            if (m_PathGrid.Count < 2 || m_PathWorld.Count < 2 || m_Grid == null || m_Root == null)
                return;

            m_StreamActive = true;
            ActiveInstance = this;
        }

        public void EndLevel()
        {
            if (ActiveInstance == this)
                ActiveInstance = null;
            m_StreamActive = false;
            m_PathGrid.Clear();
            m_PathWorld.Clear();

            for (int i = m_ActiveMelons.Count - 1; i >= 0; i--)
            {
                int id = m_ActiveMelons[i].EntityId;
                if (id >= 0)
                    GF.Entity.HideEntitySafe(id);
            }

            m_ActiveMelons.Clear();
            m_Grid = null;
            m_LevelOwner = null;

            if (m_HoleEntityId >= 0)
            {
                GF.Entity.HideEntitySafe(m_HoleEntityId);
                m_HoleEntityId = -1;
            }

            if (m_Root != null)
            {
                Destroy(m_Root);
                m_Root = null;
            }
        }

        void RemoveMelonAt(int listIndex)
        {
            var m = m_ActiveMelons[listIndex];
            if (m.EntityId >= 0)
                GF.Entity.HideEntitySafe(m.EntityId);
            m_ActiveMelons.RemoveAt(listIndex);
        }

        void Update()
        {
            if (!m_StreamActive || m_PathGrid.Count < 2 || m_PathWorld.Count < 2 || m_Grid == null) return;

            float dt = Time.deltaTime;

            m_SpawnTimer += dt;
            if (m_SpawnTimer >= m_SpawnInterval)
            {
                m_SpawnTimer = 0f;
                TrySpawnMelon();
            }

            for (int i = m_ActiveMelons.Count - 1; i >= 0; i--)
            {
                var melon = m_ActiveMelons[i];
                if (melon.Transform == null) continue;
                TickMelon(melon, dt, i);
            }
        }

        void LateUpdate()
        {
            if (!m_StreamActive || m_PathGrid.Count < 2 || m_ActiveMelons.Count == 0 || m_Grid == null) return;

            for (int i = m_ActiveMelons.Count - 1; i >= 0; i--)
                ApplyArrowHeadHitsIfOnSameCell(m_ActiveMelons[i], i);
        }

        private void OnDestroy()
        {
            EndLevel();
        }
    }
}
