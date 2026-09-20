using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArrowMaze
{
    /// <summary>
    /// 高级皮肤配置库，按 skinId 查找对应配置。
    /// </summary>
    [CreateAssetMenu(fileName = "ArrowAdvancedSkinDatabase", menuName = "ArrowMaze/Arrow Advanced Skin Database", order = 1)]
    public class ArrowAdvancedSkinDatabase : ScriptableObject
    {
        private static ArrowAdvancedSkinDatabase mInstance = null;
        [SerializeField] private List<ArrowAdvancedSkinConfig> m_Configs = new List<ArrowAdvancedSkinConfig>();
        private void Awake()
        {
            mInstance = this;
        }
        /// <summary>
        /// 根据高级皮肤 ID 获取配置，未找到返回 null。
        /// </summary>
        public ArrowAdvancedSkinConfig GetConfig(int skinId)
        {
            if (m_Configs == null) return null;
            foreach (var c in m_Configs)
            {
                if (c != null && c.skinId == skinId)
                    return c;
            }
            return null;
        }

        public IReadOnlyList<ArrowAdvancedSkinConfig> Configs => m_Configs;

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下获取实例
        /// </summary>
        /// <returns></returns>
        public static ArrowAdvancedSkinDatabase GetInstanceEditor()
        {
            if (mInstance == null)
            {
                var configAsset = UtilityBuiltin.AssetsPath.GetScriptableAsset("SkinConfigs/ArrowAdvancedSkinDatabase");
                mInstance = AssetDatabase.LoadAssetAtPath<ArrowAdvancedSkinDatabase>(configAsset);
            }
            return mInstance;
        }
#endif
        public static async Task<ArrowAdvancedSkinDatabase> GetInstanceSync()
        {
            var configAsset = UtilityBuiltin.AssetsPath.GetScriptableAsset("SkinConfigs/ArrowAdvancedSkinDatabase");
            if (mInstance == null)
            {
                mInstance = await GFBuiltin.Resource.LoadAssetAwait<ArrowAdvancedSkinDatabase>(configAsset);
            }
            return mInstance;
        }
    }
}
