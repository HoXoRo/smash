using System;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace ArrowMaze
{
    /// <summary>
    /// 箭头线实体参数
    /// </summary>
    public class ArrowLineEntityParams : EntityParams
    {
        public ArrowLineDef ArrowDef { get; set; }
        public List<Dot> DotPath { get; set; }
        public Action<ArrowLineEntity> OnExited { get; set; }
        public Action<ArrowLineEntity> OnHit { get; set; }
        /// <summary> 高级皮肤配置，非 null 时实体将应用该配置（body 粗细、头部精灵、材质、速度、头尾同速等）。 </summary>
        public ArrowAdvancedSkinConfig AdvancedSkinConfig { get; set; }

        public static ArrowLineEntityParams Create(ArrowLineDef arrowDef, List<Dot> dotPath, 
            Action<ArrowLineEntity> onExited, Action<ArrowLineEntity> onHit, Vector3? position = null)
        {
            var eParams = ReferencePool.Acquire<ArrowLineEntityParams>();
            eParams.CreateRoot();
            eParams.ArrowDef = arrowDef;
            eParams.DotPath = dotPath;
            eParams.OnExited = onExited;
            eParams.OnHit = onHit;
            eParams.position = position;
            eParams.gameObjectLayer = LayerMask.NameToLayer("WorldUI");
            return eParams;
        }

        protected override void ResetProperties()
        {
            base.ResetProperties();
            ArrowDef = null;
            DotPath = null;
            OnExited = null;
            OnHit = null;
            AdvancedSkinConfig = null;
        }
    }
}
