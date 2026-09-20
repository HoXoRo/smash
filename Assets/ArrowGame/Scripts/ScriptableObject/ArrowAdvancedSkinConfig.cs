using UnityEngine;

namespace ArrowMaze
{
    /// <summary>
    /// 箭头高级皮肤配置（ScriptableObject），支持配置 body 粗细、头部精灵、body 材质、移动速度等。
    /// 使用高级皮肤时 HeadSpeedRatio == TailSpeedRatio（由 SpeedRatio 指定），头尾同速移动。
    /// </summary>
    [CreateAssetMenu(fileName = "ArrowAdvancedSkin", menuName = "ArrowMaze/Arrow Advanced Skin Config", order = 0)]
    public class ArrowAdvancedSkinConfig : ScriptableObject
    {
        [Tooltip("高级皮肤唯一 ID，与玩家持久化中的 ArrowAdvancedSkinId 对应。0 表示默认皮肤。")]
        public int skinId = 1;

        [Tooltip("Body 线条粗细（世界单位）")]
        [Min(0.05f)]
        public float bodyWidth = 0.2f;

        [Tooltip("箭头头部精灵，为空则使用预制体原有")]
        public Sprite headSprite;

        [Tooltip("头部精灵缩放比例，默认皮肤为 2，高级皮肤可在此配置")]
        [Min(0.1f)]
        public float headSpriteScale = 2f;

        [Tooltip("Body 使用的材质球，为空则使用预制体原有")]
        public Material bodyMaterial;

        [Tooltip("移动速度（世界单位/秒）")]
        [Min(0.1f)]
        public float moveSpeed = 20f;

        [Tooltip("高级皮肤下头尾同速，此值同时作为 HeadSpeedRatio 与 TailSpeedRatio。1 表示头尾完全同速。")]
        [Range(0.5f, 2f)]
        public float speedRatio = 1f;

        [Tooltip("是否使用重复纹理 body（按长度动态 tiling）")]
        public bool useRepeatingTextureBody = false;

        [Tooltip("重复纹理时，每段纹理占用的世界长度")]
        [Min(0.01f)]
        public float textureRepeatWorldLength = 0.25f;
    }
}
