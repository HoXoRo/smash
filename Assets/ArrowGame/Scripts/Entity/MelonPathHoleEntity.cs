using UnityEngine;

namespace ArrowMaze
{
    /// <summary> 西瓜环路洞口（Entity 系统，参数为 <see cref="MelonPathHoleEntityParams"/>） </summary>
    public class MelonPathHoleEntity : EntityBase
    {
        Transform m_AttachRestoreParent;
        bool m_HasAttachRestoreParent;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            m_HasAttachRestoreParent = false;
            m_AttachRestoreParent = null;
            if (Params is MelonPathHoleEntityParams holeParams && holeParams.AttachParent != null &&
                CachedTransform != null)
            {
                m_AttachRestoreParent = CachedTransform.parent;
                m_HasAttachRestoreParent = true;
                CachedTransform.SetParent(holeParams.AttachParent, true);
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            if (m_HasAttachRestoreParent && CachedTransform != null && m_AttachRestoreParent != null)
            {
                CachedTransform.SetParent(m_AttachRestoreParent, false);
                m_HasAttachRestoreParent = false;
                m_AttachRestoreParent = null;
            }

            base.OnHide(isShutdown, userData);
        }
    }
}
