using System;
using GameFramework;
using UnityEngine;

namespace ArrowMaze
{
    /// <summary>
    /// 洞口实体参数：创建方式与 <see cref="ArrowLineEntityParams.Create"/> 相同（ReferencePool + CreateRoot）。
    /// </summary>
    public class MelonPathHoleEntityParams : EntityParams
    {
        /// <summary> 显示后挂到该节点下（通常为 WatermelonPathExtension 根节点） </summary>
        public Transform AttachParent { get; set; }

        public static MelonPathHoleEntityParams Create(Vector3 position, Vector3 localScale, Transform attachParent)
        {
            var p = ReferencePool.Acquire<MelonPathHoleEntityParams>();
            p.CreateRoot();
            p.position = position;
            p.localScale = localScale;
            p.AttachParent = attachParent;
            p.gameObjectLayer = LayerMask.NameToLayer("WorldUI");
            return p;
        }

        protected override void ResetProperties()
        {
            base.ResetProperties();
            AttachParent = null;
        }
    }

    /// <summary>
    /// 西瓜实体参数：创建方式与 <see cref="ArrowLineEntityParams.Create"/> 相同。
    /// </summary>
    public class MelonPathWatermelonEntityParams : EntityParams
    {
        public Transform AttachParent { get; set; }
        public Action<MelonPathWatermelonEntity> OnWatermelonShown { get; set; }

        public static MelonPathWatermelonEntityParams Create(Vector3 position, Vector3 localScale, Transform attachParent)
        {
            var p = ReferencePool.Acquire<MelonPathWatermelonEntityParams>();
            p.CreateRoot();
            p.position = position;
            p.localScale = localScale;
            p.AttachParent = attachParent;
            p.gameObjectLayer = LayerMask.NameToLayer("WorldUI");
            return p;
        }

        protected override void ResetProperties()
        {
            base.ResetProperties();
            AttachParent = null;
            OnWatermelonShown = null;
        }
    }
}
