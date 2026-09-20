using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// 网格管理器 - 管理游戏网格和Dot节点
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        private GridDef m_GridDef;
        private Dot[,] m_Dots; // 二维数组存储所有Dot节点
        private Dictionary<Vector2Int, Dot> m_DotByGrid; // 快速查找字典
        
        [SerializeField]
        private GameObject m_DotPrefab; // Dot预制体（如果使用预制体）
        
        [SerializeField]
        private Transform m_DotsRoot; // Dot节点的父对象

        /// <summary>
        /// 网格定义
        /// </summary>
        public GridDef GridDef => m_GridDef;

        /// <summary>
        /// 根据网格坐标获取Dot节点
        /// </summary>
        public Dot GetDotByGrid(Vector2Int gridPos)
        {
            if (m_DotByGrid != null && m_DotByGrid.TryGetValue(gridPos, out Dot dot))
            {
                return dot;
            }
            return null;
        }

        /// <summary>
        /// 获取从指定起始点沿某方向上的所有有效格点（按距离排序，近的在前）。
        /// 用于不规则几何形状时，同行/同列上有效格可能不连续，需遍历整行/列上所有有效格。
        /// </summary>
        /// <param name="from">起始网格坐标</param>
        /// <param name="step">方向步进 (1,0)/(-1,0)/(0,1)/(0,-1)</param>
        /// <returns>该方向上所有有效 Dot，按距离 from 从近到远排序</returns>
        public List<Dot> GetDotsInDirection(Vector2Int from, Vector2Int step)
        {
            var result = new List<Dot>();
            if (m_DotByGrid == null || m_DotByGrid.Count == 0) return result;

            foreach (var kv in m_DotByGrid)
            {
                Vector2Int g = kv.Key;
                if (step.x != 0)
                {
                    if (g.y != from.y) continue;
                    if (step.x > 0 && g.x <= from.x) continue;
                    if (step.x < 0 && g.x >= from.x) continue;
                }
                else if (step.y != 0)
                {
                    if (g.x != from.x) continue;
                    if (step.y > 0 && g.y <= from.y) continue;
                    if (step.y < 0 && g.y >= from.y) continue;
                }
                else
                {
                    continue;
                }
                result.Add(kv.Value);
            }

            result.Sort((a, b) =>
            {
                int da = Mathf.Abs(a.G.x - from.x) + Mathf.Abs(a.G.y - from.y);
                int db = Mathf.Abs(b.G.x - from.x) + Mathf.Abs(b.G.y - from.y);
                return da.CompareTo(db);
            });
            return result;
        }

        /// <summary>
        /// 网格坐标转世界坐标
        /// 注意：Unity的Y轴向上，如果编辑器使用Y轴向下，需要翻转Y坐标
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            if (m_GridDef == null) return Vector3.zero;
            
            // 计算相对于网格中心的位置
            // 网格中心在 (width-1)/2, (height-1)/2
            float centerX = (m_GridDef.width - 1) * 0.5f * m_GridDef.spacing;
            float centerY = (m_GridDef.height - 1) * 0.5f * m_GridDef.spacing;
            
            // 计算相对于中心的位置（翻转Y轴，因为Unity Y轴向上，编辑器可能Y轴向下）
            float offsetX = (gridPos.x - (m_GridDef.width - 1) * 0.5f) * m_GridDef.spacing;
            float offsetY = ((m_GridDef.height - 1) * 0.5f - gridPos.y) * m_GridDef.spacing; // 翻转Y轴
            
            return m_GridDef.origin + new Vector3(offsetX, offsetY, 0);
        }

        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (m_GridDef == null) return Vector2Int.zero;
            
            Vector3 localPos = worldPos - m_GridDef.origin;
            return new Vector2Int(
                Mathf.RoundToInt(localPos.x / m_GridDef.spacing),
                Mathf.RoundToInt(localPos.y / m_GridDef.spacing)
            );
        }

        /// <summary>
        /// 重建网格（根据关卡数据）
        /// </summary>
        public void RebuildGrid(ArrowLevelData levelData)
        {
            if (levelData == null || levelData.grid == null)
            {
                Log.Error("关卡数据无效，无法重建网格");
                return;
            }

            m_GridDef = levelData.grid;
            
            // 清理旧网格
            Cleanup();

            // 创建Dots根对象，使用关卡名称命名
            if (m_DotsRoot == null)
            {
                string dotsRootName = string.IsNullOrEmpty(levelData.levelName) ? "Dots" : $"Dots_{levelData.levelName}";
                m_DotsRoot = new GameObject(dotsRootName).transform;
                m_DotsRoot.SetParent(transform);
            }

            // 初始化数组和字典
            m_Dots = new Dot[m_GridDef.width, m_GridDef.height];
            m_DotByGrid = new Dictionary<Vector2Int, Dot>();

            var validSet = levelData.validGrids ?? new HashSet<Vector2Int>();

            // 收集被箭头占用的有效格（所有箭头路径上的点）
            var occupiedByArrows = new HashSet<Vector2Int>();
            if (levelData.arrows != null)
            {
                foreach (var arrow in levelData.arrows)
                {
                    var path = arrow.path != null && arrow.path.Count > 0
                        ? arrow.path
                        : (arrow.serializedPath?.ConvertAll(sp => sp.ToVector2Int()) ?? new List<Vector2Int>());
                    foreach (var p in path)
                    {
                        if (validSet.Contains(p))
                            occupiedByArrows.Add(p);
                    }
                }
            }

            // 只为有效网格创建 Dot；未被箭头使用的有效格设为隐藏
            foreach (var gridPos in validSet)
            {
                if (gridPos.x >= 0 && gridPos.x < m_GridDef.width &&
                    gridPos.y >= 0 && gridPos.y < m_GridDef.height)
                {
                    CreateDot(gridPos);
                    bool isOccupiedByArrow = occupiedByArrows.Contains(gridPos);
                    m_Dots[gridPos.x, gridPos.y].gameObject.SetActive(isOccupiedByArrow);
                }
            }

            Log.Info($"网格重建完成 - 关卡: {levelData.levelName}, 有效网格数: {validSet.Count}, 箭头占用: {occupiedByArrows.Count}");
        }

        /// <summary>
        /// 创建Dot节点
        /// </summary>
        private void CreateDot(Vector2Int gridPos)
        {
            Vector3 worldPos = GridToWorld(gridPos);
            
            GameObject dotObj;
            if (m_DotPrefab != null)
            {
                dotObj = Instantiate(m_DotPrefab, worldPos, Quaternion.identity, m_DotsRoot);
                // 重命名实例化的预制体
                dotObj.name = $"Dot_{gridPos.x}_{gridPos.y}";
            }
            else
            {
                // 如果没有预制体，创建一个简单的GameObject
                dotObj = new GameObject($"Dot_{gridPos.x}_{gridPos.y}");
                dotObj.transform.SetParent(m_DotsRoot);
                dotObj.transform.position = worldPos;
                
                // 添加Dot组件
                Dot dot = dotObj.AddComponent<Dot>();
                dot.Initialize(gridPos, this);
            }

            Dot dotComponent = dotObj.GetComponent<Dot>();
            if (dotComponent == null)
            {
                dotComponent = dotObj.AddComponent<Dot>();
            }
            
            dotComponent.Initialize(gridPos, this);

            // 存储到数组和字典
            m_Dots[gridPos.x, gridPos.y] = dotComponent;
            m_DotByGrid[gridPos] = dotComponent;
        }

        /// <summary>
        /// 获取网格在世界空间中的 AABB 边界（用于相机定位）。
        /// GridToWorld 对 Y 做了翻转，故两个对角格 (0,0) 与 (w-1,h-1) 的世界坐标取各自分量 min/max 得到真实世界 min/max，保证 size 各分量为正。
        /// </summary>
        public Bounds GetGridBounds()
        {
            if (m_GridDef == null) return new Bounds(Vector3.zero, Vector3.zero);
            
            Vector3 p00 = GridToWorld(Vector2Int.zero);
            Vector3 pWh = GridToWorld(new Vector2Int(m_GridDef.width - 1, m_GridDef.height - 1));
            Vector3 worldMin = new Vector3(
                Mathf.Min(p00.x, pWh.x),
                Mathf.Min(p00.y, pWh.y),
                Mathf.Min(p00.z, pWh.z)
            );
            Vector3 worldMax = new Vector3(
                Mathf.Max(p00.x, pWh.x),
                Mathf.Max(p00.y, pWh.y),
                Mathf.Max(p00.z, pWh.z)
            );
            Vector3 center = (worldMin + worldMax) * 0.5f;
            Vector3 size = worldMax - worldMin;
            
            return new Bounds(center, size);
        }

        /// <summary>
        /// 获取网格中心位置
        /// </summary>
        public Vector3 GetGridCenter()
        {
            if (m_GridDef == null) return Vector3.zero;
            
            Vector2Int centerGrid = new Vector2Int(
                (m_GridDef.width - 1) / 2,
                (m_GridDef.height - 1) / 2
            );
            
            return GridToWorld(centerGrid);
        }

        /// <summary>
        /// 清理网格
        /// </summary>
        public void Cleanup()
        {
            if (m_DotsRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_DotsRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(m_DotsRoot.gameObject);
                }
                m_DotsRoot = null;
            }

            m_Dots = null;
            m_DotByGrid?.Clear();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
