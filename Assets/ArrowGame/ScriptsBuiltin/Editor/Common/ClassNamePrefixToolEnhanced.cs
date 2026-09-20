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
    /// 增强版类名前缀工具
    /// 支持程序集级别的批量处理，降低代码重复率，适用于iOS上架审核
    /// </summary>
    public class ClassNamePrefixToolEnhanced : EditorWindow
    {
        private string m_Prefix = "Mahjong";
        private string m_OldPrefix = "";
        
        // 程序集选择
        private bool m_ProcessBuiltinRuntime = true;
        private bool m_ProcessHotfix = true;
        private bool m_ProcessUIScripts = true;
        private bool m_ProcessDataTables = true;
        private bool m_ProcessOtherScripts = true;
        
        // 处理选项
        private bool m_ProcessClasses = true;
        private bool m_ProcessInterfaces = true;
        private bool m_ProcessEnums = true;
        private bool m_ProcessStructs = true;
        private bool m_ProcessDelegates = true;
        private bool m_ProcessNamespaces = false; // 命名空间处理默认关闭，因为可能影响较大
        private bool m_ProcessSerializableClasses = true;
        private bool m_ProcessPrefabs = true;
        private bool m_ProcessConfigFiles = false; // JSON/XML配置文件处理默认关闭
        
        // 路径配置
        private string m_BuiltinRuntimePath = "Assets/ArrowGame/ScriptsBuiltin/Runtime";
        private string m_HotfixPath = "Assets/ArrowGame/Scripts";
        private string m_UIScriptsPath = ConstEditor.UIScriptsPath;
        private string m_DataTablePath = ConstEditor.DataTableCodePath;
        private string m_UIVariablesPath = ConstEditor.UISerializeFieldDir;
        private string m_UIViewsPath = ConstEditor.UIViewScriptFile;
        private string m_UIPrefabsPath = "Assets/ArrowGame/Prefabs/UI";
        private string m_UIItemPrefabsPath = "Assets/ArrowGame/Prefabs/UI/Items";
        
        // UI状态
        private Vector2 m_ScrollPosition;
        private Vector2 m_ExcludeScrollPosition;
        private Vector2 m_OptionsScrollPosition;
        private List<string> m_LogMessages = new List<string>();
        private bool m_ShowPreview = true;
        private bool m_ShowAdvancedOptions = false;
        private List<CodeElementRenameInfo> m_PreviewResults = new List<CodeElementRenameInfo>();
        private List<PrefabRenameInfo> m_PrefabRenameResults = new List<PrefabRenameInfo>();
        
        // 排除配置
        private List<string> m_ExcludePaths = new List<string>();
        private string m_NewExcludePath = "";
        private bool m_ShowExcludePaths = false;
        
        // 统计信息
        private RenameStatistics m_Statistics = new RenameStatistics();

        [MenuItem("Tools/类名前缀工具(增强版)")]
        public static void ShowWindow()
        {
            var window = GetWindow<ClassNamePrefixToolEnhanced>("类名前缀工具(增强版)");
            window.minSize = new Vector2(700, 600);
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
            
            // 标题和说明
            EditorGUILayout.LabelField("增强版类名前缀工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("此工具支持程序集级别的批量处理，可处理类、接口、枚举、结构体、委托等多种代码元素。\n" +
                                   "适用于iOS上架审核，降低代码重复率。", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 前缀配置
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("前缀配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("旧前缀:", GUILayout.Width(80));
            m_OldPrefix = EditorGUILayout.TextField(m_OldPrefix);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("留空表示添加前缀模式，填写表示替换前缀模式", MessageType.None);
            
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
                EditorGUILayout.HelpBox($"当前模式：添加前缀模式 - 将为没有前缀的代码元素添加 \"{m_Prefix}\" 前缀", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"当前模式：替换前缀模式 - 将 \"{m_OldPrefix}\" 替换为 \"{m_Prefix}\"", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 程序集选择
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("处理范围", EditorStyles.boldLabel);
            m_ProcessBuiltinRuntime = EditorGUILayout.Toggle("处理 Builtin.Runtime 程序集", m_ProcessBuiltinRuntime);
            m_ProcessHotfix = EditorGUILayout.Toggle("处理 Hotfix 程序集", m_ProcessHotfix);
            m_ProcessUIScripts = EditorGUILayout.Toggle("处理 UI 脚本", m_ProcessUIScripts);
            m_ProcessDataTables = EditorGUILayout.Toggle("处理数据表脚本", m_ProcessDataTables);
            m_ProcessOtherScripts = EditorGUILayout.Toggle("处理其他脚本", m_ProcessOtherScripts);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 处理选项
            m_ShowAdvancedOptions = EditorGUILayout.Foldout(m_ShowAdvancedOptions, "高级选项", true);
            if (m_ShowAdvancedOptions)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("代码元素类型", EditorStyles.boldLabel);
                m_ProcessClasses = EditorGUILayout.Toggle("处理类 (class)", m_ProcessClasses);
                m_ProcessInterfaces = EditorGUILayout.Toggle("处理接口 (interface)", m_ProcessInterfaces);
                m_ProcessEnums = EditorGUILayout.Toggle("处理枚举 (enum)", m_ProcessEnums);
                m_ProcessStructs = EditorGUILayout.Toggle("处理结构体 (struct)", m_ProcessStructs);
                m_ProcessDelegates = EditorGUILayout.Toggle("处理委托 (delegate)", m_ProcessDelegates);
                m_ProcessNamespaces = EditorGUILayout.Toggle("处理命名空间 (namespace)", m_ProcessNamespaces);
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("其他选项", EditorStyles.boldLabel);
                m_ProcessSerializableClasses = EditorGUILayout.Toggle("处理序列化类", m_ProcessSerializableClasses);
                m_ProcessPrefabs = EditorGUILayout.Toggle("处理预制体", m_ProcessPrefabs);
                m_ProcessConfigFiles = EditorGUILayout.Toggle("处理配置文件 (JSON/XML)", m_ProcessConfigFiles);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(10);
            
            // 排除路径配置
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_ShowExcludePaths = EditorGUILayout.Foldout(m_ShowExcludePaths, "排除文件路径配置", true);
            if (m_ShowExcludePaths)
            {
                EditorGUILayout.HelpBox("在此添加需要排除的文件路径或文件名（支持部分匹配）。", MessageType.Info);
                
                EditorGUILayout.Space(5);
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
                }
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
                
                string fileDescription = $"{m_PreviewResults.Count} 个代码元素";
                if (m_PrefabRenameResults.Count > 0)
                {
                    fileDescription += $" 和 {m_PrefabRenameResults.Count} 个预制体文件";
                }
                
                if (EditorUtility.DisplayDialog("确认", 
                    $"确定要为 {fileDescription}{modeDescription}吗？\n\n" +
                    "此操作会修改代码元素名称、文件名和预制体名，建议先备份项目！", 
                    "确定", "取消"))
                {
                    ExecuteRename();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 统计信息
            if (m_Statistics.TotalProcessed > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"总处理数: {m_Statistics.TotalProcessed}");
                EditorGUILayout.LabelField($"类: {m_Statistics.Classes}, 接口: {m_Statistics.Interfaces}, 枚举: {m_Statistics.Enums}");
                EditorGUILayout.LabelField($"结构体: {m_Statistics.Structs}, 委托: {m_Statistics.Delegates}");
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(10);
            
            // 预览结果
            int totalPreviewFiles = m_PreviewResults.Count + m_PrefabRenameResults.Count;
            if (m_ShowPreview && totalPreviewFiles > 0)
            {
                EditorGUILayout.LabelField($"预览结果 ({totalPreviewFiles} 个):", EditorStyles.boldLabel);
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(200));
                
                // 按类型分组显示
                var groupedResults = m_PreviewResults.GroupBy(r => r.ElementType).OrderBy(g => g.Key);
                foreach (var group in groupedResults)
                {
                    EditorGUILayout.LabelField($"{group.Key} ({group.Count()} 个):", EditorStyles.boldLabel);
                    foreach (var result in group)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"文件: {result.OldFileName} → {result.NewFileName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"{group.Key}: {result.OldElementName} → {result.NewElementName}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                    }
                }
                
                // 显示预制体
                if (m_PrefabRenameResults.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"预制体文件 ({m_PrefabRenameResults.Count} 个):", EditorStyles.boldLabel);
                    foreach (var prefab in m_PrefabRenameResults)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"预制体: {prefab.OldPrefabName} → {prefab.NewPrefabName}", EditorStyles.miniLabel);
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
            m_PrefabRenameResults.Clear();
            m_Statistics.Reset();
            m_LogMessages.Clear();
            
            string modeDescription = string.IsNullOrEmpty(m_OldPrefix) 
                ? $"添加前缀模式 - 将为没有前缀的代码元素添加 \"{m_Prefix}\" 前缀"
                : $"替换前缀模式 - 将 \"{m_OldPrefix}\" 前缀替换为 \"{m_Prefix}\" 前缀";
            
            AddLog($"开始预览... ({modeDescription})");
            
            // 处理Builtin.Runtime程序集
            if (m_ProcessBuiltinRuntime)
            {
                AddLog("扫描 Builtin.Runtime 程序集...");
                ScanAssemblyDirectory(m_BuiltinRuntimePath, "Builtin.Runtime");
            }
            
            // 处理Hotfix程序集
            if (m_ProcessHotfix)
            {
                AddLog("扫描 Hotfix 程序集...");
                ScanAssemblyDirectory(m_HotfixPath, "Hotfix");
            }
            
            // 处理UI脚本
            if (m_ProcessUIScripts)
            {
                AddLog("扫描 UI 脚本...");
                ScanDirectory(m_UIScriptsPath, true);
            }
            
            // 处理数据表脚本
            if (m_ProcessDataTables)
            {
                AddLog("扫描数据表脚本...");
                ScanDirectory(m_DataTablePath, false);
            }
            
            // 处理其他脚本（如果启用）
            if (m_ProcessOtherScripts)
            {
                // 可以添加其他路径的扫描
            }
            
            // 扫描预制体
            if (m_ProcessPrefabs)
            {
                ScanPrefabs();
            }
            
            AddLog($"预览完成，共找到 {m_PreviewResults.Count} 个代码元素和 {m_PrefabRenameResults.Count} 个预制体文件需要重命名");
        }

        /// <summary>
        /// 扫描程序集目录
        /// </summary>
        private void ScanAssemblyDirectory(string directory, string assemblyName)
        {
            if (!Directory.Exists(directory))
            {
                AddLog($"警告: 目录不存在 - {directory}");
                return;
            }

            var files = GetScriptFiles(directory);
            
            foreach (var filePath in files)
            {
                ProcessScriptFile(filePath, false);
            }
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

            var files = GetScriptFiles(directory);
            
            foreach (var filePath in files)
            {
                ProcessScriptFile(filePath, isUIScript);
            }
        }

        /// <summary>
        /// 获取脚本文件列表（排除不需要的文件）
        /// </summary>
        private List<string> GetScriptFiles(string directory)
        {
            var defaultExcludes = new[]
            {
                "UIViews.cs",
                "UIFormBase.cs",
                "UIItemBase.cs",
                "UIItemObject.cs",
                "UIParams.cs",
                "ArrowSoundGroupTable.cs",
                "ArrowUIGroupTable.cs",
                "ClassNamePrefixTool",
                "ClassNamePrefixToolEnhanced"
            };
            
            return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
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
                            if (fileName.Contains(excludePath) || filePath.Contains(excludePath))
                                return false;
                        }
                    }
                    
                    return true;
                })
                .ToList();
        }

        /// <summary>
        /// 处理脚本文件
        /// </summary>
        private void ProcessScriptFile(string filePath, bool isUIScript)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // 跳过.Variables.cs文件（会在主文件中处理）
            if (fileName.EndsWith(".Variables"))
                return;
            
            // 根据模式判断是否需要处理
            bool shouldProcess = false;
            if (string.IsNullOrEmpty(m_OldPrefix))
            {
                // 添加前缀模式：只处理没有前缀的文件
                if (fileName.StartsWith(m_Prefix))
                    return; // 已有新前缀，跳过
                shouldProcess = true;
            }
            else
            {
                // 替换前缀模式：只处理有旧前缀的文件
                if (fileName.StartsWith(m_Prefix))
                    return; // 已有新前缀，跳过
                if (!fileName.StartsWith(m_OldPrefix))
                    return; // 没有旧前缀，跳过
                shouldProcess = true;
            }
            
            if (!shouldProcess)
                return;
            
            // 读取文件内容
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            
            // 解析各种代码元素
            if (m_ProcessClasses)
            {
                ProcessClasses(content, filePath, fileName, isUIScript);
            }
            
            if (m_ProcessInterfaces)
            {
                ProcessInterfaces(content, filePath, fileName);
            }
            
            if (m_ProcessEnums)
            {
                ProcessEnums(content, filePath, fileName);
            }
            
            if (m_ProcessStructs)
            {
                ProcessStructs(content, filePath, fileName);
            }
            
            if (m_ProcessDelegates)
            {
                ProcessDelegates(content, filePath, fileName);
            }
        }

        /// <summary>
        /// 处理类
        /// </summary>
        private void ProcessClasses(string content, string filePath, string fileName, bool isUIScript)
        {
            // 匹配类定义：public (partial )?class ClassName : BaseClass
            var classMatches = Regex.Matches(content, @"(public\s+|internal\s+|private\s+)?(abstract\s+|sealed\s+)?(partial\s+)?class\s+(\w+)(\s*:\s*([\w\.]+))?");
            
            foreach (Match match in classMatches)
            {
                var className = match.Groups[4].Value;
                
                // 检查是否需要处理
                if (ShouldProcessElement(className))
                {
                    var newClassName = GetNewElementName(className);
                    var newFileName = GetNewFileName(fileName);
                    
                    var classInfo = ParseClassInfo(content, className);
                    
                    var renameInfo = new CodeElementRenameInfo
                    {
                        FilePath = filePath,
                        OldFileName = Path.GetFileName(filePath),
                        NewFileName = newFileName + ".cs",
                        OldElementName = className,
                        NewElementName = newClassName,
                        ElementType = "类",
                        AddComponentMenuValue = classInfo?.AddComponentMenuValue,
                        NeedsAddComponentMenu = classInfo?.NeedsAddComponentMenu ?? false,
                        IsUIScript = isUIScript
                    };
                    
                    m_PreviewResults.Add(renameInfo);
                    m_Statistics.Classes++;
                    m_Statistics.TotalProcessed++;
                }
            }
        }

        /// <summary>
        /// 处理接口
        /// </summary>
        private void ProcessInterfaces(string content, string filePath, string fileName)
        {
            var interfaceMatches = Regex.Matches(content, @"(public\s+|internal\s+)?interface\s+(\w+)(\s*:\s*([\w\.]+))?");
            
            foreach (Match match in interfaceMatches)
            {
                var interfaceName = match.Groups[2].Value;
                
                if (ShouldProcessElement(interfaceName))
                {
                    var newInterfaceName = GetNewElementName(interfaceName);
                    var newFileName = GetNewFileName(fileName);
                    
                    var renameInfo = new CodeElementRenameInfo
                    {
                        FilePath = filePath,
                        OldFileName = Path.GetFileName(filePath),
                        NewFileName = newFileName + ".cs",
                        OldElementName = interfaceName,
                        NewElementName = newInterfaceName,
                        ElementType = "接口",
                        IsUIScript = false
                    };
                    
                    m_PreviewResults.Add(renameInfo);
                    m_Statistics.Interfaces++;
                    m_Statistics.TotalProcessed++;
                }
            }
        }

        /// <summary>
        /// 处理枚举
        /// </summary>
        private void ProcessEnums(string content, string filePath, string fileName)
        {
            var enumMatches = Regex.Matches(content, @"(public\s+|internal\s+)?enum\s+(\w+)");
            
            foreach (Match match in enumMatches)
            {
                var enumName = match.Groups[2].Value;
                
                if (ShouldProcessElement(enumName))
                {
                    var newEnumName = GetNewElementName(enumName);
                    var newFileName = GetNewFileName(fileName);
                    
                    var renameInfo = new CodeElementRenameInfo
                    {
                        FilePath = filePath,
                        OldFileName = Path.GetFileName(filePath),
                        NewFileName = newFileName + ".cs",
                        OldElementName = enumName,
                        NewElementName = newEnumName,
                        ElementType = "枚举",
                        IsUIScript = false
                    };
                    
                    m_PreviewResults.Add(renameInfo);
                    m_Statistics.Enums++;
                    m_Statistics.TotalProcessed++;
                }
            }
        }

        /// <summary>
        /// 处理结构体
        /// </summary>
        private void ProcessStructs(string content, string filePath, string fileName)
        {
            var structMatches = Regex.Matches(content, @"(public\s+|internal\s+)?struct\s+(\w+)");
            
            foreach (Match match in structMatches)
            {
                var structName = match.Groups[2].Value;
                
                if (ShouldProcessElement(structName))
                {
                    var newStructName = GetNewElementName(structName);
                    var newFileName = GetNewFileName(fileName);
                    
                    var renameInfo = new CodeElementRenameInfo
                    {
                        FilePath = filePath,
                        OldFileName = Path.GetFileName(filePath),
                        NewFileName = newFileName + ".cs",
                        OldElementName = structName,
                        NewElementName = newStructName,
                        ElementType = "结构体",
                        IsUIScript = false
                    };
                    
                    m_PreviewResults.Add(renameInfo);
                    m_Statistics.Structs++;
                    m_Statistics.TotalProcessed++;
                }
            }
        }

        /// <summary>
        /// 处理委托
        /// </summary>
        private void ProcessDelegates(string content, string filePath, string fileName)
        {
            var delegateMatches = Regex.Matches(content, @"(public\s+|internal\s+)?delegate\s+[\w\<\>,\s]+\s+(\w+)\s*\(");
            
            foreach (Match match in delegateMatches)
            {
                var delegateName = match.Groups[2].Value;
                
                if (ShouldProcessElement(delegateName))
                {
                    var newDelegateName = GetNewElementName(delegateName);
                    var newFileName = GetNewFileName(fileName);
                    
                    var renameInfo = new CodeElementRenameInfo
                    {
                        FilePath = filePath,
                        OldFileName = Path.GetFileName(filePath),
                        NewFileName = newFileName + ".cs",
                        OldElementName = delegateName,
                        NewElementName = newDelegateName,
                        ElementType = "委托",
                        IsUIScript = false
                    };
                    
                    m_PreviewResults.Add(renameInfo);
                    m_Statistics.Delegates++;
                    m_Statistics.TotalProcessed++;
                }
            }
        }

        /// <summary>
        /// 判断是否应该处理该元素
        /// </summary>
        private bool ShouldProcessElement(string elementName)
        {
            if (string.IsNullOrEmpty(m_OldPrefix))
            {
                // 添加前缀模式：只处理没有前缀的元素
                return !elementName.StartsWith(m_Prefix);
            }
            else
            {
                // 替换前缀模式：只处理有旧前缀的元素
                if (elementName.StartsWith(m_Prefix))
                    return false; // 已有新前缀，跳过
                return elementName.StartsWith(m_OldPrefix);
            }
        }

        /// <summary>
        /// 获取新的元素名称
        /// </summary>
        private string GetNewElementName(string elementName)
        {
            if (string.IsNullOrEmpty(m_OldPrefix))
            {
                // 添加前缀模式
                return m_Prefix + elementName;
            }
            else
            {
                // 替换前缀模式
                if (elementName.StartsWith(m_OldPrefix))
                {
                    var nameWithoutPrefix = elementName.Substring(m_OldPrefix.Length);
                    return m_Prefix + nameWithoutPrefix;
                }
                return elementName;
            }
        }

        /// <summary>
        /// 获取新的文件名
        /// </summary>
        private string GetNewFileName(string fileName)
        {
            if (string.IsNullOrEmpty(m_OldPrefix))
            {
                // 添加前缀模式
                return m_Prefix + fileName;
            }
            else
            {
                // 替换前缀模式
                if (fileName.StartsWith(m_OldPrefix))
                {
                    var nameWithoutPrefix = fileName.Substring(m_OldPrefix.Length);
                    return m_Prefix + nameWithoutPrefix;
                }
                return fileName;
            }
        }

        /// <summary>
        /// 解析类信息
        /// </summary>
        private ClassInfo ParseClassInfo(string content, string className)
        {
            var classInfo = new ClassInfo();
            
            // 匹配类定义和基类
            var classMatch = Regex.Match(content, $@"(public\s+|internal\s+)?(abstract\s+|sealed\s+)?(partial\s+)?class\s+{Regex.Escape(className)}(\s*:\s*([\w\.]+))?");
            if (!classMatch.Success)
                return null;
            
            var baseClass = classMatch.Groups.Count > 5 && !string.IsNullOrEmpty(classMatch.Groups[5].Value) 
                ? classMatch.Groups[5].Value 
                : null;
            
            // 检查是否需要 AddComponentMenu 特性
            bool needsAddComponentMenu = false;
            if (!string.IsNullOrEmpty(baseClass))
            {
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
            
            return classInfo;
        }

        /// <summary>
        /// 扫描预制体文件
        /// </summary>
        private void ScanPrefabs()
        {
            m_PrefabRenameResults.Clear();
            
            if (Directory.Exists(m_UIPrefabsPath))
            {
                ScanPrefabDirectory(m_UIPrefabsPath);
            }
            
            if (Directory.Exists(m_UIItemPrefabsPath))
            {
                ScanPrefabDirectory(m_UIItemPrefabsPath);
            }
        }

        /// <summary>
        /// 扫描预制体目录
        /// </summary>
        private void ScanPrefabDirectory(string directory)
        {
            var prefabFiles = Directory.GetFiles(directory, "*.prefab", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains(".meta"))
                .ToList();

            foreach (var prefabPath in prefabFiles)
            {
                var prefabFileName = Path.GetFileNameWithoutExtension(prefabPath);
                
                bool shouldProcess = false;
                string newPrefabName = null;
                
                if (string.IsNullOrEmpty(m_OldPrefix))
                {
                    if (prefabFileName.StartsWith(m_Prefix))
                        continue;
                    
                    var matchingScript = m_PreviewResults.FirstOrDefault(r => 
                        r.OldElementName == prefabFileName || 
                        r.NewElementName == prefabFileName);
                    
                    if (matchingScript != null)
                    {
                        shouldProcess = true;
                        newPrefabName = matchingScript.NewElementName + ".prefab";
                    }
                }
                else
                {
                    if (prefabFileName.StartsWith(m_Prefix))
                        continue;
                    if (!prefabFileName.StartsWith(m_OldPrefix))
                        continue;
                    
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
                
                // 按文件分组处理（一个文件可能包含多个代码元素）
                var filesGrouped = m_PreviewResults.GroupBy(r => r.FilePath);
                
                foreach (var fileGroup in filesGrouped)
                {
                    try
                    {
                        RenameFile(fileGroup.ToList());
                        successCount++;
                    }
                    catch (Exception e)
                    {
                        AddLog($"错误: 重命名文件 {Path.GetFileName(fileGroup.Key)} 失败 - {e.Message}");
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
                m_Statistics.Reset();
                
                EditorUtility.DisplayDialog("完成", resultMessage, "确定");
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                AddLog($"执行失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"执行失败: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 重命名文件（处理文件中的所有代码元素）
        /// </summary>
        private void RenameFile(List<CodeElementRenameInfo> renameInfos)
        {
            if (renameInfos.Count == 0)
                return;
            
            var firstInfo = renameInfos[0];
            var filePath = firstInfo.FilePath;
            var dir = Path.GetDirectoryName(filePath);
            var newFilePath = Path.Combine(dir, firstInfo.NewFileName);
            
            // 读取文件内容
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var originalContent = content;
            
            // 处理每个需要重命名的代码元素
            foreach (var info in renameInfos)
            {
                // 替换代码元素名称（使用单词边界，避免部分匹配）
                content = Regex.Replace(content, 
                    $@"\b{Regex.Escape(info.OldElementName)}\b", 
                    match =>
                    {
                        // 检查是否在字符串字面量中
                        var pos = match.Index;
                        var beforeMatch = content.Substring(0, pos);
                        
                        // 检查是否在 AddComponentMenu 特性的字符串中
                        var lastAddComponentMenu = beforeMatch.LastIndexOf("[AddComponentMenu");
                        if (lastAddComponentMenu >= 0)
                        {
                            var afterAddComponentMenu = content.Substring(lastAddComponentMenu);
                            var firstQuote = afterAddComponentMenu.IndexOf('"');
                            var secondQuote = afterAddComponentMenu.IndexOf('"', firstQuote + 1);
                            if (secondQuote > 0 && pos < lastAddComponentMenu + secondQuote)
                            {
                                return match.Value; // 在字符串中，不替换
                            }
                        }
                        
                        // 检查是否在注释中
                        var lastLineComment = beforeMatch.LastIndexOf("//");
                        var lastBlockComment = beforeMatch.LastIndexOf("/*");
                        var lastBlockCommentEnd = beforeMatch.LastIndexOf("*/");
                        if (lastLineComment > lastBlockCommentEnd || (lastBlockComment > lastBlockCommentEnd && pos < beforeMatch.IndexOf('\n', lastLineComment)))
                        {
                            return match.Value; // 在注释中，不替换
                        }
                        
                        return info.NewElementName;
                    });
                
                // 处理 AddComponentMenu 特性
                if (info.NeedsAddComponentMenu && string.IsNullOrEmpty(info.AddComponentMenuValue))
                {
                    var menuValue = info.IsUIScript ? 
                        $"UI/{info.OldElementName}" : 
                        $"Items/{info.OldElementName}";
                    
                    var classDefMatch = Regex.Match(content, $@"(\s*)(public\s+(?:partial\s+)?class\s+{Regex.Escape(info.NewElementName)})");
                    if (classDefMatch.Success)
                    {
                        var indent = classDefMatch.Groups[1].Value;
                        var classDef = classDefMatch.Groups[2].Value;
                        var addComponentMenuReplacement = $"{indent}[AddComponentMenu(\"{menuValue}\")]\n{indent}{classDef}";
                        content = content.Replace(classDefMatch.Value, addComponentMenuReplacement);
                    }
                }
            }
            
            if (content != originalContent)
            {
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
            
            // 更新枚举值
            foreach (var info in m_PreviewResults.Where(r => r.IsUIScript && r.ElementType == "类"))
            {
                var pattern = $@"\b{Regex.Escape(info.OldElementName)}\s*=";
                if (Regex.IsMatch(content, pattern))
                {
                    content = Regex.Replace(content, pattern, 
                        $"{info.NewElementName} =");
                    AddLog($"✓ 更新UIViews枚举: {info.OldElementName} → {info.NewElementName}");
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
            
            var defaultExcludes = new[]
            {
                "UIViews.cs",
                "UIFormBase.cs",
                "UIItemBase.cs",
                "UIItemObject.cs",
                "UIParams.cs",
                "ArrowSoundGroupTable.cs",
                "ArrowUIGroupTable.cs",
                "ClassNamePrefixTool",
                "ClassNamePrefixToolEnhanced"
            };
            
            // 获取所有需要更新的文件
            var allScripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                .Where(f => 
                {
                    if (f.Contains(".meta"))
                        return false;
                    
                    var fileName = Path.GetFileName(f);
                    var filePath = f.Replace('\\', '/');
                    
                    foreach (var exclude in defaultExcludes)
                    {
                        if (f.Contains(exclude))
                            return false;
                    }
                    
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
                    
                    // 更新每个重命名的代码元素的引用
                    foreach (var info in m_PreviewResults)
                    {
                        var pattern = $@"\b{Regex.Escape(info.OldElementName)}\b";
                        if (Regex.IsMatch(content, pattern))
                        {
                            content = Regex.Replace(content, pattern, info.NewElementName);
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
            Debug.Log($"[类名前缀工具(增强版)] {message}");
        }
        
        private void SaveExcludePaths()
        {
            var key = $"ClassNamePrefixToolEnhanced_ExcludePaths_{m_Prefix}";
            var json = JsonUtility.ToJson(new ExcludePathsData { paths = m_ExcludePaths.ToArray() });
            EditorPrefs.SetString(key, json);
        }
        
        private void LoadExcludePaths()
        {
            var key = $"ClassNamePrefixToolEnhanced_ExcludePaths_{m_Prefix}";
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
            public string AddComponentMenuValue;
            public bool NeedsAddComponentMenu;
        }

        private class CodeElementRenameInfo
        {
            public string FilePath;
            public string OldFileName;
            public string NewFileName;
            public string OldElementName;
            public string NewElementName;
            public string ElementType; // 类、接口、枚举、结构体、委托
            public string AddComponentMenuValue;
            public bool NeedsAddComponentMenu;
            public bool IsUIScript;
        }

        private class PrefabRenameInfo
        {
            public string PrefabPath;
            public string OldPrefabName;
            public string NewPrefabName;
        }

        private class RenameStatistics
        {
            public int TotalProcessed = 0;
            public int Classes = 0;
            public int Interfaces = 0;
            public int Enums = 0;
            public int Structs = 0;
            public int Delegates = 0;

            public void Reset()
            {
                TotalProcessed = 0;
                Classes = 0;
                Interfaces = 0;
                Enums = 0;
                Structs = 0;
                Delegates = 0;
            }
        }

        #endregion
    }
}
#endif

