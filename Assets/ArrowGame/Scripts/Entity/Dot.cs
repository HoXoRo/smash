using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// Dot节点 - 网格上的一个点
    /// </summary>
    public class Dot : MonoBehaviour
    {
        /// <summary>
        /// 网格坐标
        /// </summary>
        public Vector2Int G { get; private set; }

        /// <summary>
        /// 占用此节点的箭头线（null表示空闲）
        /// </summary>
        public ArrowLineEntity Occupant { get; set; }

        /// <summary>
        /// 是否空闲
        /// </summary>
        public bool IsFree => Occupant == null;

        private GridManager m_GridManager;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(Vector2Int gridPos, GridManager gridManager)
        {
            G = gridPos;
            m_GridManager = gridManager;
            Occupant = null;
            
            // 可以在这里添加视觉显示（可选）
            // ShowDot();
        }

        /// <summary>
        /// 显示Dot（可选，用于调试）
        /// </summary>
        public void ShowDot()
        {
            // 可以添加SpriteRenderer或其他视觉组件
            if (GetComponent<SpriteRenderer>() == null)
            {
                SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
                // 可以设置一个小的点精灵
            }
        }

        /// <summary>
        /// 隐藏Dot（可选）
        /// </summary>
        public void HideDot()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }
        }

        private void OnDrawGizmos()
        {
            // 在Scene视图中显示Dot位置（调试用）
            Gizmos.color = IsFree ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
