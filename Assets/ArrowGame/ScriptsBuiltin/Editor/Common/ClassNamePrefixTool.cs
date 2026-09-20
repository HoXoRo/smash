#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace UGF.EditorTools
{
    /// <summary>
    /// 类名和文件名前缀添加工具
    /// 用于批量给UI界面、配置文件等添加项目前缀，减少项目间内容重复度
    /// </summary>
    public class ClassNamePrefixTool : EditorWindow
    {
        private string m_Prefix = "Arrow";
        private string m_OldPrefix = ""; // 旧前缀，为空时表示添加前缀模式，不为空时表示替换前缀模式
        private string m_UIScriptsPath = ConstEditor.UIScriptsPath;
        private string m_DataTablePath = ConstEditor.DataTableCodePath;
        private string m_UIVariablesPath = ConstEditor.UISerializeFieldDir;
        private string m_UIItemVariablesPath = ConstEditor.UIItemSerializeFiledDir;
        private string m_UIViewsPath = ConstEditor.UIViewScriptFile;
        private string m_UIPrefabsPath = "Assets/{0}Game/Prefabs/UI";
        private string m_UIItemPrefabsPath = "Assets/{0}Game/Prefabs/UI/Items";
        
        private Vector2 m_ScrollPosition;
        private Vector2 m_ExcludeScrollPosition;
        private List<string> m_LogMessages = new List<string>();
        private bool m_ShowPreview = true;
        private List<FileRenameInfo> m_PreviewResults = new List<FileRenameInfo>();
        private List<PrefabRenameInfo> m_PrefabRenameResults = new List<PrefabRenameInfo>();
        
        // 排除路径列表
        private List<string> m_ExcludePaths = new List<string>();
        private string m_NewExcludePath = "";
        private bool m_ShowExcludePaths = false;

        [MenuItem("Tools/类名前缀工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<ClassNamePrefixTool>("类名前缀工具");
            window.m_UIPrefabsPath = window.m_UIPrefabsPath.Replace("{0}", window.m_Prefix);
            window.m_UIItemPrefabsPath = window.m_UIItemPrefabsPath.Replace("{0}", window.m_Prefix);
            window.minSize = new Vector2(600, 500);
            window.LoadExcludePaths();
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadExcludePaths();
        }
        
        private void OnDisable()
        {
            SaveExcludePaths();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            EditorGUILayout.LabelField("类名和文件名前缀工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("此工具支持两种模式：\n" +
                                   "1. 添加前缀：旧前缀为空时，给没有前缀的类添加新前缀\n" +
                                   "2. 替换前缀：旧前缀不为空时，将旧前缀替换为新前缀\n" +
                                   "会自动处理类名、文件名、AddComponentMenu特性、UIViews枚举等。", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 输入旧前缀
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("旧前缀:", GUILayout.Width(80));
            m_OldPrefix = EditorGUILayout.TextField(m_OldPrefix);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("留空表示添加前缀模式，填写表示替换前缀模式", MessageType.None);
            
            EditorGUILayout.Space(5);
            
            // 输入新前缀
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("新前缀:", GUILayout.Width(80));
            m_Prefix = EditorGUILayout.TextField(m_Prefix);
            EditorGUILayout.EndHorizontal();
            
            if (string.IsNullOrEmpty(m_Prefix))
            {
                EditorGUILayout.HelpBox("请输入新前缀！", MessageType.Warning);
                return;
            }
            
            // 显示当前模式
            if (string.IsNullOrEmpty(m_OldPrefix))
            {
                EditorGUILayout.HelpBox($"当前模式：添加前缀模式 - 将为没有前缀的类添加 \"{m_Prefix}\" 前缀", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"当前模式：替换前缀模式 - 将 \"{m_OldPrefix}\" 替换为 \"{m_Prefix}\"", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // 路径显示（只读）
            EditorGUILayout.LabelField("扫描路径:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("UI脚本目录:", m_UIScriptsPath);
            EditorGUILayout.TextField("配置文件目录:", m_DataTablePath);
            EditorGUILayout.TextField("UI变量目录:", m_UIVariablesPath);
            EditorGUILayout.TextField("UIViews文件:", m_UIViewsPath);
            EditorGUILayout.TextField("UI预制体目录:", m_UIPrefabsPath);
            EditorGUILayout.TextField("UI Item预制体目录:", m_UIItemPrefabsPath);
            EditorGUILayout.TextField("UI Item脚本目录:", ConstEditor.UIItemScriptsPath);
            EditorGUILayout.TextField("UI Item变量脚本目录:", m_UIItemVariablesPath);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // 排除路径配置
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_ShowExcludePaths = EditorGUILayout.Foldout(m_ShowExcludePaths, "排除文件路径配置", true);
            if (m_ShowExcludePaths)
            {
                EditorGUILayout.HelpBox("在此添加需要排除的文件路径或文件名（支持部分匹配）。\n" +
                                       "例如：\"UIFormBase\" 会排除所有包含此字符串的文件。", MessageType.Info);
                
                EditorGUILayout.Space(5);
                
                // 添加新排除路径
                EditorGUILayout.BeginHorizontal();
                m_NewExcludePath = EditorGUILayout.TextField("新增排除路径:", m_NewExcludePath);
                if (GUILayout.Button("添加", GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(m_NewExcludePath) && !m_ExcludePaths.Contains(m_NewExcludePath))
                    {
                        m_ExcludePaths.Add(m_NewExcludePath);
                        m_NewExcludePath = "";
                        SaveExcludePaths();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // 显示排除路径列表
                if (m_ExcludePaths.Count > 0)
                {
                    EditorGUILayout.LabelField($"已排除路径 ({m_ExcludePaths.Count} 个):", EditorStyles.boldLabel);
                    m_ExcludeScrollPosition = EditorGUILayout.BeginScrollView(m_ExcludeScrollPosition, GUILayout.Height(100));
                    
                    for (int i = m_ExcludePaths.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  • {m_ExcludePaths[i]}", EditorStyles.wordWrappedMiniLabel);
                        if (GUILayout.Button("删除", GUILayout.Width(50)))
                        {
                            m_ExcludePaths.RemoveAt(i);
                            SaveExcludePaths();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndScrollView();
                    
                    if (GUILayout.Button("清空所有排除路径", GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog("确认", "确定要清空所有排除路径吗？", "确定", "取消"))
                        {
                            m_ExcludePaths.Clear();
                            SaveExcludePaths();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("暂无排除路径", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.Space(5);
                
                // 默认排除项说明
                EditorGUILayout.LabelField("默认排除项（自动应用）:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("  • UIViews.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • UIFormBase.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • UIItemBase.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • UIItemObject.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • UIParams.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • SoundGroupTable.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • UIGroupTable.cs", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  • 已有前缀的文件（如 PaopaoXXX.cs）", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 预览选项
            m_ShowPreview = EditorGUILayout.Toggle("显示预览", m_ShowPreview);
            
            EditorGUILayout.Space(10);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("预览更改", GUILayout.Height(30)))
            {
                PreviewChanges();
            }
            
            int totalFiles = m_PreviewResults.Count + m_PrefabRenameResults.Count;
            EditorGUI.BeginDisabledGroup(totalFiles == 0);
            if (GUILayout.Button("执行重命名", GUILayout.Height(30)))
            {
                string modeDescription = string.IsNullOrEmpty(m_OldPrefix) 
                    ? $"添加前缀 \"{m_Prefix}\""
                    : $"将前缀 \"{m_OldPrefix}\" 替换为 \"{m_Prefix}\"";
                
                string fileDescription = $"{m_PreviewResults.Count} 个脚本文件";
                if (m_PrefabRenameResults.Count > 0)
                {
                    fileDescription += $" 和 {m_PrefabRenameResults.Count} 个预制体文件";
                }
                
                if (EditorUtility.DisplayDialog("确认", 
                    $"确定要为 {fileDescription}{modeDescription}吗？\n\n" +
                    "此操作会修改类名、文件名和预制体名，建议先备份项目！", 
                    "确定", "取消"))
                {
                    ExecuteRename();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 预览结果
            int totalPreviewFiles = m_PreviewResults.Count + m_PrefabRenameResults.Count;
            if (m_ShowPreview && totalPreviewFiles > 0)
            {
                EditorGUILayout.LabelField($"预览结果 ({totalPreviewFiles} 个文件):", EditorStyles.boldLabel);
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(200));
                
                // 显示脚本文件
                if (m_PreviewResults.Count > 0)
                {
                    EditorGUILayout.LabelField($"脚本文件 ({m_PreviewResults.Count} 个):", EditorStyles.boldLabel);
                    foreach (var result in m_PreviewResults)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"文件: {result.OldFileName} → {result.NewFileName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"类名: {result.OldClassName} → {result.NewClassName}", EditorStyles.miniLabel);
                        if (result.NeedsAddComponentMenu)
                        {
                            if (!string.IsNullOrEmpty(result.AddComponentMenuValue))
                            {
                                EditorGUILayout.LabelField($"AddComponentMenu: {result.AddComponentMenuValue} (保留原始值)", EditorStyles.miniLabel);
                            }
                            else
                            {
                                var menuValue = result.IsUIScript ? $"UI/{result.OldClassName}" : $"Items/{result.OldClassName}";
                                EditorGUILayout.LabelField($"AddComponentMenu: {menuValue} (将添加)", EditorStyles.miniLabel);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"AddComponentMenu: 不需要 (配置类)", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                
                // 显示预制体文件
                if (m_PrefabRenameResults.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"预制体文件 ({m_PrefabRenameResults.Count} 个):", EditorStyles.boldLabel);
                    foreach (var prefab in m_PrefabRenameResults)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"预制体: {prefab.OldPrefabName} → {prefab.NewPrefabName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"路径: {prefab.PrefabPath}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(10);
            
            // 日志
            if (m_LogMessages.Count > 0)
            {
                EditorGUILayout.LabelField("操作日志:", EditorStyles.boldLabel);
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(150));
                foreach (var log in m_LogMessages)
                {
                    EditorGUILayout.LabelField(log, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndScrollView();
                
                if (GUILayout.Button("清空日志"))
                {
                    m_LogMessages.Clear();
                }
            }
        }

        /// <summary>
        /// 预览更改
        /// </summary>
        private void PreviewChanges()
        {
            m_PreviewResults.Clear();
            m_LogMessages.Clear();
            
            string modeDescription = string.IsNullOrEmpty(m_OldPrefix) 
                ? $"添加前缀模式 - 将为没有前缀的类添加 \"{m_Prefix}\" 前缀"
                : $"替换前缀模式 - 将 \"{m_OldPrefix}\" 前缀替换为 \"{m_Prefix}\" 前缀";
            
            AddLog($"开始预览... ({modeDescription})");
            
            // 扫描UI脚本
            ScanDirectory(m_UIScriptsPath, true);
            
            // 扫描配置文件
            ScanDirectory(m_DataTablePath, false);
            
            // 扫描预制体文件
            ScanPrefabs();
            
            AddLog($"预览完成，共找到 {m_PreviewResults.Count} 个脚本文件和 {m_PrefabRenameResults.Count} 个预制体文件需要重命名");
        }

        /// <summary>
        /// 扫描目录
        /// </summary>
        private void ScanDirectory(string directory, bool isUIScript)
        {
            if (!Directory.Exists(directory))
            {
                AddLog($"警告: 目录不存在 - {directory}");
                return;
            }

            // 默认排除项
            var defaultExcludes = new[]
            {
                "UIViews.cs",
                "UIFormBase.cs",
                "UIItemBase.cs",
                "UIItemObject.cs",
                "UIParams.cs"
            };
            
            var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(f => 
                {
                    var fileName = Path.GetFileName(f);
                    var filePath = f.Replace('\\', '/');
                    
                    // 排除默认排除项
                    foreach (var exclude in defaultExcludes)
                    {
                        if (f.Contains(exclude))
                            return false;
                    }
                    
                    // 排除用户自定义排除路径
                    foreach (var excludePath in m_ExcludePaths)
                    {
                        if (!string.IsNullOrEmpty(excludePath))
                        {
                            // 支持文件名匹配和路径匹配
                            if (fileName.Contains(excludePath) || filePath.Contains(excludePath))
                                return false;
                        }
                    }
                    
                    return true;
                })
                .ToList();

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // 跳过.Variables.cs文件（会在主文件中处理）
                // if (fileName.EndsWith(".Variables"))
                    // continue;
                
                // 根据模式判断是否需要处理
                bool shouldProcess = false;
                if (string.IsNullOrEmpty(m_OldPrefix))
                {
                    // 添加前缀模式：只处理没有前缀的文件
                    if (fileName.StartsWith(m_Prefix))
                        continue; // 已有新前缀，跳过
                    shouldProcess = true;
                }
                else
                {
                    // 替换前缀模式：只处理有旧前缀的文件
                    if (fileName.StartsWith(m_Prefix))
                        continue; // 已有新前缀，跳过
                    if (!fileName.StartsWith(m_OldPrefix))
                        continue; // 没有旧前缀，跳过
                    shouldProcess = true;
                }
                
                if (!shouldProcess)
                    continue;
                
                // 解析文件内容
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                var classInfo = ParseClassInfo(content, fileName);
                
                if (classInfo != null)
                {
                    // 根据模式处理类名
                    string newClassName;
                    string newFileName;
                    
                    if (string.IsNullOrEmpty(m_OldPrefix))
                    {
                        // 添加前缀模式
                        if (classInfo.ClassName.StartsWith(m_Prefix))
                            continue; // 已有新前缀，跳过
                        
                        newClassName = m_Prefix + classInfo.ClassName;
                        newFileName = m_Prefix + fileName + ".cs";
                    }
                    else
                    {
                        // 替换前缀模式
                        if (!classInfo.ClassName.StartsWith(m_OldPrefix))
                            continue; // 没有旧前缀，跳过
                        if (classInfo.ClassName.StartsWith(m_Prefix))
                            continue; // 已有新前缀，跳过
                        
                        // 移除旧前缀，添加新前缀
                        var classNameWithoutPrefix = classInfo.ClassName.Substring(m_OldPrefix.Length);
                        newClassName = m_Prefix + classNameWithoutPrefix;
                        
                        var fileNameWithoutPrefix = fileName.Substring(m_OldPrefix.Length);
                        newFileName = m_Prefix + fileNameWithoutPrefix + ".cs";
                    }
                    
                    var renameInfo = new FileRenameInfo
                    {
                        FilePath = filePath,
                        OldFileName = Path.GetFileName(filePath),
                        OldClassName = classInfo.ClassName,
                        NewClassName = newClassName,
                        NewFileName = newFileName,
                        AddComponentMenuValue = classInfo.AddComponentMenuValue,
                        NeedsAddComponentMenu = classInfo.NeedsAddComponentMenu,
                        IsUIScript = isUIScript
                    };
                    
                    m_PreviewResults.Add(renameInfo);
                }
            }
            
            // 单独处理UIVariables目录中的.Variables.cs文件
            if (isUIScript && Directory.Exists(m_UIVariablesPath))
            {
                var variablesFiles = Directory.GetFiles(m_UIVariablesPath, "*.Variables.cs", SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).StartsWith(m_Prefix))
                    .ToList();
                
                foreach (var varFilePath in variablesFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(varFilePath)); // 去掉.Variables.cs
                    
                    // 检查对应的主文件是否在重命名列表中
                    var mainFileInfo = m_PreviewResults.FirstOrDefault(r => r.OldClassName == fileName);
                    if (mainFileInfo != null)
                    {
                        // 这个文件会在RenameFile中处理，这里不需要单独添加
                    }
                }
            }
        }

        /// <summary>
        /// 解析类信息
        /// </summary>
        private ClassInfo ParseClassInfo(string content, string fileName)
        {
            var classInfo = new ClassInfo();
            
            // 匹配类定义：public (partial )?class ClassName : BaseClass
            var classMatch = Regex.Match(content, @"public\s+(partial\s+)?class\s+(\w+)(\s*:\s*([\w\.]+))?");
            if (!classMatch.Success)
                return null;
            
            classInfo.ClassName = classMatch.Groups[2].Value;
            var baseClass = classMatch.Groups.Count > 4 && !string.IsNullOrEmpty(classMatch.Groups[4].Value) 
                ? classMatch.Groups[4].Value 
                : null;
            
            // 检查是否需要 AddComponentMenu 特性
            // 只有继承 UIFormBase 或 UIItemBase 的类才需要（这些是 MonoBehaviour 类型，需要挂载到预制体）
            bool needsAddComponentMenu = false;
            if (!string.IsNullOrEmpty(baseClass))
            {
                // 检查是否继承 UIFormBase 或 UIItemBase
                if (baseClass.Contains("UIFormBase") || baseClass.Contains("UIItemBase"))
                {
                    needsAddComponentMenu = true;
                }
            }
            
            classInfo.NeedsAddComponentMenu = needsAddComponentMenu;
            
            // 匹配AddComponentMenu特性
            var menuMatch = Regex.Match(content, @"\[AddComponentMenu\s*\(\s*""([^""]+)""\s*\)\]");
            if (menuMatch.Success)
            {
                classInfo.AddComponentMenuValue = menuMatch.Groups[1].Value;
            }
            else if (needsAddComponentMenu)
            {
                // 如果需要但没有AddComponentMenu，根据类名生成一个
                // 例如：GameUIForm -> UI/GameUIForm
                if (classInfo.ClassName.EndsWith("UIForm") || classInfo.ClassName.EndsWith("Dialog") || 
                    classInfo.ClassName.EndsWith("Tips"))
                {
                    classInfo.AddComponentMenuValue = $"UI/{classInfo.ClassName}";
                }
                else if (classInfo.ClassName.EndsWith("Item"))
                {
                    classInfo.AddComponentMenuValue = $"Items/{classInfo.ClassName}";
                }
                else
                {
                    // 默认使用 UI/ 前缀
                    classInfo.AddComponentMenuValue = $"UI/{classInfo.ClassName}";
                }
            }
            
            return classInfo;
        }

        /// <summary>
        /// 扫描预制体文件
        /// </summary>
        private void ScanPrefabs()
        {
            m_PrefabRenameResults.Clear();
            
            // 扫描UI预制体目录
            ScanPrefabDirectory(m_UIPrefabsPath);
            
            // 扫描UI Item预制体目录
            ScanPrefabDirectory(m_UIItemPrefabsPath);
        }

        /// <summary>
        /// 扫描预制体目录
        /// </summary>
        private void ScanPrefabDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                AddLog($"警告: 预制体目录不存在 - {directory}");
                return;
            }

            var prefabFiles = Directory.GetFiles(directory, "*.prefab", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains(".meta"))
                .ToList();

            foreach (var prefabPath in prefabFiles)
            {
                var prefabFileName = Path.GetFileNameWithoutExtension(prefabPath);
                
                // 根据模式判断是否需要处理
                bool shouldProcess = false;
                string newPrefabName = null;
                
                if (string.IsNullOrEmpty(m_OldPrefix))
                {
                    // 添加前缀模式：只处理没有前缀的预制体
                    if (prefabFileName.StartsWith(m_Prefix))
                        continue; // 已有新前缀，跳过
                    
                    // 检查是否有对应的脚本文件需要重命名
                    var matchingScript = m_PreviewResults.FirstOrDefault(r => 
                        r.OldClassName == prefabFileName || 
                        r.NewClassName == prefabFileName);
                    
                    if (matchingScript != null)
                    {
                        shouldProcess = true;
                        newPrefabName = matchingScript.NewClassName + ".prefab";
                    }
                }
                else
                {
                    // 替换前缀模式：只处理有旧前缀的预制体
                    if (prefabFileName.StartsWith(m_Prefix))
                        continue; // 已有新前缀，跳过
                    if (!prefabFileName.StartsWith(m_OldPrefix))
                        continue; // 没有旧前缀，跳过
                    
                    // 移除旧前缀，添加新前缀
                    var nameWithoutPrefix = prefabFileName.Substring(m_OldPrefix.Length);
                    newPrefabName = m_Prefix + nameWithoutPrefix + ".prefab";
                    shouldProcess = true;
                }
                
                if (shouldProcess && !string.IsNullOrEmpty(newPrefabName))
                {
                    var renameInfo = new PrefabRenameInfo
                    {
                        PrefabPath = prefabPath,
                        OldPrefabName = Path.GetFileName(prefabPath),
                        NewPrefabName = newPrefabName
                    };
                    
                    m_PrefabRenameResults.Add(renameInfo);
                }
            }
        }

        /// <summary>
        /// 执行重命名
        /// </summary>
        private void ExecuteRename()
        {
            m_LogMessages.Clear();
            AddLog("开始执行重命名...");
            
            AssetDatabase.StartAssetEditing();
            
            try
            {
                int successCount = 0;
                int failCount = 0;
                
                foreach (var info in m_PreviewResults)
                {
                    try
                    {
                        RenameFile(info);
                        successCount++;
                    }
                    catch (Exception e)
                    {
                        AddLog($"错误: 重命名 {info.OldFileName} 失败 - {e.Message}");
                        failCount++;
                    }
                }
                
                // 更新UIViews枚举
                UpdateUIViewsEnum();
                
                // 更新其他文件中的引用
                UpdateReferences();
                
                // 重命名预制体文件
                int prefabSuccessCount = 0;
                int prefabFailCount = 0;
                foreach (var prefabInfo in m_PrefabRenameResults)
                {
                    try
                    {
                        RenamePrefab(prefabInfo);
                        prefabSuccessCount++;
                    }
                    catch (Exception e)
                    {
                        AddLog($"错误: 重命名预制体 {prefabInfo.OldPrefabName} 失败 - {e.Message}");
                        prefabFailCount++;
                    }
                }
                
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                
                string resultMessage = $"重命名完成！\n脚本文件: 成功 {successCount}, 失败 {failCount}";
                if (m_PrefabRenameResults.Count > 0)
                {
                    resultMessage += $"\n预制体文件: 成功 {prefabSuccessCount}, 失败 {prefabFailCount}";
                }
                AddLog(resultMessage);
                m_PreviewResults.Clear();
                m_PrefabRenameResults.Clear();
                
                string dialogMessage = $"重命名完成！\n脚本文件: 成功 {successCount}, 失败 {failCount}";
                if (m_PrefabRenameResults.Count > 0)
                {
                    dialogMessage += $"\n预制体文件: 成功 {prefabSuccessCount}, 失败 {prefabFailCount}";
                }
                EditorUtility.DisplayDialog("完成", dialogMessage, "确定");
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                AddLog($"执行失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"执行失败: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 重命名单个文件
        /// </summary>
        private void RenameFile(FileRenameInfo info)
        {
            var filePath = info.FilePath;
            var dir = Path.GetDirectoryName(filePath);
            var newFilePath = Path.Combine(dir, info.NewFileName);
            
            // 读取文件内容
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            
            // 先处理 AddComponentMenu 特性（如果需要）
            string addComponentMenuReplacement = null;
            if (info.NeedsAddComponentMenu)
            {
                if (!string.IsNullOrEmpty(info.AddComponentMenuValue))
                {
                    // AddComponentMenu的值保持原始类名不变（不带前缀）
                    // 这样Unity预制体中的组件引用不会丢失
                    // 例如：[AddComponentMenu("UI/GameUIForm")] 保持不变
                    // 即使类名已经改为 PaopaoGameUIForm
                    // 需要确保在替换类名时，不会替换 AddComponentMenu 中的值
                }
                else
                {
                    // 如果没有AddComponentMenu，准备添加一个（使用原始类名，不带前缀）
                    var menuValue = info.IsUIScript ? 
                        $"UI/{info.OldClassName}" : 
                        $"Items/{info.OldClassName}";
                    
                    // 在类定义前添加特性
                    // 找到类定义的位置（使用旧类名，因为还没替换）
                    var classDefMatch = Regex.Match(content, $@"(\s*)(public\s+(?:partial\s+)?class\s+{Regex.Escape(info.OldClassName)})");
                    if (classDefMatch.Success)
                    {
                        var indent = classDefMatch.Groups[1].Value;
                        var classDef = classDefMatch.Groups[2].Value;
                        addComponentMenuReplacement = $"{indent}[AddComponentMenu(\"{menuValue}\")]\n{indent}{classDef.Replace(info.OldClassName, info.NewClassName)}";
                    }
                }
            }
            
            // 替换类名（排除 AddComponentMenu 特性中的内容）
            // 使用更精确的匹配，避免替换字符串字面量中的类名
            content = Regex.Replace(content, 
                $@"\b{Regex.Escape(info.OldClassName)}\b", 
                match =>
                {
                    // 检查是否在 AddComponentMenu 特性的字符串中
                    var pos = match.Index;
                    var beforeMatch = content.Substring(0, pos);
                    
                    // 检查是否在 [AddComponentMenu("...")] 中
                    var lastAddComponentMenu = beforeMatch.LastIndexOf("[AddComponentMenu");
                    if (lastAddComponentMenu >= 0)
                    {
                        var afterAddComponentMenu = content.Substring(lastAddComponentMenu);
                        var endQuote = afterAddComponentMenu.IndexOf('"', afterAddComponentMenu.IndexOf('"') + 1);
                        if (endQuote > 0 && pos < lastAddComponentMenu + endQuote)
                        {
                            // 在 AddComponentMenu 的字符串中，不替换
                            return match.Value;
                        }
                    }
                    
                    return info.NewClassName;
                });
            
            // 如果有新的 AddComponentMenu 需要添加，现在添加
            if (!string.IsNullOrEmpty(addComponentMenuReplacement))
            {
                var classDefMatch = Regex.Match(content, $@"(\s*)(public\s+(?:partial\s+)?class\s+{Regex.Escape(info.NewClassName)})");
                if (classDefMatch.Success)
                {
                    content = content.Replace(classDefMatch.Value, addComponentMenuReplacement);
                }
            }
            
            // 写入新文件
            File.WriteAllText(newFilePath, content, Encoding.UTF8);
            
            // 删除旧文件
            File.Delete(filePath);
            
            // 处理.meta文件
            var oldMetaPath = filePath + ".meta";
            var newMetaPath = newFilePath + ".meta";
            if (File.Exists(oldMetaPath))
            {
                File.Move(oldMetaPath, newMetaPath);
            }
            
            AddLog($"✓ {info.OldFileName} → {info.NewFileName}");
            
            // 如果是UI脚本，检查是否有对应的.Variables.cs文件
            if (info.IsUIScript && !filePath.Contains("UIVariables"))
            {
                // 尝试多个可能的路径
                var possiblePaths = new[]
                {
                    Path.Combine(m_UIVariablesPath, info.OldClassName + ".Variables.cs"),
                    Path.Combine(Path.GetDirectoryName(filePath), "UIVariables", info.OldClassName + ".Variables.cs"),
                    Path.Combine(Path.GetDirectoryName(filePath), info.OldClassName + ".Variables.cs")
                };
                
                foreach (var variablesFilePath in possiblePaths)
                {
                    if (File.Exists(variablesFilePath))
                    {
                        RenameVariablesFile(variablesFilePath, info);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 重命名Variables文件
        /// </summary>
        private void RenameVariablesFile(string filePath, FileRenameInfo mainFileInfo)
        {
            var dir = Path.GetDirectoryName(filePath);
            var newFilePath = Path.Combine(dir, mainFileInfo.NewClassName + ".Variables.cs");
            
            // 读取文件内容
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            
            // 替换类名（partial class）
            content = Regex.Replace(content,
                $@"public\s+partial\s+class\s+{Regex.Escape(mainFileInfo.OldClassName)}\b",
                $"public partial class {mainFileInfo.NewClassName}");
            
            // 写入新文件
            File.WriteAllText(newFilePath, content, Encoding.UTF8);
            
            // 删除旧文件
            File.Delete(filePath);
            
            // 处理.meta文件
            var oldMetaPath = filePath + ".meta";
            var newMetaPath = newFilePath + ".meta";
            if (File.Exists(oldMetaPath))
            {
                File.Move(oldMetaPath, newMetaPath);
            }
            
            AddLog($"✓ {Path.GetFileName(filePath)} → {Path.GetFileName(newFilePath)}");
        }

        /// <summary>
        /// 更新UIViews枚举
        /// </summary>
        private void UpdateUIViewsEnum()
        {
            if (!File.Exists(m_UIViewsPath))
            {
                AddLog($"警告: UIViews文件不存在 - {m_UIViewsPath}");
                return;
            }
            
            var content = File.ReadAllText(m_UIViewsPath, Encoding.UTF8);
            var originalContent = content;
            
            // 更新枚举值（只更新没有前缀的）
            foreach (var info in m_PreviewResults.Where(r => r.IsUIScript))
            {
                // 匹配枚举项：OldClassName = number
                var pattern = $@"\b{Regex.Escape(info.OldClassName)}\s*=";
                if (Regex.IsMatch(content, pattern))
                {
                    content = Regex.Replace(content, pattern, 
                        $"{info.NewClassName} =");
                    AddLog($"✓ 更新UIViews枚举: {info.OldClassName} → {info.NewClassName}");
                }
            }
            
            if (content != originalContent)
            {
                File.WriteAllText(m_UIViewsPath, content, Encoding.UTF8);
            }
        }

        /// <summary>
        /// 更新其他文件中的引用
        /// </summary>
        private void UpdateReferences()
        {
            AddLog("开始更新引用...");
            
            // 默认排除项
            var defaultExcludes = new[]
            {
                "UIViews.cs",
                "UIFormBase.cs",
                "UIItemBase.cs",
                "UIItemObject.cs",
                "UIParams.cs",
                "SoundGroupTable.cs",
                "UIGroupTable.cs",
            };
            
            // 扫描所有.cs文件（排除编辑器工具和meta文件）
            var allScripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                .Where(f => 
                {
                    if (f.Contains(".meta"))
                        return false;
                    
                    if (f.EndsWith("ClassNamePrefixTool.cs"))
                        return false;
                    
                    var fileName = Path.GetFileName(f);
                    var filePath = f.Replace('\\', '/');
                    
                    // 排除默认排除项
                    foreach (var exclude in defaultExcludes)
                    {
                        if (f.Contains(exclude))
                            return false;
                    }
                    
                    // 排除用户自定义排除路径
                    foreach (var excludePath in m_ExcludePaths)
                    {
                        if (!string.IsNullOrEmpty(excludePath))
                        {
                            if (fileName.Contains(excludePath) || filePath.Contains(excludePath))
                                return false;
                        }
                    }
                    
                    return true;
                })
                .ToList();
            
            int updateCount = 0;
            foreach (var scriptPath in allScripts)
            {
                try
                {
                    var content = File.ReadAllText(scriptPath, Encoding.UTF8);
                    var originalContent = content;
                    
                    // 更新每个重命名的类的引用
                    foreach (var info in m_PreviewResults)
                    {
                        // 使用单词边界匹配，避免部分匹配
                        var pattern = $@"\b{Regex.Escape(info.OldClassName)}\b";
                        if (Regex.IsMatch(content, pattern))
                        {
                            content = Regex.Replace(content, pattern, info.NewClassName);
                        }
                    }
                    
                    if (content != originalContent)
                    {
                        File.WriteAllText(scriptPath, content, Encoding.UTF8);
                        updateCount++;
                        AddLog($"✓ 更新引用: {Path.GetFileName(scriptPath)}");
                    }
                }
                catch (Exception e)
                {
                    AddLog($"警告: 更新引用失败 {scriptPath} - {e.Message}");
                }
            }
            
            AddLog($"引用更新完成，共更新 {updateCount} 个文件");
        }

        /// <summary>
        /// 重命名预制体文件
        /// </summary>
        private void RenamePrefab(PrefabRenameInfo info)
        {
            var prefabPath = info.PrefabPath;
            var dir = Path.GetDirectoryName(prefabPath);
            var newPrefabPath = Path.Combine(dir, info.NewPrefabName);
            
            // 使用AssetDatabase重命名（Unity推荐方式）
            string error = AssetDatabase.MoveAsset(prefabPath, newPrefabPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }
            
            AddLog($"✓ 预制体: {info.OldPrefabName} → {info.NewPrefabName}");
        }

        private void AddLog(string message)
        {
            m_LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[类名前缀工具] {message}");
        }
        
        /// <summary>
        /// 保存排除路径到EditorPrefs
        /// </summary>
        private void SaveExcludePaths()
        {
            var key = $"ClassNamePrefixTool_ExcludePaths_{m_Prefix}";
            var json = JsonUtility.ToJson(new ExcludePathsData { paths = m_ExcludePaths.ToArray() });
            EditorPrefs.SetString(key, json);
        }
        
        /// <summary>
        /// 从EditorPrefs加载排除路径
        /// </summary>
        private void LoadExcludePaths()
        {
            var key = $"ClassNamePrefixTool_ExcludePaths_{m_Prefix}";
            if (EditorPrefs.HasKey(key))
            {
                var json = EditorPrefs.GetString(key);
                try
                {
                    var data = JsonUtility.FromJson<ExcludePathsData>(json);
                    if (data != null && data.paths != null)
                    {
                        m_ExcludePaths = new List<string>(data.paths);
                    }
                }
                catch
                {
                    m_ExcludePaths = new List<string>();
                }
            }
            else
            {
                // 初始化默认排除路径（如果需要）
                m_ExcludePaths = new List<string>();
            }
        }
        
        [Serializable]
        private class ExcludePathsData
        {
            public string[] paths;
        }

        #region 数据结构

        private class ClassInfo
        {
            public string ClassName;
            public string AddComponentMenuValue;
            public bool NeedsAddComponentMenu; // 是否需要 AddComponentMenu 特性（只有继承 UIFormBase 或 UIItemBase 的类才需要）
        }

        private class FileRenameInfo
        {
            public string FilePath;
            public string OldFileName;
            public string NewFileName;
            public string OldClassName;
            public string NewClassName;
            public string AddComponentMenuValue;
            public bool NeedsAddComponentMenu; // 是否需要 AddComponentMenu 特性
            public bool IsUIScript;
        }

        private class PrefabRenameInfo
        {
            public string PrefabPath;
            public string OldPrefabName;
            public string NewPrefabName;
        }

        #endregion
    }
}
#endif

