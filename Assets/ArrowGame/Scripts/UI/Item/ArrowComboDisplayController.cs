using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze.UI
{
    /// <summary>
    /// 连消 Combo 展示：在箭头附近对象池弹出放大缩小动画。
    /// </summary>
    public class ArrowComboDisplayController
    {
        const float SpawnOffsetRadius = 24f;

        readonly ArrowMazeUIForm m_Owner;
        readonly GameObject m_ItemPrefab;
        readonly RectTransform m_ParentRect;
        int m_SpawnIndex;

        public ArrowComboDisplayController(ArrowMazeUIForm owner, GameObject itemPrefab, RectTransform parentRect)
        {
            m_Owner = owner;
            m_ItemPrefab = itemPrefab;
            m_ParentRect = parentRect;
        }

        public void Reset()
        {
            m_SpawnIndex = 0;
            if (m_Owner == null || m_ItemPrefab == null)
                return;

            m_Owner.UnspawnAllComboFloatItems();
        }

        public void Show(int comboCount, Vector3 worldPosition)
        {
            if (comboCount <= 0 || m_Owner == null || m_ItemPrefab == null || m_ParentRect == null)
                return;

            if (!TryWorldToLocalPoint(worldPosition, out Vector2 localPoint))
                localPoint = Vector2.zero;

            localPoint += GetSpawnOffset();

            var comboItem = m_Owner.SpawnComboFloatItem();
            if (comboItem == null)
                return;

            comboItem.Play(comboCount, localPoint, () =>
            {
                if (m_Owner != null && comboItem != null)
                    m_Owner.UnspawnComboFloatItem(comboItem);
            });
        }

        Vector2 GetSpawnOffset()
        {
            m_SpawnIndex++;
            float angle = (m_SpawnIndex % 6) * (Mathf.PI * 2f / 6f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SpawnOffsetRadius;
        }

        bool TryWorldToLocalPoint(Vector3 worldPosition, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (m_ParentRect == null)
                return false;

            Camera gameCam = Camera.main;
            Camera uiCam = GFBuiltin.UICamera;
            if (gameCam == null || uiCam == null)
                return false;

            Vector3 screenPoint = gameCam.WorldToScreenPoint(worldPosition);
            if (screenPoint.z < 0f)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_ParentRect,
                screenPoint,
                uiCam,
                out localPoint);
        }
    }
}
