using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace UGF.EditorTools
{
    [EditorToolMenu("替换字体", typeof(BatchOperateToolEditor), 0)]
    public class FontReplaceTool : UtilitySubToolBase
    {
        public override string AssetSelectorTypeFilter => "t:prefab t:folder";
        public override string DragAreaTips => "拖拽添加Prefab文件或文件夹";
        protected override Type[] SupportAssetTypes => new Type[] { typeof(GameObject) };

        UnityEngine.Font textFont;
        TMP_FontAsset tmpFont;
        TMP_SpriteAsset tmpFontSpriteAsset;
        TMP_StyleSheet tmpFontStyleSheet;
        bool copyMaterialProperties = true;
        string newMaterialsFolder = "Assets/ArrowGame/Font/Common/NewFont";

        public FontReplaceTool(BatchOperateToolEditor ownerEditor) : base(ownerEditor)
        {
        }

        public override void DrawBottomButtonsPanel()
        {
            if (GUILayout.Button("一键替换", GUILayout.Height(30)))
            {
                ReplaceFont();
            }
        }

        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                textFont = EditorGUILayout.ObjectField("Text字体替换:", textFont, typeof(UnityEngine.Font), false) as UnityEngine.Font;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginVertical("box");
            {
                tmpFont = EditorGUILayout.ObjectField("TextMeshPro字体替换:", tmpFont, typeof(TMP_FontAsset), false) as TMP_FontAsset;
                tmpFontSpriteAsset = EditorGUILayout.ObjectField("Sprite Asset替换:", tmpFontSpriteAsset, typeof(TMP_SpriteAsset), false) as TMP_SpriteAsset;
                tmpFontStyleSheet = EditorGUILayout.ObjectField("Style Sheet替换:", tmpFontStyleSheet, typeof(TMP_StyleSheet), false) as TMP_StyleSheet;
                
                // 添加材质复制选项
                EditorGUILayout.Space();
                copyMaterialProperties = EditorGUILayout.Toggle("复制材质属性", copyMaterialProperties);
                if (copyMaterialProperties)
                {
                    newMaterialsFolder = EditorGUILayout.TextField("材质保存路径", newMaterialsFolder);
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        private void ReplaceFont()
        {
            var prefabs = OwnerEditor.GetSelectedAssets();
            if (prefabs == null || prefabs.Count < 1) return;

            int taskIdx = 0;
            int totalTaskCount = prefabs.Count;
            bool batTmpfont = tmpFont != null || tmpFontSpriteAsset != null || tmpFontStyleSheet != null;
            
            foreach (var item in prefabs)
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(item);
                if (pfb == null) continue;
                
                EditorUtility.DisplayProgressBar($"进度({taskIdx++}/{totalTaskCount})", item, taskIdx / (float)totalTaskCount);
                bool hasChanged = false;
                
                if (textFont != null)
                {
                    foreach (var textCom in pfb.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    {
                        textCom.font = textFont;
                        hasChanged = true;
                    }
                }
                
                if (batTmpfont)
                {
                    foreach (var tmpTextCom in pfb.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    {
                        if (tmpFont != null)
                        {
                            // 获取旧材质信息
                            Material oldMaterial = GetActualMaterial(tmpTextCom);
                            string oldMaterialName = oldMaterial != null ? oldMaterial.name : string.Empty;
                            
                            // 替换字体
                            tmpTextCom.font = tmpFont;
                            
                            // 处理材质
                            if (oldMaterial != null && copyMaterialProperties)
                            {
                                Material newMaterial = FindOrCreateMatchingMaterial(tmpFont, oldMaterial, oldMaterialName);
                                if (newMaterial != null)
                                {
                                    SetMaterial(tmpTextCom, newMaterial);
                                }
                            }
                        }
                        
                        if (tmpFontSpriteAsset != null) tmpTextCom.spriteAsset = tmpFontSpriteAsset;
                        if (tmpFontStyleSheet != null) tmpTextCom.styleSheet = tmpFontStyleSheet;
                        hasChanged = true;
                    }
                }
                
                if (hasChanged)
                {
                    PrefabUtility.SavePrefabAsset(pfb);
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region Material Handling Methods
        
        private Material GetActualMaterial(TMP_Text tmpComponent)
        {
            // if (tmpComponent.GetComponent<Renderer>() != null)
            // {
            //     return tmpComponent.GetComponent<Renderer>().sharedMaterial;
            // }
            
            // CanvasRenderer canvasRenderer = tmpComponent.GetComponent<CanvasRenderer>();
            // if (canvasRenderer != null)
            // {
            //     return canvasRenderer.GetMaterial();
            // }
            
            return tmpComponent.fontSharedMaterial;
        }

        private void SetMaterial(TMP_Text tmpComponent, Material newMaterial)
        {
            // if (tmpComponent.GetComponent<Renderer>() != null)
            // {
            //     tmpComponent.GetComponent<Renderer>().sharedMaterial = newMaterial;
            // }
            // else
            // {
            //     CanvasRenderer canvasRenderer = tmpComponent.GetComponent<CanvasRenderer>();
            //     if (canvasRenderer != null)
            //     {
            //         canvasRenderer.SetMaterial(newMaterial, 0);
            //     }
            //     else
            //     {
                    tmpComponent.fontSharedMaterial = newMaterial;
            //     }
            // }
        }

        private Material FindOrCreateMatchingMaterial(TMP_FontAsset fontAsset, Material oldMaterial, string oldMaterialName)
        {
            if (string.IsNullOrEmpty(oldMaterialName))
                return fontAsset.material;

            // 1. 尝试查找现有材质
            Material matchedMaterial = FindExistingMaterial(fontAsset, oldMaterialName);
            if (matchedMaterial != null)
            {
                return matchedMaterial;
            }

            // 2. 创建新材质并复制属性
            // return CreateMaterialCopyWithProperties(fontAsset, oldMaterial, oldMaterialName);
            return null;
        }

        private Material FindExistingMaterial(TMP_FontAsset fontAsset, string materialName)
        {
            string baseMaterialName = CleanMaterialName(materialName);
            List<Material> allMaterials = GetAllMaterialVariants(fontAsset);

            return allMaterials.FirstOrDefault(m => 
                CleanMaterialName(m.name) == baseMaterialName);
        }

        private Material CreateMaterialCopyWithProperties(TMP_FontAsset fontAsset, Material sourceMaterial, string materialName)
        {
            // 确保材质目录存在
            if (!Directory.Exists(newMaterialsFolder))
            {
                Directory.CreateDirectory(newMaterialsFolder);
                AssetDatabase.Refresh();
            }

            // 创建新材质实例
            Material newMaterial = new Material(fontAsset.material)
            {
                name = CleanMaterialName(materialName)
            };

            // 复制属性
            CopyMaterialProperties(sourceMaterial, newMaterial);

            // 保存新材质
            string materialPath = $"{newMaterialsFolder}{newMaterial.name}.mat";
            materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);
            
            AssetDatabase.CreateAsset(newMaterial, materialPath);
            Debug.Log($"创建新材质: {materialPath}");

            return newMaterial;
        }

        private void CopyMaterialProperties(Material source, Material destination)
        {
            // 使用SerializedObject进行深度复制
            SerializedObject sourceSerialized = new SerializedObject(source);
            SerializedObject destSerialized = new SerializedObject(destination);

            SerializedProperty srcIterator = sourceSerialized.GetIterator();
            bool enterChildren = true;
            while (srcIterator.Next(enterChildren))
            {
                if (srcIterator.name == "m_Script") continue;

                SerializedProperty destProperty = destSerialized.FindProperty(srcIterator.propertyPath);
                if (destProperty != null && destProperty.propertyType == srcIterator.propertyType)
                {
                    CopySerializedPropertyValue(srcIterator, destProperty);
                }
                enterChildren = false;
            }

            destSerialized.ApplyModifiedProperties();
            destination.shaderKeywords = source.shaderKeywords;
        }

        private void CopySerializedPropertyValue(SerializedProperty source, SerializedProperty dest)
        {
            switch (source.propertyType)
            {
                case SerializedPropertyType.Color:
                    dest.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.Float:
                    dest.floatValue = source.floatValue;
                    break;
                case SerializedPropertyType.Vector4:
                    dest.vector4Value = source.vector4Value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    dest.objectReferenceValue = source.objectReferenceValue;
                    break;
                // 添加其他需要的属性类型
            }
        }

        private string CleanMaterialName(string materialName)
        {
            return materialName
                .Replace(" (Instance)", "")
                .Replace(" Material", "")
                .Replace(".mat", "")
                .Trim();
        }

        private List<Material> GetAllMaterialVariants(TMP_FontAsset fontAsset)
        {
            List<Material> materials = new List<Material>();

            // 添加主材质
            if (fontAsset.material != null)
            {
                materials.Add(fontAsset.material);
            }

            // 查找使用相同纹理的材质
            string[] allMaterialGUIDs = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in allMaterialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.HasProperty("_MainTex") && 
                    mat.GetTexture("_MainTex") == fontAsset.atlasTexture)
                {
                    materials.Add(mat);
                }
            }

            return materials.Distinct().ToList();
        }
        
        #endregion
    }
}