using UnityEngine;

namespace ArrowMaze
{
    /// <summary> 地图手指引导实体，由 LevelManager 控制显示与位置。 </summary>
    public class FingerGuideEntity : EntityBase
    {
        const int FingerSortingOrder = 100;

        MeshRenderer m_MeshRenderer;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            ApplyRenderOrder();
        }

        void LateUpdate()
        {
            if (!gameObject.activeInHierarchy) return;
            ApplyRenderOrder();
        }

        void ApplyRenderOrder()
        {
            if (m_MeshRenderer == null)
                m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshRenderer == null) return;

            m_MeshRenderer.sortingOrder = FingerSortingOrder;
        }
    }
}
