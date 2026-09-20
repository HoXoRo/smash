using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ArrowMaze;

namespace ArrowMaze.Editor
{
    /// <summary>
    /// 箭头迷宫关卡编辑器
    /// </summary>
    public class ArrowLevelEditor : EditorWindow
    {
        /// <summary>关卡数据默认存放路径（相对于工程根目录）</summary>
        private const string DefaultLevelFolder = "Assets/ArrowGame/ArrowDataLevel";
        /// <summary>关卡文件名前缀，用于快捷切换</summary>
        private const string LevelFilePrefix = "Level";

        private ArrowLevelData m_CurrentLevelData;
        private Vector2 m_ScrollPosition;
        private Vector2 m_GridScrollPosition;
        
        // 网格绘制相关
        private bool[,] m_GridValidState; // 网格有效状态
        private Vector2Int m_GridSize = new Vector2Int(20, 20);
        private float m_GridCellSize = 20f;
        private Vector2 m_GridOffset = Vector2.zero;
        
        // 框选相关
        private bool m_IsSelecting = false;
        private Vector2Int m_SelectionStart;
        private Vector2Int m_SelectionEnd;
        private bool m_IsDragMode = false; // true=添加，false=移除
        
        // 箭头编辑相关
        private List<ArrowLineDef> m_EditingArrows = new List<ArrowLineDef>();
        private ArrowLineDef m_CurrentEditingArrow;
        private bool m_IsDrawingArrow = false;
        private List<Vector2Int> m_CurrentArrowPath = new List<Vector2Int>();
        /// <summary> 当前校验发现存在互斥关系的箭头集合，用于在编辑器中高亮显示 </summary>
        private HashSet<ArrowLineDef> m_ConflictingArrows = new HashSet<ArrowLineDef>();
        
        // UI状态
        private int m_SelectedTool = 0; // 0=选择网格, 1=绘制箭头
        private string[] m_ToolNames = { "选择网格", "绘制箭头" };
        
        // 文件路径
        private string m_CurrentFilePath = "";
        
        // 填充时缓存，减少大网格下的重复计算
        private HashSet<Vector2Int> m_CachedBoundsValidGridsRef;
        private bool m_CachedBoundsValid;
        private int m_CachedMinX, m_CachedMaxX, m_CachedMinY, m_CachedMaxY;

        // 自动填充配置
        private ArrowFillConfig m_FillConfig = new ArrowFillConfig
        {
            minLength = 2,
            maxLength = 10,
            allowTurn = true,
            autoFill = true
        };
        
        [MenuItem("ArrowMaze/关卡编辑器", false, 1)]
        public static void Open()
        {
            ArrowLevelEditor window = GetWindow<ArrowLevelEditor>("箭头迷宫关卡编辑器");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 创建新关卡
            CreateNewLevel();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            
            // 左侧面板
            DrawLeftPanel();
            
            // 右侧网格编辑区
            DrawGridEditor();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 文件操作
            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("新建关卡", "确定要创建新关卡吗？未保存的数据将丢失。", "确定", "取消"))
                {
                    CreateNewLevel();
                }
            }
            
            if (GUILayout.Button("加载", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadLevel();
            }
            
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveLevel();
            }
            
            if (GUILayout.Button("另存为", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveLevelAs();
            }
            
            GUILayout.Space(10);
            
            // 显示当前文件路径
            if (!string.IsNullOrEmpty(m_CurrentFilePath))
            {
                GUILayout.Label($"当前文件: {Path.GetFileName(m_CurrentFilePath)}", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("未保存", EditorStyles.miniLabel);
            }
            
            GUILayout.FlexibleSpace();
            
            // 工具选择
            m_SelectedTool = GUILayout.Toolbar(m_SelectedTool, m_ToolNames, EditorStyles.toolbarButton);
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制左侧面板
        /// </summary>
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            
            // 关卡信息
            DrawLevelInfo();
            
            EditorGUILayout.Space(10);
            
            // 网格设置
            DrawGridSettings();
            
            EditorGUILayout.Space(10);
            
            // 箭头填充配置
            DrawFillConfig();
            
            EditorGUILayout.Space(10);
            
            // 箭头列表
            DrawArrowList();
            
            EditorGUILayout.Space(10);
            
            // 操作按钮
            DrawActionButtons();
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            // 分隔线
            EditorGUILayout.BeginVertical(GUILayout.Width(1));
            GUI.Box(new Rect(0, 0, 1, position.height), "");
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制关卡信息
        /// </summary>
        private void DrawLevelInfo()
        {
            EditorGUILayout.LabelField("关卡信息", EditorStyles.boldLabel);
            
            if (m_CurrentLevelData != null)
            {
                m_CurrentLevelData.levelName = EditorGUILayout.TextField("关卡名称", m_CurrentLevelData.levelName);
                
                EditorGUI.BeginChangeCheck();
                m_CurrentLevelData.difficulty = EditorGUILayout.IntSlider("难度系数", m_CurrentLevelData.difficulty, 1, 5);
                if (EditorGUI.EndChangeCheck())
                {
                    // 难度改变时，提示用户需要重新填充箭头
                    if (m_CurrentLevelData.arrows.Count > 0)
                    {
                        EditorUtility.DisplayDialog("难度已更改", 
                            $"难度系数已设置为 {m_CurrentLevelData.difficulty}。\n\n" +
                            "难度说明：\n" +
                            "1-2 (简单-中等)：多个解，玩家有多种方式通关\n" +
                            "3 (困难)：3-5个解，需要一定策略\n" +
                            "4 (极难)：1-3个解，需要精确操作\n" +
                            "5 (地狱)：唯一解，任何失误都会失败\n\n" +
                            "建议：更改难度后重新填充箭头以应用新的难度策略。", 
                            "确定");
                    }
                }
                
                // 显示难度说明
                string difficultyDesc = "";
                switch (m_CurrentLevelData.difficulty)
                {
                    case 1:
                        difficultyDesc = "简单：多个解（10+），玩家有多种方式通关";
                        break;
                    case 2:
                        difficultyDesc = "中等：多个解（5-10），玩家有多种方式通关";
                        break;
                    case 3:
                        difficultyDesc = "困难：较少解（3-5），需要一定策略";
                        break;
                    case 4:
                        difficultyDesc = "极难：极少解（1-3），需要精确操作";
                        break;
                    case 5:
                        difficultyDesc = "地狱：唯一解（1），任何失误都会失败";
                        break;
                }
                EditorGUILayout.HelpBox(difficultyDesc, MessageType.Info);
                
                m_CurrentLevelData.cameraSize = EditorGUILayout.FloatField("相机大小", m_CurrentLevelData.cameraSize);
                
                EditorGUILayout.LabelField("有效网格数", m_CurrentLevelData.validGrids.Count.ToString());
                EditorGUILayout.LabelField("箭头数量", m_CurrentLevelData.arrows.Count.ToString());
            }
        }
        
        /// <summary>
        /// 绘制网格设置
        /// </summary>
        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("网格设置", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            Vector2Int newSize = EditorGUILayout.Vector2IntField("网格大小", m_GridSize);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSize.x > 0 && newSize.y > 0)
                {
                    ResizeGrid(newSize);
                }
            }
            
            if (m_CurrentLevelData != null)
            {
                m_CurrentLevelData.grid.spacing = EditorGUILayout.FloatField("网格间距", m_CurrentLevelData.grid.spacing);
                m_CurrentLevelData.grid.origin = EditorGUILayout.Vector3Field("原点", m_CurrentLevelData.grid.origin);
            }
            
            m_GridCellSize = EditorGUILayout.Slider("单元格大小", m_GridCellSize, 10f, 50f);
            
            if (GUILayout.Button("清除所有有效网格"))
            {
                if (EditorUtility.DisplayDialog("清除", "确定要清除所有有效网格吗？", "确定", "取消"))
                {
                    ClearAllValidGrids();
                }
            }
        }
        
        /// <summary>
        /// 绘制填充配置
        /// </summary>
        private void DrawFillConfig()
        {
            EditorGUILayout.LabelField("箭头填充配置", EditorStyles.boldLabel);
            
            m_FillConfig.minLength = EditorGUILayout.IntField("最小长度", m_FillConfig.minLength);
            m_FillConfig.maxLength = EditorGUILayout.IntField("最大长度", m_FillConfig.maxLength);
            m_FillConfig.allowTurn = EditorGUILayout.Toggle("允许拐弯", m_FillConfig.allowTurn);
            m_FillConfig.autoFill = EditorGUILayout.Toggle("自动填充", m_FillConfig.autoFill);
            
            if (m_CurrentLevelData != null)
            {
                m_CurrentLevelData.fillConfig = m_FillConfig;
            }
        }
        
        /// <summary>
        /// 绘制箭头列表（可折叠）
        /// </summary>
        private bool m_ShowArrowList = true;
        private void DrawArrowList()
        {
            m_ShowArrowList = EditorGUILayout.Foldout(m_ShowArrowList, "箭头列表", true);
            if (!m_ShowArrowList)
            {
                return;
            }
            
            if (m_CurrentLevelData != null)
            {
                for (int i = 0; i < m_CurrentLevelData.arrows.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = m_CurrentEditingArrow == m_CurrentLevelData.arrows[i];
                    if (GUILayout.Toggle(isSelected, $"箭头 {i + 1}", EditorStyles.miniButtonLeft))
                    {
                        if (!isSelected)
                        {
                            m_CurrentEditingArrow = m_CurrentLevelData.arrows[i];
                            Repaint(); // 触发重绘以显示高亮
                        }
                    }
                    else if (isSelected)
                    {
                        m_CurrentEditingArrow = null;
                        Repaint(); // 触发重绘以取消高亮
                    }
                    
                    if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                    {
                        if (m_CurrentEditingArrow == m_CurrentLevelData.arrows[i])
                        {
                            m_CurrentEditingArrow = null;
                        }
                        m_CurrentLevelData.arrows.RemoveAt(i);
                        Repaint();
                        break;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            // 关卡快捷切换（仅显示默认目录下以 Level 前缀命名的关卡）
            DrawQuickLevelSwitcher();

            // 校验当前关卡箭头合理性
            if (GUILayout.Button("校验箭头合理性"))
            {
                ValidateArrows();
            }
            
            if (GUILayout.Button("自动填充箭头"))
            {
                AutoFillArrows();
            }
            
            if (GUILayout.Button("清除所有箭头"))
            {
                if (EditorUtility.DisplayDialog("清除", "确定要清除所有箭头吗？", "确定", "取消"))
                {
                    ClearAllArrows();
                }
            }
        }
        
        /// <summary>
        /// 绘制网格编辑区
        /// </summary>
        private void DrawGridEditor()
        {
            EditorGUILayout.BeginVertical();
            
            // 网格绘制区域
            Rect gridRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            // 处理鼠标事件
            HandleMouseEvents(gridRect);
            
            // 绘制网格
            DrawGrid(gridRect);
            
            // 绘制有效网格
            DrawValidGrids(gridRect);
            
            // 绘制箭头路径
            DrawArrowPaths(gridRect);
            
            // 绘制选择框
            if (m_IsSelecting)
            {
                DrawSelectionRect(gridRect);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 处理鼠标事件
        /// </summary>
        private void HandleMouseEvents(Rect gridRect)
        {
            Event e = Event.current;
            
            if (!gridRect.Contains(e.mousePosition))
                return;
            
            // 计算鼠标所在的网格坐标
            Vector2 localPos = e.mousePosition - gridRect.position - m_GridOffset;
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(localPos.x / m_GridCellSize),
                Mathf.FloorToInt(localPos.y / m_GridCellSize)
            );
            
            // 限制在网格范围内
            gridPos.x = Mathf.Clamp(gridPos.x, 0, m_GridSize.x - 1);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, m_GridSize.y - 1);
            
            if (m_SelectedTool == 0) // 选择网格工具
            {
                HandleGridSelection(e, gridPos, gridRect);
            }
            else if (m_SelectedTool == 1) // 绘制箭头工具
            {
                HandleArrowDrawing(e, gridPos, gridRect);
            }
        }
        
        /// <summary>
        /// 处理网格选择
        /// </summary>
        private void HandleGridSelection(Event e, Vector2Int gridPos, Rect gridRect)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                m_IsSelecting = true;
                m_SelectionStart = gridPos;
                m_SelectionEnd = gridPos;
                
                // 判断是添加还是移除
                bool isCurrentlyValid = IsGridValid(gridPos);
                m_IsDragMode = !isCurrentlyValid; // 如果当前有效，则移除；否则添加
                
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && m_IsSelecting)
            {
                m_SelectionEnd = gridPos;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && m_IsSelecting)
            {
                ApplySelection();
                m_IsSelecting = false;
                e.Use();
            }
        }
        
        /// <summary>
        /// 处理箭头绘制
        /// 在绘制箭头模式下，允许点击箭头进行选中
        /// </summary>
        private void HandleArrowDrawing(Event e, Vector2Int gridPos, Rect gridRect)
        {
            // 如果点击了已有箭头，选中该箭头
            if (e.type == EventType.MouseDown && e.button == 0 && !m_IsDrawingArrow)
            {
                ArrowLineDef clickedArrow = GetArrowAtPosition(gridPos);
                if (clickedArrow != null)
                {
                    // 选中箭头
                    m_CurrentEditingArrow = clickedArrow;
                    Repaint(); // 触发重绘以显示高亮
                    e.Use();
                    return;
                }
            }
            
            // 如果正在绘制箭头，继续绘制流程
            if (m_IsDrawingArrow)
            {
                // 只能在没有箭头路径的有效网格上绘制
                if (!IsGridValid(gridPos))
                    return;
                
                // 检查是否已有箭头占用此网格
                if (IsGridOccupiedByArrow(gridPos))
                    return;
                
                if (e.type == EventType.MouseDrag)
                {
                    // 添加新点到路径（相邻且有效即可，允许内旋）
                    if (m_CurrentArrowPath.Count > 0)
                    {
                        Vector2Int lastPos = m_CurrentArrowPath[m_CurrentArrowPath.Count - 1];
                        if (IsAdjacent(lastPos, gridPos) && 
                            IsGridValid(gridPos) && 
                            !m_CurrentArrowPath.Contains(gridPos) &&
                            !IsGridOccupiedByArrow(gridPos))
                        {
                            m_CurrentArrowPath.Add(gridPos);
                        }
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    // 完成箭头绘制
                    if (m_CurrentArrowPath.Count >= m_FillConfig.minLength)
                    {
                        FinishArrowDrawing();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", $"箭头长度至少需要 {m_FillConfig.minLength} 个网格单位", "确定");
                        m_CurrentArrowPath.Clear();
                    }
                    m_IsDrawingArrow = false;
                    e.Use();
                }
                return;
            }
            
            // 开始绘制新箭头
            // 只能在没有箭头路径的有效网格上绘制
            if (!IsGridValid(gridPos))
                return;
            
            // 检查是否已有箭头占用此网格
            if (IsGridOccupiedByArrow(gridPos))
                return;
            
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                m_IsDrawingArrow = true;
                m_CurrentArrowPath.Clear();
                m_CurrentArrowPath.Add(gridPos);
                m_CurrentEditingArrow = null; // 取消选中已有箭头
                e.Use();
            }
        }
        
        /// <summary>
        /// 获取指定位置上的箭头（如果存在）
        /// </summary>
        private ArrowLineDef GetArrowAtPosition(Vector2Int gridPos)
        {
            if (m_CurrentLevelData == null || m_CurrentLevelData.arrows == null)
                return null;
            
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path != null && arrow.path.Contains(gridPos))
                {
                    return arrow;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 应用选择
        /// </summary>
        private void ApplySelection()
        {
            Vector2Int min = new Vector2Int(
                Mathf.Min(m_SelectionStart.x, m_SelectionEnd.x),
                Mathf.Min(m_SelectionStart.y, m_SelectionEnd.y)
            );
            Vector2Int max = new Vector2Int(
                Mathf.Max(m_SelectionStart.x, m_SelectionEnd.x),
                Mathf.Max(m_SelectionStart.y, m_SelectionEnd.y)
            );
            
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (m_IsDragMode)
                    {
                        // 添加
                        SetGridValid(pos, true);
                    }
                    else
                    {
                        // 移除
                        SetGridValid(pos, false);
                    }
                }
            }
            
            Repaint();
        }
        
        /// <summary>
        /// 完成箭头绘制
        /// </summary>
        private void FinishArrowDrawing()
        {
            ArrowLineDef arrow = new ArrowLineDef
            {
                path = new List<Vector2Int>(m_CurrentArrowPath),
                lineColor = GetRandomColor(),
                hitColor = Color.red,
                occupyAllNodes = true,
                startIndex = 0
            };
            
            m_CurrentLevelData.arrows.Add(arrow);
            m_CurrentEditingArrow = arrow;
            m_CurrentArrowPath.Clear();
            
            Repaint();
        }
        
        /// <summary>
        /// 绘制网格
        /// </summary>
        private void DrawGrid(Rect rect)
        {
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            
            // 绘制网格线
            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.3f, 0.3f);
            
            Vector2 startPos = rect.position + m_GridOffset;
            
            // 垂直线
            for (int x = 0; x <= m_GridSize.x; x++)
            {
                float xPos = startPos.x + x * m_GridCellSize;
                Handles.DrawLine(
                    new Vector3(xPos, startPos.y),
                    new Vector3(xPos, startPos.y + m_GridSize.y * m_GridCellSize)
                );
            }
            
            // 水平线
            for (int y = 0; y <= m_GridSize.y; y++)
            {
                float yPos = startPos.y + y * m_GridCellSize;
                Handles.DrawLine(
                    new Vector3(startPos.x, yPos),
                    new Vector3(startPos.x + m_GridSize.x * m_GridCellSize, yPos)
                );
            }
            
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 绘制有效网格
        /// </summary>
        private void DrawValidGrids(Rect rect)
        {
            Handles.BeginGUI();
            
            Vector2 startPos = rect.position + m_GridOffset;
            
            foreach (var gridPos in m_CurrentLevelData.validGrids)
            {
                if (gridPos.x >= 0 && gridPos.x < m_GridSize.x && 
                    gridPos.y >= 0 && gridPos.y < m_GridSize.y)
                {
                    Rect cellRect = new Rect(
                        startPos.x + gridPos.x * m_GridCellSize + 1,
                        startPos.y + gridPos.y * m_GridCellSize + 1,
                        m_GridCellSize - 2,
                        m_GridCellSize - 2
                    );
                    
                    EditorGUI.DrawRect(cellRect, new Color(0.3f, 0.7f, 0.3f, 0.5f));
                }
            }
            
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 绘制箭头路径
        /// </summary>
        private void DrawArrowPaths(Rect rect)
        {
            Handles.BeginGUI();
            
            Vector2 startPos = rect.position + m_GridOffset;
            
            // 绘制已完成的箭头
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path == null || arrow.path.Count < 2) continue;
                
                // 检查是否是选中的箭头
                bool isSelected = m_CurrentEditingArrow == arrow;
                bool isConflicting = m_ConflictingArrows != null && m_ConflictingArrows.Contains(arrow);
                
                // 选中优先级最高，其次是互斥高亮，最后是普通颜色
                Color arrowColor;
                float lineWidth;
                if (isSelected)
                {
                    arrowColor = Color.yellow;
                    lineWidth = 5f;
                }
                else if (isConflicting)
                {
                    arrowColor = Color.red;
                    lineWidth = 5f;
                }
                else
                {
                    arrowColor = Color.white;//arrow.lineColor;
                    lineWidth = 3f;
                }
                
                Handles.color = arrowColor;
                
                // 使用DrawAAPolyLine绘制带宽度的线
                Vector3[] linePoints = new Vector3[arrow.path.Count];
                for (int i = 0; i < arrow.path.Count; i++)
                {
                    Vector2Int pos = arrow.path[i];
                    linePoints[i] = new Vector3(
                        startPos.x + pos.x * m_GridCellSize + m_GridCellSize * 0.5f,
                        startPos.y + pos.y * m_GridCellSize + m_GridCellSize * 0.5f,
                        0
                    );
                }
                Handles.DrawAAPolyLine(lineWidth, linePoints);
                
                // 绘制箭头头部（选中时更大更明显）
                if (arrow.path.Count > 0)
                {
                    Vector2Int head = arrow.path[arrow.path.Count - 1];
                    Vector3 headPos = new Vector3(
                        startPos.x + head.x * m_GridCellSize + m_GridCellSize * 0.5f,
                        startPos.y + head.y * m_GridCellSize + m_GridCellSize * 0.5f,
                        0
                    );
                    float headRadius = isSelected ? m_GridCellSize * 0.3f : m_GridCellSize * 0.2f;
                    Handles.DrawSolidDisc(headPos, Vector3.forward, headRadius);
                }
            }
            
            // 绘制正在绘制的箭头
            if (m_CurrentArrowPath.Count > 0)
            {
                Handles.color = Color.cyan;
                
                // 使用DrawAAPolyLine绘制带宽度的线
                Vector3[] linePoints = new Vector3[m_CurrentArrowPath.Count];
                for (int i = 0; i < m_CurrentArrowPath.Count; i++)
                {
                    Vector2Int pos = m_CurrentArrowPath[i];
                    linePoints[i] = new Vector3(
                        startPos.x + pos.x * m_GridCellSize + m_GridCellSize * 0.5f,
                        startPos.y + pos.y * m_GridCellSize + m_GridCellSize * 0.5f,
                        0
                    );
                }
                Handles.DrawAAPolyLine(2f, linePoints);
            }
            
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 绘制选择框
        /// </summary>
        private void DrawSelectionRect(Rect rect)
        {
            Vector2 startPos = rect.position + m_GridOffset;
            
            Vector2Int min = new Vector2Int(
                Mathf.Min(m_SelectionStart.x, m_SelectionEnd.x),
                Mathf.Min(m_SelectionStart.y, m_SelectionEnd.y)
            );
            Vector2Int max = new Vector2Int(
                Mathf.Max(m_SelectionStart.x, m_SelectionEnd.x),
                Mathf.Max(m_SelectionStart.y, m_SelectionEnd.y)
            );
            
            Rect selectionRect = new Rect(
                startPos.x + min.x * m_GridCellSize,
                startPos.y + min.y * m_GridCellSize,
                (max.x - min.x + 1) * m_GridCellSize,
                (max.y - min.y + 1) * m_GridCellSize
            );
            
            Color borderColor = m_IsDragMode ? Color.green : Color.red;
            EditorGUI.DrawRect(selectionRect, new Color(borderColor.r, borderColor.g, borderColor.b, 0.3f));
            
            // 绘制边框
            Handles.BeginGUI();
            Handles.color = borderColor;
            Handles.DrawWireCube(selectionRect.center, selectionRect.size);
            Handles.EndGUI();
        }
        
        // ========== 辅助方法 ==========
        
        private void CreateNewLevel()
        {
            m_CurrentLevelData = new ArrowLevelData
            {
                levelName = "New Level",
                grid = new GridDef
                {
                    width = m_GridSize.x,
                    height = m_GridSize.y,
                    spacing = 1f,
                    origin = Vector3.zero
                },
                validGrids = new HashSet<Vector2Int>(),
                arrows = new List<ArrowLineDef>(),
                fillConfig = m_FillConfig,
                cameraSize = 5f,
                difficulty = 1,
                createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                modifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            m_GridValidState = new bool[m_GridSize.x, m_GridSize.y];
            m_CurrentFilePath = "";
            m_ConflictingArrows.Clear();
            
            Repaint();
        }
        
        private void ResizeGrid(Vector2Int newSize)
        {
            bool[,] newGridState = new bool[newSize.x, newSize.y];
            
            // 复制旧数据
            if (m_GridValidState != null)
            {
                int copyX = Mathf.Min(m_GridSize.x, newSize.x);
                int copyY = Mathf.Min(m_GridSize.y, newSize.y);
                for (int x = 0; x < copyX; x++)
                {
                    for (int y = 0; y < copyY; y++)
                    {
                        newGridState[x, y] = m_GridValidState[x, y];
                    }
                }
            }
            
            m_GridSize = newSize;
            m_GridValidState = newGridState;
            
            if (m_CurrentLevelData != null)
            {
                m_CurrentLevelData.grid.width = newSize.x;
                m_CurrentLevelData.grid.height = newSize.y;
                
                // 移除超出范围的有效网格
                m_CurrentLevelData.validGrids.RemoveWhere(pos => 
                    pos.x >= newSize.x || pos.y >= newSize.y);
            }
            
            Repaint();
        }
        
        private void ClearAllValidGrids()
        {
            m_CurrentLevelData.validGrids.Clear();
            m_GridValidState = new bool[m_GridSize.x, m_GridSize.y];
            Repaint();
        }
        
        private void ClearAllArrows()
        {
            m_CurrentLevelData.arrows.Clear();
            m_CurrentEditingArrow = null;
            m_ConflictingArrows.Clear();
            Repaint();
        }
        
        private bool IsGridValid(Vector2Int gridPos)
        {
            return m_CurrentLevelData.validGrids.Contains(gridPos);
        }
        
        private void SetGridValid(Vector2Int gridPos, bool valid)
        {
            if (valid)
            {
                m_CurrentLevelData.validGrids.Add(gridPos);
                if (gridPos.x >= 0 && gridPos.x < m_GridSize.x && 
                    gridPos.y >= 0 && gridPos.y < m_GridSize.y)
                {
                    m_GridValidState[gridPos.x, gridPos.y] = true;
                }
            }
            else
            {
                m_CurrentLevelData.validGrids.Remove(gridPos);
                if (gridPos.x >= 0 && gridPos.x < m_GridSize.x && 
                    gridPos.y >= 0 && gridPos.y < m_GridSize.y)
                {
                    m_GridValidState[gridPos.x, gridPos.y] = false;
                }
            }
        }
        
        private bool IsGridOccupiedByArrow(Vector2Int gridPos)
        {
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path != null && arrow.path.Contains(gridPos))
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            Vector2Int diff = b - a;
            return Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1;
        }

        private Color GetRandomColor()
        {
            return Color.white;
            // return new Color(
            //     UnityEngine.Random.Range(0.3f, 1f),
            //     UnityEngine.Random.Range(0.3f, 1f),
            //     UnityEngine.Random.Range(0.3f, 1f)
            // );
        }

        /// <summary>
        /// 绘制关卡数据快捷切换（通过关卡 ID，在默认目录下按 Level{ID}.json 加载）
        /// </summary>
        private int m_LevelIdForEditor = 1;
        private void DrawQuickLevelSwitcher()
        {
            EditorGUILayout.LabelField("关卡切换（默认目录）", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("上一关", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
            {
                m_LevelIdForEditor = Mathf.Max(1, m_LevelIdForEditor - 1);
                TryLoadLevelById(m_LevelIdForEditor);
            }

            m_LevelIdForEditor = EditorGUILayout.IntField( m_LevelIdForEditor, GUILayout.Width(60));
            if (m_LevelIdForEditor < 1) m_LevelIdForEditor = 1;

            if (GUILayout.Button("加载", EditorStyles.miniButtonMid, GUILayout.Width(60)))
            {
                TryLoadLevelById(m_LevelIdForEditor);
            }

            if (GUILayout.Button("下一关", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                m_LevelIdForEditor = Mathf.Max(1, m_LevelIdForEditor + 1);
                TryLoadLevelById(m_LevelIdForEditor);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 在默认目录下按 Level 前缀和关卡 ID 尝试加载关卡（Level{id}.json）
        /// </summary>
        private void TryLoadLevelById(int levelId)
        {
            string projectRoot = Application.dataPath.Replace("Assets", string.Empty);
            string defaultDirFullPath = System.IO.Path.Combine(projectRoot, DefaultLevelFolder);
            if (!System.IO.Directory.Exists(defaultDirFullPath))
            {
                EditorUtility.DisplayDialog("加载失败", $"默认关卡目录不存在：{DefaultLevelFolder}", "确定");
                return;
            }

            string fileName = $"{LevelFilePrefix}{levelId}.json";
            string fullPath = System.IO.Path.Combine(defaultDirFullPath, fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("加载失败", $"未找到关卡文件：{fileName}\n目录：{DefaultLevelFolder}", "确定");
                return;
            }

            LoadLevelFromPath(fullPath);
        }
        
        private void SaveLevel()
        {
            if (string.IsNullOrEmpty(m_CurrentFilePath))
            {
                SaveLevelAs();
            }
            else
            {
                ArrowLevelDataUtility.SaveToJson(m_CurrentLevelData, m_CurrentFilePath);
            }
        }
        
        private void SaveLevelAs()
        {
            string initialDirectory = Application.dataPath;
            // 将默认目录转换为绝对路径作为初始路径
            string defaultDirFullPath = System.IO.Path.Combine(Application.dataPath.Replace("Assets", string.Empty), DefaultLevelFolder);
            if (System.IO.Directory.Exists(defaultDirFullPath))
            {
                initialDirectory = defaultDirFullPath;
            }

            string defaultFileName = string.IsNullOrEmpty(m_CurrentLevelData.levelName)
                ? LevelFilePrefix
                : m_CurrentLevelData.levelName;

            string path = EditorUtility.SaveFilePanel("保存关卡", initialDirectory, defaultFileName, "json");
            if (!string.IsNullOrEmpty(path))
            {
                m_CurrentFilePath = path;
                ArrowLevelDataUtility.SaveToJson(m_CurrentLevelData, path);
            }
        }
        
        private void LoadLevel()
        {
            string initialDirectory = Application.dataPath;
            // 将默认目录转换为绝对路径作为初始路径
            string defaultDirFullPath = System.IO.Path.Combine(Application.dataPath.Replace("Assets", string.Empty), DefaultLevelFolder);
            if (System.IO.Directory.Exists(defaultDirFullPath))
            {
                initialDirectory = defaultDirFullPath;
            }

            string path = EditorUtility.OpenFilePanel("加载关卡", initialDirectory, "json");
            if (!string.IsNullOrEmpty(path))
            {
                LoadLevelFromPath(path);
            }
        }

        /// <summary>
        /// 从指定路径加载关卡数据，并刷新编辑器视图。
        /// </summary>
        private void LoadLevelFromPath(string path)
        {
            ArrowLevelData data = ArrowLevelDataUtility.LoadFromJson(path);
            if (data == null)
            {
                EditorUtility.DisplayDialog("加载失败", $"无法加载关卡数据：{path}", "确定");
                return;
            }

            m_CurrentLevelData = data;
            m_CurrentFilePath = path;
            m_GridSize = new Vector2Int(data.grid.width, data.grid.height);
            m_FillConfig = data.fillConfig ?? m_FillConfig;

            // 重建网格状态
            m_GridValidState = new bool[m_GridSize.x, m_GridSize.y];
            foreach (var pos in data.validGrids)
            {
                if (pos.x >= 0 && pos.x < m_GridSize.x &&
                    pos.y >= 0 && pos.y < m_GridSize.y)
                {
                    m_GridValidState[pos.x, pos.y] = true;
                }
            }

            m_ConflictingArrows.Clear();
            m_CurrentEditingArrow = null;
            Repaint();
        }
        
        /// <summary>
        /// 自动填充箭头：自动反复填充并做通关校验，直到生成「可全部移除」的布局或尝试次数耗尽。
        /// </summary>
        private void AutoFillArrows()
        {
            if (m_CurrentLevelData.validGrids == null || m_CurrentLevelData.validGrids.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择有效网格", "确定");
                return;
            }

            bool clearedExisting = false;
            // 是否清除现有箭头；若不清除，则仅尝试一次，避免意外覆盖手动设计的箭头
            if (m_CurrentLevelData.arrows != null && m_CurrentLevelData.arrows.Count > 0)
            {
                if (EditorUtility.DisplayDialog("自动填充", "是否清除现有箭头后再自动填充？", "是", "否"))
                {
                    ClearAllArrows();
                    clearedExisting = true;
                }
            }

            const int maxAttempts = 20;
            int attempt = 0;
            bool success = false;
            int lastArrowCount = 0;

            for (attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 每次尝试前都基于当前关卡数据重新计算可用格（支持先手动放一部分箭头再自动补充）
                HashSet<Vector2Int> availableGrids = new HashSet<Vector2Int>(m_CurrentLevelData.validGrids);
                if (m_CurrentLevelData.arrows != null)
                {
                    foreach (var arrow in m_CurrentLevelData.arrows)
                    {
                        if (arrow.path == null) continue;
                        foreach (var pos in arrow.path)
                            availableGrids.Remove(pos);
                    }
                }

                if (availableGrids.Count < m_FillConfig.minLength)
                {
                    EditorUtility.DisplayDialog("提示", $"可用网格数量不足（需要至少 {m_FillConfig.minLength} 个）", "确定");
                    return;
                }

                // 使用智能填充算法
                lastArrowCount = SmartFillArrows(availableGrids);

                // 使用与玩法一致的校验逻辑模拟一次完整通关
                var arrows = m_CurrentLevelData.arrows;
                var validGrids = m_CurrentLevelData.validGrids;
                (bool allCleared, _) = SimulateClearance(arrows, validGrids);
                if (allCleared)
                {
                    success = true;
                    break;
                }

                // 未通过校验：若之前选择清除现有箭头，则清空后再重试；否则只尝试一次，避免覆盖手动布局
                if (clearedExisting)
                {
                    ClearAllArrows();
                }
                else
                {
                    break;
                }
            }

            if (success)
            {
                string msg = clearedExisting
                    ? $"成功生成 {lastArrowCount} 个箭头，并通过通关校验（尝试 {attempt + 1} 次）。"
                    : $"自动补充 {lastArrowCount} 个箭头，并通过通关校验。";
                EditorUtility.DisplayDialog("自动填充完成", msg, "确定");
            }
            else
            {
                string msg = clearedExisting
                    ? $"在最多 {maxAttempts} 次尝试内未能生成可通关的箭头布局，请调整网格或填充参数后重试。"
                    : "当前布局在一次自动填充后仍无法通过通关校验，请手动调整或选择清除现有箭头后重试自动填充。";
                EditorUtility.DisplayDialog("自动填充失败", msg, "确定");
            }

            Repaint();
        }
        
        /// <summary>
        /// 智能填充箭头（新策略）
        /// 要求：1.保证填充的箭头可全部移除 2.尽量多利用有效格 3.允许多次转折增加解谜难度
        /// </summary>
        private int SmartFillArrows(HashSet<Vector2Int> availableGrids)
        {
            if (availableGrids.Count == 0) return 0;
            if (m_CurrentLevelData?.validGrids == null) return 0;
            var validGrids = m_CurrentLevelData.validGrids;
            HashSet<Vector2Int> usedGrids = new HashSet<Vector2Int>();
            int arrowCount = 0;

            // 填充过程缓存：已占用格、格→占用者前进方向，避免每轮重复遍历所有箭头
            var occupiedCells = new HashSet<Vector2Int>();
            var cellToOccupierDirection = new Dictionary<Vector2Int, Vector2Int>();
            RefreshFillCaches(occupiedCells, cellToOccupierDirection);

            HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(availableGrids);
            const int maxAttemptsPerPass = 6000;
            const int maxPasses = 10;
            for (int pass = 0; pass < maxPasses && remaining.Count >= m_FillConfig.minLength; pass++)
            {
                int addedThisPass = 0;
                int attempts = 0;
                while (remaining.Count >= m_FillConfig.minLength && attempts < maxAttemptsPerPass)
                {
                    attempts++;
                    List<Vector2Int> remainingList = new List<Vector2Int>(remaining);
                    Vector2Int start = remainingList[UnityEngine.Random.Range(0, remainingList.Count)];
                    List<Vector2Int> path = GenerateArrowPathEncourageTurns(start, availableGrids, usedGrids, 0.55f);
                    if (path == null || path.Count < m_FillConfig.minLength) continue;
                    Vector2Int head = path[path.Count - 1];
                    Vector2Int dir = path[path.Count - 1] - path[path.Count - 2];
                    if (!IsForwardStepUnblocked(head, dir, validGrids, cellToOccupierDirection)) continue;
                    if (!IsForwardRayClearOfPath(path, validGrids)) continue;
                    if (WouldBlockExistingArrows(path, occupiedCells) || WouldConflictWithArrowDirection(path) || WouldCreateDeadlock(path))
                        continue;
                    AddArrowFromPath(path, usedGrids);
                    UpdateFillCachesAfterAdd(path, occupiedCells, cellToOccupierDirection);
                    foreach (var p in path) remaining.Remove(p);
                    arrowCount++;
                    addedThisPass++;
                }
                if (addedThisPass == 0) break;
            }

            if (arrowCount > 0) return arrowCount;

            // 兜底：有效格过少或形状特殊时用原有难度策略
            int difficulty = m_CurrentLevelData != null ? m_CurrentLevelData.difficulty : 1;
            difficulty = Mathf.Clamp(difficulty, 1, 5);
            Vector2Int center = FindCenter(availableGrids);
            List<Vector2Int> boundaryGrids = FindBoundaryGrids(availableGrids);
            if (boundaryGrids.Count == 0)
                return RandomFillArrows(availableGrids);
            if (difficulty <= 2)
                return FillArrowsLowDifficulty(availableGrids, usedGrids, center, boundaryGrids);
            if (difficulty == 3)
                return FillArrowsMediumDifficulty(availableGrids, usedGrids, center, boundaryGrids);
            return FillArrowsHighDifficulty(availableGrids, usedGrids, center, boundaryGrids, difficulty);
        }

        private void AddArrowFromPath(List<Vector2Int> path, HashSet<Vector2Int> usedGrids)
        {
            var arrow = new ArrowLineDef
            {
                path = path,
                lineColor = GetRandomColor(),
                hitColor = Color.red,
                occupyAllNodes = true,
                startIndex = 0
            };
            m_CurrentLevelData.arrows.Add(arrow);
            foreach (var p in path) usedGrids.Add(p);
        }

        private void RefreshFillCaches(HashSet<Vector2Int> occupiedCells, Dictionary<Vector2Int, Vector2Int> cellToOccupierDirection)
        {
            occupiedCells.Clear();
            cellToOccupierDirection.Clear();
            if (m_CurrentLevelData?.arrows == null) return;
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path == null || arrow.path.Count < 2) continue;
                Vector2Int dir = arrow.path[arrow.path.Count - 1] - arrow.path[arrow.path.Count - 2];
                if (dir.x != 0) dir.x = dir.x > 0 ? 1 : -1;
                if (dir.y != 0) dir.y = dir.y > 0 ? 1 : -1;
                foreach (var p in arrow.path)
                {
                    occupiedCells.Add(p);
                    cellToOccupierDirection[p] = dir;
                }
            }
        }

        private void UpdateFillCachesAfterAdd(List<Vector2Int> path, HashSet<Vector2Int> occupiedCells, Dictionary<Vector2Int, Vector2Int> cellToOccupierDirection)
        {
            if (path == null || path.Count < 2) return;
            Vector2Int dir = path[path.Count - 1] - path[path.Count - 2];
            if (dir.x != 0) dir.x = dir.x > 0 ? 1 : -1;
            if (dir.y != 0) dir.y = dir.y > 0 ? 1 : -1;
            foreach (var p in path)
            {
                occupiedCells.Add(p);
                cellToOccupierDirection[p] = dir;
            }
        }
        
        /// <summary>
        /// 低难度填充（多解策略）
        /// 策略：创建独立箭头，减少依赖关系，允许更多箭头共存
        /// </summary>
        private int FillArrowsLowDifficulty(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids,
            Vector2Int center, List<Vector2Int> boundaryGrids)
        {
            int arrowCount = 0;
            
            // 优先填充：从中心到边界的主要路径（独立路径）
            List<Vector2Int> directions = new List<Vector2Int>
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };
            
            // 打乱方向顺序，增加随机性
            for (int i = 0; i < directions.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, directions.Count);
                var temp = directions[i];
                directions[i] = directions[j];
                directions[j] = temp;
            }
            
            // 从中心向各个方向填充独立路径
            foreach (var direction in directions)
            {
                List<Vector2Int> path = FindPathToBoundary(center, direction, availableGrids, usedGrids);
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    Vector2Int head = path[path.Count - 1], dir = path[path.Count - 1] - path[path.Count - 2];
                    var vg = m_CurrentLevelData?.validGrids;
                    if (vg != null && (!IsForwardStepUnblocked(head, dir, vg) || !IsForwardRayClearOfPath(path, vg)))
                        continue;
                    // 低难度：宽松的冲突检测，只检测真正的矛盾相交
                    if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            usedGrids.Add(pos);
                        }
                    }
                }
            }
            
            // 填充剩余网格：优先创建独立箭头
            arrowCount += FillRemainingGridsLowDifficulty(availableGrids, usedGrids);
            
            return arrowCount;
        }
        
        /// <summary>
        /// 中难度填充（平衡策略）
        /// </summary>
        private int FillArrowsMediumDifficulty(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids,
            Vector2Int center, List<Vector2Int> boundaryGrids)
        {
            int arrowCount = 0;
            
            // 先填充主路径
            List<Vector2Int> directions = new List<Vector2Int>
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };
            
            foreach (var direction in directions)
            {
                List<Vector2Int> path = FindPathToBoundary(center, direction, availableGrids, usedGrids);
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    Vector2Int h = path[path.Count - 1], d = path[path.Count - 1] - path[path.Count - 2];
                    var vg = m_CurrentLevelData?.validGrids;
                    if (vg != null && (!IsForwardStepUnblocked(h, d, vg) || !IsForwardRayClearOfPath(path, vg)))
                        continue;
                    if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            usedGrids.Add(pos);
                        }
                    }
                }
            }
            
            // 填充剩余网格：混合策略
            arrowCount += FillRemainingGridsSmart(availableGrids, usedGrids);
            
            return arrowCount;
        }
        
        /// <summary>
        /// 高难度填充（少解策略）
        /// 策略：创建依赖链，形成严格的移动顺序
        /// 优先尝试创建唯一解，如果无法实现则放宽条件允许多个解
        /// 所有箭头必须沿网格方向（无斜线）
        /// </summary>
        private int FillArrowsHighDifficulty(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids,
            Vector2Int center, List<Vector2Int> boundaryGrids, int difficulty)
        {
            int arrowCount = 0;
            
            // 高难度：创建依赖链
            // 策略：先填充"被依赖"的箭头（依赖链的末端），再填充"依赖"的箭头（依赖链的前端）
            
            // 1. 先填充边界箭头（这些箭头不依赖其他箭头，可以最先移动）
            // 高难度时，边界箭头数量要少，形成瓶颈
            List<Vector2Int> boundaryStartPoints = new List<Vector2Int>(boundaryGrids);
            
            // 根据难度调整边界箭头数量：难度越高，边界箭头越少（形成更严格的依赖）
            int boundaryArrowCount = difficulty == 5 ? Mathf.Min(boundaryStartPoints.Count / 3, 2) : 
                                    Mathf.Min(boundaryStartPoints.Count / 2, 3);
            
            for (int i = 0; i < boundaryArrowCount && boundaryStartPoints.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, boundaryStartPoints.Count);
                Vector2Int startPos = boundaryStartPoints[index];
                boundaryStartPoints.RemoveAt(index);
                
                // 从边界向中心填充短路径（这些箭头可以最先移动）
                // 确保路径只沿网格方向
                List<Vector2Int> path = GenerateArrowPathFromBoundary(startPos, availableGrids, usedGrids);
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    Vector2Int h = path[path.Count - 1], d = path[path.Count - 1] - path[path.Count - 2];
                    var vg = m_CurrentLevelData?.validGrids;
                    if (vg != null && (!IsForwardStepUnblocked(h, d, vg) || !IsForwardRayClearOfPath(path, vg)))
                        continue;
                    // 验证路径只使用网格方向
                    if (IsPathGridAligned(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            usedGrids.Add(pos);
                        }
                    }
                }
            }
            
            // 2. 填充依赖箭头（这些箭头的前进路径被已填充的箭头占用）
            // 根据难度决定依赖链的长度和复杂度
            int dependencyLevels = difficulty == 5 ? 3 : 2; // 难度5创建3层依赖，难度4创建2层依赖
            
            for (int level = 0; level < dependencyLevels; level++)
            {
                int levelArrowCount = FillDependencyLevel(availableGrids, usedGrids, level, difficulty);
                arrowCount += levelArrowCount;
            }
            
            // 3. 尝试填充剩余网格，优先创建强依赖关系
            int remainingCount = FillRemainingGridsWithDependency(availableGrids, usedGrids, difficulty);
            
            // 如果剩余网格填充失败或填充数量很少，尝试放宽条件
            // 允许创建一些独立箭头，但优先创建依赖箭头
            if (remainingCount == 0 && availableGrids.Count - usedGrids.Count >= m_FillConfig.minLength)
            {
                // 放宽条件：允许创建一些独立箭头（但仍优先依赖）
                remainingCount = FillRemainingGridsWithRelaxedDependency(availableGrids, usedGrids, difficulty);
            }
            
            arrowCount += remainingCount;
            
            return arrowCount;
        }
        
        /// <summary>
        /// 验证路径是否只使用网格方向（无斜线）
        /// </summary>
        private bool IsPathGridAligned(List<Vector2Int> path)
        {
            if (path == null || path.Count < 2) return true;
            
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int dir = path[i] - path[i - 1];
                
                // 检查方向是否是网格方向（只允许up/down/left/right）
                if (dir != Vector2Int.up && dir != Vector2Int.down && 
                    dir != Vector2Int.left && dir != Vector2Int.right)
                {
                    return false; // 发现斜线方向
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 放宽条件的剩余网格填充（高难度时如果无法创建强依赖，允许创建一些独立箭头）
        /// </summary>
        private int FillRemainingGridsWithRelaxedDependency(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids, int difficulty)
        {
            int arrowCount = 0;
            int maxAttempts = 200; // 减少尝试次数
            int attempts = 0;
            
            HashSet<Vector2Int> remainingGrids = new HashSet<Vector2Int>(availableGrids);
            foreach (var used in usedGrids)
            {
                remainingGrids.Remove(used);
            }
            
            while (remainingGrids.Count >= m_FillConfig.minLength && attempts < maxAttempts)
            {
                attempts++;
                
                List<Vector2Int> remainingList = new List<Vector2Int>(remainingGrids);
                Vector2Int startPos = remainingList[UnityEngine.Random.Range(0, remainingList.Count)];
                
                // 尝试创建路径，优先依赖，但允许独立
                List<Vector2Int> path = GenerateArrowPathSafe(startPos, remainingGrids, usedGrids);
                
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    Vector2Int h = path[path.Count - 1], d = path[path.Count - 1] - path[path.Count - 2];
                    var vg = m_CurrentLevelData?.validGrids;
                    if (vg != null && (!IsForwardStepUnblocked(h, d, vg) || !IsForwardRayClearOfPath(path, vg)))
                        continue;
                    // 验证路径只使用网格方向
                    if (!IsPathGridAligned(path))
                    {
                        continue; // 跳过斜线路径
                    }
                    
                    // 放宽冲突检测：只检测严重的冲突
                    if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            remainingGrids.Remove(pos);
                            usedGrids.Add(pos);
                        }
                    }
                }
            }
            
            return arrowCount;
        }
        
        /// <summary>
        /// 从边界生成箭头路径（向中心方向）
        /// 确保只使用网格方向（无斜线）
        /// </summary>
        private List<Vector2Int> GenerateArrowPathFromBoundary(Vector2Int startPos, HashSet<Vector2Int> availableGrids, 
            HashSet<Vector2Int> usedGrids)
        {
            if (usedGrids.Contains(startPos)) return null;
            
            // 计算从边界指向中心的方向（只使用网格方向，不允许斜线）
            Vector2Int center = FindCenter(availableGrids);
            Vector2Int directionToCenter = Vector2Int.zero;
            
            // 优先选择水平或垂直方向中更接近中心的方向
            int dx = center.x - startPos.x;
            int dy = center.y - startPos.y;
            
            // 选择距离中心更近的方向（只使用网格方向）
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                // 水平方向更接近
                directionToCenter = dx > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else if (dy != 0)
            {
                // 垂直方向更接近
                directionToCenter = dy > 0 ? Vector2Int.up : Vector2Int.down;
            }
            else
            {
                // 如果已经在中心，随机选择一个网格方向
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                directionToCenter = dirs[UnityEngine.Random.Range(0, dirs.Length)];
            }
            
            // 确保方向是有效的网格方向
            if (directionToCenter == Vector2Int.zero)
            {
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                directionToCenter = dirs[UnityEngine.Random.Range(0, dirs.Length)];
            }
            
            List<Vector2Int> path = new List<Vector2Int> { startPos };
            Vector2Int currentPos = startPos;
            Vector2Int lastDirection = directionToCenter;
            
            int targetLength = UnityEngine.Random.Range(m_FillConfig.minLength, Mathf.Min(m_FillConfig.maxLength, 6)); // 边界箭头较短
            
            for (int i = 1; i < targetLength; i++)
            {
                List<Vector2Int> candidates = GetAvailableNeighborsForPath(currentPos, availableGrids, path, usedGrids, lastDirection);
                
                if (candidates.Count == 0) break;
                
                Vector2Int nextPos;
                Vector2Int preferredPos = currentPos + lastDirection;
                if (candidates.Contains(preferredPos))
                {
                    nextPos = preferredPos;
                }
                else if (m_FillConfig.allowTurn && candidates.Count > 0)
                {
                    nextPos = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                }
                else
                {
                    if (candidates.Count > 0)
                    {
                        nextPos = candidates[0];
                    }
                    else
                    {
                        break;
                    }
                }
                
                lastDirection = nextPos - currentPos;
                path.Add(nextPos);
                currentPos = nextPos;
            }
            
            return path.Count >= m_FillConfig.minLength ? path : null;
        }
        
        /// <summary>
        /// 填充依赖层级（高难度）
        /// 创建依赖链：后填充的箭头依赖先填充的箭头
        /// 确保所有路径只使用网格方向（无斜线）
        /// </summary>
        private int FillDependencyLevel(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids, 
            int level, int difficulty)
        {
            int arrowCount = 0;
            int maxAttempts = 300;
            int attempts = 0;
            
            // 获取未使用的网格
            HashSet<Vector2Int> remainingGrids = new HashSet<Vector2Int>(availableGrids);
            foreach (var used in usedGrids)
            {
                remainingGrids.Remove(used);
            }
            
            // 计算已有箭头的方向分布
            Dictionary<Vector2Int, int> directionCount = new Dictionary<Vector2Int, int>();
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path == null || arrow.path.Count < 2) continue;
                Vector2Int dir = arrow.path[arrow.path.Count - 1] - arrow.path[arrow.path.Count - 2];
                if (dir != Vector2Int.zero)
                {
                    directionCount[dir] = directionCount.GetValueOrDefault(dir, 0) + 1;
                }
            }
            
            while (remainingGrids.Count >= m_FillConfig.minLength && attempts < maxAttempts)
            {
                attempts++;
                
                // 随机选择起始点
                List<Vector2Int> remainingList = new List<Vector2Int>(remainingGrids);
                Vector2Int startPos = remainingList[UnityEngine.Random.Range(0, remainingList.Count)];
                
                // 根据层级决定依赖程度
                // level 0: 轻度依赖（可以依赖已有箭头）
                // level 1+: 强依赖（必须依赖已有箭头）
                List<Vector2Int> path = null;
                
                if (level == 0)
                {
                    // 第一层：可以创建独立箭头或轻度依赖
                    path = GenerateArrowPathSafe(startPos, remainingGrids, usedGrids);
                }
                else
                {
                    // 后续层级：创建强依赖箭头
                    // 优先选择会依赖已有箭头的方向
                    path = GenerateArrowPathWithDependency(startPos, remainingGrids, usedGrids, directionCount);
                }
                
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    Vector2Int h = path[path.Count - 1], d = path[path.Count - 1] - path[path.Count - 2];
                    var vg = m_CurrentLevelData?.validGrids;
                    if (vg != null && (!IsForwardStepUnblocked(h, d, vg) || !IsForwardRayClearOfPath(path, vg)))
                        continue;
                    // 验证路径只使用网格方向（无斜线）
                    if (!IsPathGridAligned(path))
                    {
                        continue; // 跳过斜线路径
                    }
                    
                    // 检查冲突（高难度时严格检测）
                    if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            remainingGrids.Remove(pos);
                            usedGrids.Add(pos);
                        }
                        
                        // 更新方向统计
                        Vector2Int dir = path[path.Count - 1] - path[path.Count - 2];
                        if (dir != Vector2Int.zero)
                        {
                            directionCount[dir] = directionCount.GetValueOrDefault(dir, 0) + 1;
                        }
                    }
                }
            }
            
            return arrowCount;
        }
        
        /// <summary>
        /// 生成带依赖的箭头路径（高难度）
        /// 优先创建依赖已有箭头的路径
        /// </summary>
        private List<Vector2Int> GenerateArrowPathWithDependency(Vector2Int startPos, HashSet<Vector2Int> availableGrids,
            HashSet<Vector2Int> usedGrids, Dictionary<Vector2Int, int> directionCount)
        {
            if (usedGrids.Contains(startPos)) return null;
            
            // 分析已有箭头的前进路径，找到可以依赖的方向
            List<Vector2Int> preferredDirections = new List<Vector2Int>();
            
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path == null || arrow.path.Count < 2) continue;
                
                Vector2Int existingDirection = arrow.path[arrow.path.Count - 1] - arrow.path[arrow.path.Count - 2];
                Vector2Int existingHeadPos = arrow.path[arrow.path.Count - 1];
                
                if (existingDirection == Vector2Int.zero) continue;
                
                // 计算已有箭头的前进路径
                List<Vector2Int> forwardPath = CalculateForwardPath(existingHeadPos, existingDirection, m_CurrentLevelData.validGrids);
                
                // 检查新箭头是否可以依赖这个已有箭头
                // 如果新箭头的前进方向与已有箭头相反，且新箭头的前进路径会被已有箭头占用
                Vector2Int oppositeDirection = -existingDirection;
                
                // 检查新起点是否在已有箭头前进路径的"后方"（可以依赖）
                bool canDepend = false;
                foreach (var forwardPos in forwardPath)
                {
                    // 检查新起点是否在已有箭头前进路径附近
                    if (Vector2Int.Distance(startPos, forwardPos) <= 2)
                    {
                        canDepend = true;
                        break;
                    }
                }
                
                if (canDepend && !preferredDirections.Contains(oppositeDirection))
                {
                    preferredDirections.Add(oppositeDirection);
                }
            }
            
            // 如果有可依赖的方向，优先使用
            Vector2Int targetDirection = Vector2Int.zero;
            if (preferredDirections.Count > 0)
            {
                targetDirection = preferredDirections[UnityEngine.Random.Range(0, preferredDirections.Count)];
            }
            else
            {
                // 否则随机选择方向
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                targetDirection = dirs[UnityEngine.Random.Range(0, dirs.Length)];
            }
            
            return GenerateArrowPathWithDirection(startPos, availableGrids, usedGrids, targetDirection);
        }
        
        /// <summary>
        /// 低难度填充剩余网格（创建独立箭头）
        /// </summary>
        private int FillRemainingGridsLowDifficulty(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids)
        {
            int arrowCount = 0;
            int maxAttempts = 800; // 低难度允许更多尝试
            int attempts = 0;
            
            HashSet<Vector2Int> remainingGrids = new HashSet<Vector2Int>(availableGrids);
            foreach (var used in usedGrids)
            {
                remainingGrids.Remove(used);
            }
            
            while (remainingGrids.Count >= m_FillConfig.minLength && attempts < maxAttempts)
            {
                attempts++;
                
                List<Vector2Int> remainingList = new List<Vector2Int>(remainingGrids);
                Vector2Int startPos = remainingList[UnityEngine.Random.Range(0, remainingList.Count)];
                
                List<Vector2Int> path = GenerateArrowPathSafe(startPos, remainingGrids, usedGrids);
                
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    // 低难度：只检测真正的矛盾相交
                    if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            remainingGrids.Remove(pos);
                            usedGrids.Add(pos);
                        }
                    }
                }
            }
            
            return arrowCount;
        }
        
        /// <summary>
        /// 高难度填充剩余网格（创建依赖箭头）
        /// 确保所有路径只使用网格方向（无斜线）
        /// </summary>
        private int FillRemainingGridsWithDependency(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids, int difficulty)
        {
            int arrowCount = 0;
            int maxAttempts = 400; // 高难度减少尝试次数
            int attempts = 0;
            
            HashSet<Vector2Int> remainingGrids = new HashSet<Vector2Int>(availableGrids);
            foreach (var used in usedGrids)
            {
                remainingGrids.Remove(used);
            }
            
            // 计算已有箭头的方向分布
            Dictionary<Vector2Int, int> directionCount = new Dictionary<Vector2Int, int>();
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path == null || arrow.path.Count < 2) continue;
                Vector2Int dir = arrow.path[arrow.path.Count - 1] - arrow.path[arrow.path.Count - 2];
                if (dir != Vector2Int.zero)
                {
                    directionCount[dir] = directionCount.GetValueOrDefault(dir, 0) + 1;
                }
            }
            
            while (remainingGrids.Count >= m_FillConfig.minLength && attempts < maxAttempts)
            {
                attempts++;
                
                List<Vector2Int> remainingList = new List<Vector2Int>(remainingGrids);
                Vector2Int startPos = remainingList[UnityEngine.Random.Range(0, remainingList.Count)];
                
                // 高难度：优先创建依赖箭头
                List<Vector2Int> path = GenerateArrowPathWithDependency(startPos, remainingGrids, usedGrids, directionCount);
                
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    // 验证路径只使用网格方向（无斜线）
                    if (!IsPathGridAligned(path))
                    {
                        continue; // 跳过斜线路径
                    }
                    
                    // 高难度：严格检测
                    if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            remainingGrids.Remove(pos);
                            usedGrids.Add(pos);
                        }
                        
                        // 更新方向统计
                        Vector2Int dir = path[path.Count - 1] - path[path.Count - 2];
                        if (dir != Vector2Int.zero)
                        {
                            directionCount[dir] = directionCount.GetValueOrDefault(dir, 0) + 1;
                        }
                    }
                }
            }
            
            return arrowCount;
        }
        
        /// <summary>
        /// 随机填充箭头（回退方案）
        /// </summary>
        private int RandomFillArrows(HashSet<Vector2Int> availableGrids)
        {
            int arrowCount = 0;
            int maxAttempts = 1000;
            int attempts = 0;
            
            while (availableGrids.Count >= m_FillConfig.minLength && attempts < maxAttempts)
            {
                attempts++;
                
                List<Vector2Int> availableList = new List<Vector2Int>(availableGrids);
                Vector2Int startPos = availableList[UnityEngine.Random.Range(0, availableList.Count)];
                
                List<Vector2Int> path = GenerateArrowPath(startPos, availableGrids);
                
                if (path != null && path.Count >= m_FillConfig.minLength)
                {
                    ArrowLineDef arrow = new ArrowLineDef
                    {
                        path = path,
                        lineColor = GetRandomColor(),
                        hitColor = Color.red,
                        occupyAllNodes = true,
                        startIndex = 0
                    };
                    
                    m_CurrentLevelData.arrows.Add(arrow);
                    arrowCount++;
                    
                    foreach (var pos in path)
                    {
                        availableGrids.Remove(pos);
                    }
                }
            }
            
            return arrowCount;
        }
        
        /// <summary>
        /// 找到有效网格的中心点
        /// </summary>
        private Vector2Int FindCenter(HashSet<Vector2Int> grids)
        {
            if (grids.Count == 0) return Vector2Int.zero;
            
            int sumX = 0, sumY = 0;
            foreach (var pos in grids)
            {
                sumX += pos.x;
                sumY += pos.y;
            }
            
            return new Vector2Int(sumX / grids.Count, sumY / grids.Count);
        }
        
        /// <summary>
        /// 根据有效格计算网格边界（XY 的最小最大值），用于不规则有效格时的边界判定。
        /// 有效格可能同行/同列有镂空，边界应以整块有效格的包围盒为准。结果会缓存，同一 validGrids 引用时直接返回缓存。
        /// </summary>
        private bool GetValidGridBounds(HashSet<Vector2Int> validGrids, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = maxX = minY = maxY = 0;
            if (validGrids == null || validGrids.Count == 0) return false;
            if (validGrids == m_CachedBoundsValidGridsRef && m_CachedBoundsValid)
            {
                minX = m_CachedMinX; maxX = m_CachedMaxX; minY = m_CachedMinY; maxY = m_CachedMaxY;
                return true;
            }
            bool first = true;
            foreach (var p in validGrids)
            {
                if (first)
                {
                    minX = maxX = p.x;
                    minY = maxY = p.y;
                    first = false;
                }
                else
                {
                    if (p.x < minX) minX = p.x;
                    if (p.x > maxX) maxX = p.x;
                    if (p.y < minY) minY = p.y;
                    if (p.y > maxY) maxY = p.y;
                }
            }
            m_CachedBoundsValidGridsRef = validGrids;
            m_CachedMinX = minX; m_CachedMaxX = maxX; m_CachedMinY = minY; m_CachedMaxY = maxY;
            m_CachedBoundsValid = true;
            return true;
        }
        
        /// <summary>
        /// 与玩法一致：从 head 沿 direction 的前进射线上的所有格（到包围盒外或镂空为止）。
        /// direction 会归一化为 ±1，用于填充与阻挡判定。
        /// </summary>
        private List<Vector2Int> GetForwardRayCells(Vector2Int head, Vector2Int direction, HashSet<Vector2Int> validGrids)
        {
            var ray = new List<Vector2Int>();
            if (direction == Vector2Int.zero) return ray;
            if (validGrids == null || !GetValidGridBounds(validGrids, out int minX, out int maxX, out int minY, out int maxY))
                return ray;
            Vector2Int step = direction;
            if (step.x != 0) step.x = step.x > 0 ? 1 : -1;
            if (step.y != 0) step.y = step.y > 0 ? 1 : -1;
            for (int k = 1; k < 200; k++)
            {
                Vector2Int nextGrid = head + step * k;
                if (nextGrid.x < minX || nextGrid.x > maxX || nextGrid.y < minY || nextGrid.y > maxY)
                    break;
                if (!validGrids.Contains(nextGrid))
                    break;
                ray.Add(nextGrid);
            }
            return ray;
        }

        /// <summary>
        /// 判断箭头在 headPos 沿 direction 是否无阻挡（有解）。
        /// 前进射线上若有其他箭头占用格，仅当该箭头前进单位向量与当前方向冲突（一上一下或一左一右）时才判定为阻挡；
        /// 不冲突时当前点到该占用格之间的射线段视为无阻挡。
        /// </summary>
        /// <param name="cellToOccupierDirection">可选；填充时传入已维护的 格→占用者前进方向，避免重复遍历所有箭头</param>
        private bool IsForwardStepUnblocked(Vector2Int headPos, Vector2Int direction, HashSet<Vector2Int> validGrids, Dictionary<Vector2Int, Vector2Int> cellToOccupierDirection = null)
        {
            if (direction == Vector2Int.zero) return false;
            if (cellToOccupierDirection == null)
            {
                cellToOccupierDirection = new Dictionary<Vector2Int, Vector2Int>();
                if (m_CurrentLevelData?.arrows != null)
                {
                    foreach (var arrow in m_CurrentLevelData.arrows)
                    {
                        if (arrow.path == null || arrow.path.Count < 2) continue;
                        Vector2Int dir = arrow.path[arrow.path.Count - 1] - arrow.path[arrow.path.Count - 2];
                        if (dir.x != 0) dir.x = dir.x > 0 ? 1 : -1;
                        if (dir.y != 0) dir.y = dir.y > 0 ? 1 : -1;
                        foreach (var p in arrow.path)
                            cellToOccupierDirection[p] = dir;
                    }
                }
            }
            Vector2Int ourStep = direction;
            if (ourStep.x != 0) ourStep.x = ourStep.x > 0 ? 1 : -1;
            if (ourStep.y != 0) ourStep.y = ourStep.y > 0 ? 1 : -1;
            var ray = GetForwardRayCells(headPos, direction, validGrids);
            foreach (var cell in ray)
            {
                if (!cellToOccupierDirection.TryGetValue(cell, out Vector2Int occupierDir)) continue;
                if (occupierDir == -ourStep) return false;
            }
            return true;
        }

        /// <summary>
        /// 判断该箭头的「前进射线」是否与自身路径相交。若射线经过的任一格在 path 上（含尾部），则箭头会被自身阻挡，无解。
        /// path 顺序为 [tail, ..., head]，射线为 head 沿 (path末段方向) 延伸，不包含 head。
        /// </summary>
        private bool IsForwardRayClearOfPath(List<Vector2Int> path, HashSet<Vector2Int> validGrids)
        {
            if (path == null || path.Count < 2 || validGrids == null) return false;
            if (!GetHeadDirectionNextStep(path, out Vector2Int head, out Vector2Int dir, out _) || dir == Vector2Int.zero)
                return true;
            var pathSet = new HashSet<Vector2Int>(path);
            var ray = GetForwardRayCells(head, dir, validGrids);
            foreach (var cell in ray)
            {
                if (pathSet.Contains(cell))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 若将路径延伸一步到 nextPos，新头部的「前进射线」是否会与当前路径相交（自身阻挡）。用于生成时优先排除此类延伸。
        /// </summary>
        private bool WouldForwardRayHitPathIfExtended(List<Vector2Int> currentPath, Vector2Int nextPos)
        {
            if (currentPath == null || currentPath.Count == 0) return false;
            var validGrids = m_CurrentLevelData?.validGrids;
            if (validGrids == null) return false;
            Vector2Int dir = nextPos - currentPath[currentPath.Count - 1];
            if (dir == Vector2Int.zero) return false;
            var pathSet = new HashSet<Vector2Int>(currentPath);
            var ray = GetForwardRayCells(nextPos, dir, validGrids);
            foreach (var cell in ray)
            {
                if (pathSet.Contains(cell))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// 从头部沿反方向补全路径（尾部），允许拐弯以充分利用有效格。路径不占用 usedGrids 且不与已有箭头路径重叠。
        /// 返回 path 顺序为 [tail, ..., head]，若无法满足最小长度则返回 null。
        /// </summary>
        private List<Vector2Int> GenerateArrowPathBackwardFromHead(Vector2Int headPos, Vector2Int forwardDirection,
            HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids)
        {
            if (forwardDirection == Vector2Int.zero) return null;
            Vector2Int backDir = -forwardDirection;
            Vector2Int firstTail = headPos + backDir;
            if (!availableGrids.Contains(firstTail) || usedGrids.Contains(firstTail))
                return null;
            if (IsCellOnAnyExistingArrowPath(firstTail))
                return null;
            List<Vector2Int> path = new List<Vector2Int> { firstTail, headPos };
            int targetLength = UnityEngine.Random.Range(m_FillConfig.minLength, m_FillConfig.maxLength + 1);
            Vector2Int preferredBackDir = backDir;

            while (path.Count < targetLength)
            {
                Vector2Int tail = path[0];
                List<Vector2Int> candidates = GetNeighborsForBackwardPath(tail, availableGrids, path, usedGrids);
                if (candidates.Count == 0) break;
                Vector2Int nextTail;
                Vector2Int straightPos = tail + preferredBackDir;
                if (candidates.Contains(straightPos))
                    nextTail = straightPos;
                else if (m_FillConfig.allowTurn && candidates.Count > 0)
                    nextTail = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                else
                    nextTail = candidates[0];
                preferredBackDir = nextTail - tail;
                path.Insert(0, nextTail);
            }
            if (path.Count < m_FillConfig.minLength) return null;
            return path;
        }

        private bool IsCellOnAnyExistingArrowPath(Vector2Int cell)
        {
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path != null && arrow.path.Contains(cell))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 用于从头部向后生长路径时，获取可选的下一格（四邻，且在 availableGrids、不在 path/usedGrids、不在已有箭头路径上）。
        /// </summary>
        private List<Vector2Int> GetNeighborsForBackwardPath(Vector2Int pos, HashSet<Vector2Int> availableGrids,
            List<Vector2Int> currentPath, HashSet<Vector2Int> usedGrids)
        {
            var neighbors = new List<Vector2Int>();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            foreach (var d in dirs)
            {
                Vector2Int n = pos + d;
                if (!availableGrids.Contains(n) || usedGrids.Contains(n) || currentPath.Contains(n))
                    continue;
                if (IsCellOnAnyExistingArrowPath(n)) continue;
                neighbors.Add(n);
            }
            return neighbors;
        }
        
        /// <summary>
        /// 按到中心点的距离升序返回网格列表（用于从中心向外填充）。
        /// </summary>
        private List<Vector2Int> GetCellsOrderedByDistanceFromCenter(HashSet<Vector2Int> grids, Vector2Int center)
        {
            List<Vector2Int> list = new List<Vector2Int>(grids);
            list.Sort((a, b) =>
            {
                int da = (a.x - center.x) * (a.x - center.x) + (a.y - center.y) * (a.y - center.y);
                int db = (b.x - center.x) * (b.x - center.x) + (b.y - center.y) * (b.y - center.y);
                return da.CompareTo(db);
            });
            return list;
        }
        
        /// <summary>
        /// 找到所有边界网格（可能的出口）。边界以有效格 XY 的最小最大值确定，同一行/列中的镂空仍视为同一行/列。
        /// </summary>
        private List<Vector2Int> FindBoundaryGrids(HashSet<Vector2Int> grids)
        {
            List<Vector2Int> boundaryGrids = new List<Vector2Int>();
            if (!GetValidGridBounds(grids, out int minX, out int maxX, out int minY, out int maxY))
                return boundaryGrids;
            
            foreach (var pos in grids)
            {
                // 边界：位于有效格包围盒的边线上（x 或 y 达到最小/最大）
                if (pos.x == minX || pos.x == maxX || pos.y == minY || pos.y == maxY)
                    boundaryGrids.Add(pos);
            }
            
            return boundaryGrids;
        }
        
        /// <summary>
        /// 从起点向指定方向找到一条到达边界的路径
        /// </summary>
        private List<Vector2Int> FindPathToBoundary(Vector2Int start, Vector2Int preferredDirection, 
            HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids)
        {
            // 如果起点已被占用，返回null
            if (usedGrids.Contains(start))
            {
                return null;
            }
            
            List<Vector2Int> path = new List<Vector2Int> { start };
            Vector2Int currentPos = start;
            Vector2Int lastDirection = preferredDirection;
            
            int targetLength = UnityEngine.Random.Range(m_FillConfig.minLength, m_FillConfig.maxLength + 1);
            int maxLength = Mathf.Min(targetLength, m_FillConfig.maxLength);
            
            for (int i = 1; i < maxLength; i++)
            {
                // 获取可用的相邻网格（排除已使用的）
                List<Vector2Int> candidates = GetAvailableNeighborsForPath(currentPos, availableGrids, path, usedGrids, lastDirection);
                
                if (candidates.Count == 0)
                {
                    break;
                }
                
                // 优先选择preferredDirection方向
                Vector2Int nextPos;
                Vector2Int preferredPos = currentPos + lastDirection;
                
                if (candidates.Contains(preferredPos))
                {
                    nextPos = preferredPos;
                }
                else if (m_FillConfig.allowTurn && candidates.Count > 0)
                {
                    // 允许拐弯，选择最接近preferredDirection的方向
                    nextPos = SelectBestDirection(currentPos, candidates, preferredDirection);
                }
                else
                {
                    nextPos = candidates[0];
                }
                
                // 检查是否到达边界
                if (IsBoundaryGrid(nextPos, availableGrids))
                {
                    path.Add(nextPos);
                    break;
                }
                
                // 更新方向
                lastDirection = nextPos - currentPos;
                path.Add(nextPos);
                currentPos = nextPos;
            }
            
            if (path.Count < m_FillConfig.minLength)
            {
                return null;
            }
            
            return path;
        }
        
        /// <summary>
        /// 验证路径是否有效（不共用网格点）
        /// </summary>
        private bool IsValidPath(List<Vector2Int> path, HashSet<Vector2Int> usedGrids)
        {
            if (path == null || path.Count < m_FillConfig.minLength)
            {
                return false;
            }
            
            // 检查是否与已使用的网格点重叠
            foreach (var pos in path)
            {
                if (usedGrids.Contains(pos))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取可用于路径的相邻网格（排除已使用的，但不排除当前路径中的点）
        /// </summary>
        private List<Vector2Int> GetAvailableNeighborsForPath(Vector2Int pos, HashSet<Vector2Int> availableGrids,
            List<Vector2Int> currentPath, HashSet<Vector2Int> usedGrids, Vector2Int preferredDirection)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            
            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };
            
            // 优先检查preferredDirection
            if (preferredDirection != Vector2Int.zero)
            {
                Vector2Int preferredPos = pos + preferredDirection;
                // 排除已使用的网格，但不排除当前路径中的点（除了最后一个点，因为那是当前位置）
                if (availableGrids.Contains(preferredPos) && 
                    !usedGrids.Contains(preferredPos) &&
                    !currentPath.Contains(preferredPos)) // 当前路径中的点也不能重复使用
                {
                    neighbors.Add(preferredPos);
                }
            }
            
            // 检查其他方向
            foreach (var dir in directions)
            {
                if (dir == preferredDirection) continue;
                
                Vector2Int neighborPos = pos + dir;
                if (availableGrids.Contains(neighborPos) && 
                    !usedGrids.Contains(neighborPos) &&
                    !currentPath.Contains(neighborPos)) // 当前路径中的点也不能重复使用
                {
                    neighbors.Add(neighborPos);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// 选择最接近preferredDirection的方向
        /// </summary>
        private Vector2Int SelectBestDirection(Vector2Int currentPos, List<Vector2Int> candidates, Vector2Int preferredDirection)
        {
            if (candidates.Count == 0) return currentPos;
            if (candidates.Count == 1) return candidates[0];
            
            // 计算每个候选方向与preferredDirection的点积（相似度）
            float maxDot = float.MinValue;
            Vector2Int bestPos = candidates[0];
            
            foreach (var candidate in candidates)
            {
                Vector2Int dir = candidate - currentPos;
                float dot = Vector2.Dot(new Vector2(dir.x, dir.y), new Vector2(preferredDirection.x, preferredDirection.y));
                
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestPos = candidate;
                }
            }
            
            return bestPos;
        }
        
        /// <summary>
        /// 检查是否是边界网格。以有效格 XY 的最小最大值确定的包围盒为准，位于边线上即为边界。
        /// </summary>
        private bool IsBoundaryGrid(Vector2Int pos, HashSet<Vector2Int> grids)
        {
            if (grids == null || grids.Count == 0) return false;
            if (!GetValidGridBounds(grids, out int minX, out int maxX, out int minY, out int maxY))
                return false;
            return pos.x == minX || pos.x == maxX || pos.y == minY || pos.y == maxY;
        }
        
        /// <summary>
        /// 智能填充剩余的网格（优化策略：优先填充与已有箭头同方向的路径）
        /// </summary>
        private int FillRemainingGridsSmart(HashSet<Vector2Int> availableGrids, HashSet<Vector2Int> usedGrids)
        {
            int arrowCount = 0;
            int maxAttempts = 1000; // 增加尝试次数
            int attempts = 0;
            
            // 获取未使用的网格
            HashSet<Vector2Int> remainingGrids = new HashSet<Vector2Int>(availableGrids);
            foreach (var used in usedGrids)
            {
                remainingGrids.Remove(used);
            }
            
            // 统计已有箭头的方向分布
            Dictionary<Vector2Int, int> directionCount = new Dictionary<Vector2Int, int>();
            foreach (var arrow in m_CurrentLevelData.arrows)
            {
                if (arrow.path == null || arrow.path.Count < 2) continue;
                Vector2Int dir = arrow.path[arrow.path.Count - 1] - arrow.path[arrow.path.Count - 2];
                if (dir != Vector2Int.zero)
                {
                    directionCount[dir] = directionCount.GetValueOrDefault(dir, 0) + 1;
                }
            }
            
            // 按方向优先级排序（优先填充已有方向）
            List<Vector2Int> preferredDirections = new List<Vector2Int>
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };
            preferredDirections.Sort((a, b) => 
                directionCount.GetValueOrDefault(b, 0).CompareTo(directionCount.GetValueOrDefault(a, 0))
            );
            
            while (remainingGrids.Count >= m_FillConfig.minLength && attempts < maxAttempts)
            {
                attempts++;
                
                // 随机选择一个起始点
                List<Vector2Int> remainingList = new List<Vector2Int>(remainingGrids);
                Vector2Int startPos = remainingList[UnityEngine.Random.Range(0, remainingList.Count)];
                
                // 尝试按优先级方向生成路径
                List<Vector2Int> path = null;
                foreach (var preferredDir in preferredDirections)
                {
                    path = GenerateArrowPathWithDirection(startPos, remainingGrids, usedGrids, preferredDir);
                    if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                    {
                        // 检查路径冲突
                        if (!WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                        {
                            break; // 找到有效路径
                        }
                        else
                        {
                            path = null; // 冲突，继续尝试其他方向
                        }
                    }
                    else
                    {
                        path = null;
                    }
                }
                
                // 如果按优先级方向都没找到，尝试随机方向
                if (path == null)
                {
                    path = GenerateArrowPathSafe(startPos, remainingGrids, usedGrids);
                }
                
                if (path != null && path.Count >= m_FillConfig.minLength && IsValidPath(path, usedGrids))
                {
                    // 检查路径是否会阻挡已有箭头且不冲突前进方向
                    if (!WouldBlockExistingArrows(path) && !WouldConflictWithArrowDirection(path) && !WouldCreateDeadlock(path))
                    {
                        ArrowLineDef arrow = new ArrowLineDef
                        {
                            path = path,
                            lineColor = GetRandomColor(),
                            hitColor = Color.red,
                            occupyAllNodes = true,
                            startIndex = 0
                        };
                        
                        m_CurrentLevelData.arrows.Add(arrow);
                        arrowCount++;
                        
                        foreach (var pos in path)
                        {
                            remainingGrids.Remove(pos);
                            usedGrids.Add(pos);
                        }
                        
                        // 更新方向统计
                        Vector2Int dir = path[path.Count - 1] - path[path.Count - 2];
                        if (dir != Vector2Int.zero)
                        {
                            directionCount[dir] = directionCount.GetValueOrDefault(dir, 0) + 1;
                            // 重新排序优先级
                            preferredDirections.Sort((a, b) => 
                                directionCount.GetValueOrDefault(b, 0).CompareTo(directionCount.GetValueOrDefault(a, 0))
                            );
                        }
                    }
                }
            }
            
            return arrowCount;
        }
        
        /// <summary>
        /// 按指定方向生成箭头路径
        /// </summary>
        private List<Vector2Int> GenerateArrowPathWithDirection(Vector2Int startPos, HashSet<Vector2Int> availableGrids, 
            HashSet<Vector2Int> usedGrids, Vector2Int preferredDirection)
        {
            // 如果起点已被占用，返回null
            if (usedGrids.Contains(startPos))
            {
                return null;
            }
            
            List<Vector2Int> path = new List<Vector2Int> { startPos };
            Vector2Int currentPos = startPos;
            Vector2Int lastDirection = preferredDirection;
            
            int targetLength = UnityEngine.Random.Range(m_FillConfig.minLength, m_FillConfig.maxLength + 1);
            
            for (int i = 1; i < targetLength; i++)
            {
                List<Vector2Int> candidates = GetAvailableNeighborsForPath(currentPos, availableGrids, path, usedGrids, lastDirection);
                
                if (candidates.Count == 0)
                {
                    break;
                }
                
                Vector2Int nextPos;
                // 优先选择preferredDirection方向
                Vector2Int preferredPos = currentPos + lastDirection;
                if (candidates.Contains(preferredPos))
                {
                    nextPos = preferredPos;
                }
                else if (m_FillConfig.allowTurn && candidates.Count > 0)
                {
                    nextPos = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                }
                else
                {
                    if (candidates.Count > 0)
                    {
                        nextPos = candidates[0];
                    }
                    else
                    {
                        break;
                    }
                }
                
                lastDirection = nextPos - currentPos;
                path.Add(nextPos);
                currentPos = nextPos;
            }
            
            return path.Count >= m_FillConfig.minLength ? path : null;
        }
        
        /// <summary>
        /// 安全生成箭头路径（考虑已使用的网格）
        /// </summary>
        private List<Vector2Int> GenerateArrowPathSafe(Vector2Int startPos, HashSet<Vector2Int> availableGrids, 
            HashSet<Vector2Int> usedGrids)
        {
            // 如果起点已被占用，返回null
            if (usedGrids.Contains(startPos))
            {
                return null;
            }
            
            List<Vector2Int> path = new List<Vector2Int> { startPos };
            Vector2Int currentPos = startPos;
            Vector2Int lastDirection = Vector2Int.zero;
            
            int targetLength = UnityEngine.Random.Range(m_FillConfig.minLength, m_FillConfig.maxLength + 1);
            
            for (int i = 1; i < targetLength; i++)
            {
                List<Vector2Int> candidates = GetAvailableNeighborsForPath(currentPos, availableGrids, path, usedGrids, lastDirection);
                
                if (candidates.Count == 0)
                {
                    break;
                }
                
                Vector2Int nextPos;
                Vector2Int straightPos = lastDirection != Vector2Int.zero ? currentPos + lastDirection : currentPos;
                if (candidates.Contains(straightPos))
                {
                    nextPos = straightPos;
                }
                else if (m_FillConfig.allowTurn && candidates.Count > 1)
                {
                    nextPos = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                }
                else
                {
                    nextPos = candidates[0];
                }
                
                lastDirection = nextPos - currentPos;
                path.Add(nextPos);
                currentPos = nextPos;
            }
            
            return path.Count >= m_FillConfig.minLength ? path : null;
        }
        
        /// <summary>
        /// 生成允许多次转折的路径，用于增加解谜难度。以 turnBias 概率优先选择拐弯而非直线延伸。
        /// </summary>
        private List<Vector2Int> GenerateArrowPathEncourageTurns(Vector2Int startPos, HashSet<Vector2Int> availableGrids,
            HashSet<Vector2Int> usedGrids, float turnBias = 0.5f)
        {
            if (usedGrids.Contains(startPos)) return null;
            List<Vector2Int> path = new List<Vector2Int> { startPos };
            Vector2Int currentPos = startPos;
            Vector2Int lastDirection = Vector2Int.zero;
            int targetLength = UnityEngine.Random.Range(
                Mathf.Max(m_FillConfig.minLength, 3),
                m_FillConfig.maxLength + 1);
            for (int i = 1; i < targetLength; i++)
            {
                List<Vector2Int> candidates = GetAvailableNeighborsForPath(currentPos, availableGrids, path, usedGrids, lastDirection);
                if (candidates.Count == 0) break;
                Vector2Int straightPos = lastDirection != Vector2Int.zero ? currentPos + lastDirection : currentPos;
                var turnCandidates = new List<Vector2Int>();
                var straightCandidates = new List<Vector2Int>();
                foreach (var c in candidates)
                {
                    if (WouldForwardRayHitPathIfExtended(path, c)) continue;
                    if (c == straightPos) straightCandidates.Add(c);
                    else turnCandidates.Add(c);
                }
                Vector2Int nextPos;
                bool preferTurn = (turnBias > 0.001f && UnityEngine.Random.value < turnBias) && turnCandidates.Count > 0;
                if (preferTurn && turnCandidates.Count > 0)
                    nextPos = turnCandidates[UnityEngine.Random.Range(0, turnCandidates.Count)];
                else if (straightCandidates.Count > 0)
                    nextPos = straightCandidates[0];
                else if (turnCandidates.Count > 0)
                    nextPos = turnCandidates[UnityEngine.Random.Range(0, turnCandidates.Count)];
                else
                    break;
                lastDirection = nextPos - currentPos;
                path.Add(nextPos);
                currentPos = nextPos;
            }
            return path.Count >= m_FillConfig.minLength ? path : null;
        }
        
        /// <summary>
        /// 从路径得到头部、前进方向、下一步格点。path 顺序为 [..., tail, head]。
        /// </summary>
        private bool GetHeadDirectionNextStep(List<Vector2Int> path, out Vector2Int head, out Vector2Int direction, out Vector2Int nextStep)
        {
            head = default;
            direction = default;
            nextStep = default;
            if (path == null || path.Count < 2) return false;
            head = path[path.Count - 1];
            direction = path[path.Count - 1] - path[path.Count - 2];
            nextStep = head + direction;
            return true;
        }

        /// <summary>
        /// 构建「谁被谁阻挡」有向图：与玩法一致，用整条前进射线判定。graph[i] = 阻挡箭头 i 的箭头下标集合（i 的前进射线上任一点在 j 的路径上则 j 在 graph[i] 中）。
        /// </summary>
        private List<List<int>> BuildBlockingGraph(List<ArrowLineDef> arrows, List<Vector2Int> extraPath = null)
        {
            int n = arrows.Count + (extraPath != null ? 1 : 0);
            var graph = new List<List<int>>(n);
            for (int i = 0; i < n; i++) graph.Add(new List<int>());

            var paths = new List<List<Vector2Int>>(n);
            var pathSets = new List<HashSet<Vector2Int>>(n);
            for (int i = 0; i < arrows.Count; i++)
            {
                var path = arrows[i].path != null && arrows[i].path.Count >= 2 ? arrows[i].path : new List<Vector2Int>();
                paths.Add(path);
                pathSets.Add(path.Count > 0 ? new HashSet<Vector2Int>(path) : new HashSet<Vector2Int>());
            }
            if (extraPath != null)
            {
                paths.Add(extraPath);
                pathSets.Add(extraPath.Count > 0 ? new HashSet<Vector2Int>(extraPath) : new HashSet<Vector2Int>());
            }

            var validGrids = m_CurrentLevelData?.validGrids;
            if (validGrids == null) return graph;

            for (int i = 0; i < n; i++)
            {
                if (!GetHeadDirectionNextStep(paths[i], out Vector2Int head, out Vector2Int dir, out _))
                    continue;
                if (dir == Vector2Int.zero) continue;
                var ray = GetForwardRayCells(head, dir, validGrids);
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    foreach (var cell in ray)
                    {
                        if (pathSets[j].Contains(cell))
                        {
                            graph[i].Add(j);
                            break;
                        }
                    }
                }
            }
            return graph;
        }

        /// <summary>
        /// 在有向图中找出所有参与环的节点（多箭头死锁中的箭头下标）。
        /// </summary>
        private HashSet<int> FindNodesInCycles(List<List<int>> graph)
        {
            int n = graph.Count;
            var inCycle = new HashSet<int>();
            var visited = new bool[n];
            var inStack = new bool[n];
            var stack = new List<int>();
            var indexInStack = new Dictionary<int, int>();

            void Dfs(int v)
            {
                visited[v] = true;
                inStack[v] = true;
                indexInStack[v] = stack.Count;
                stack.Add(v);

                foreach (int w in graph[v])
                {
                    if (!visited[w])
                        Dfs(w);
                    else if (inStack[w])
                    {
                        int idx = indexInStack[w];
                        for (int i = idx; i < stack.Count; i++)
                            inCycle.Add(stack[i]);
                    }
                }

                stack.RemoveAt(stack.Count - 1);
                indexInStack.Remove(v);
                inStack[v] = false;
            }

            for (int i = 0; i < n; i++)
            {
                if (!visited[i])
                    Dfs(i);
            }
            return inCycle;
        }

        /// <summary>
        /// 若加入新路径会与当前箭头形成循环死锁（多箭头互相阻挡），返回 true。
        /// </summary>
        private bool WouldCreateDeadlock(List<Vector2Int> newPath)
        {
            if (newPath == null || newPath.Count < 2) return false;
            if (m_CurrentLevelData?.arrows == null) return false;
            var graph = BuildBlockingGraph(m_CurrentLevelData.arrows, newPath);
            var cycleNodes = FindNodesInCycles(graph);
            return cycleNodes.Count > 0;
        }

        /// <summary>
        /// 检查新箭头路径是否与已有箭头路径重叠（占用同一格）。重叠则不允许。
        /// 在方向不冲突且不会形成死锁的前提下，允许新箭头阻挡已有箭头（或反之），仅禁止路径占同一格。
        /// </summary>
        /// <param name="occupiedCells">可选；填充时传入已维护的已占用格集合，避免重复遍历所有箭头</param>
        private bool WouldBlockExistingArrows(List<Vector2Int> newPath, HashSet<Vector2Int> occupiedCells = null)
        {
            if (newPath == null || newPath.Count < 2) return false;
            if (occupiedCells != null)
            {
                foreach (var p in newPath)
                    if (occupiedCells.Contains(p)) return true;
                return false;
            }
            var newPathSet = new HashSet<Vector2Int>(newPath);
            foreach (var existingArrow in m_CurrentLevelData.arrows)
            {
                if (existingArrow.path == null) continue;
                foreach (var p in existingArrow.path)
                {
                    if (newPathSet.Contains(p))
                        return true;
                }
            }
            return false;
        }
        

        /// <summary>
        /// 检查新箭头的最终前进方向是否与已有箭头冲突
        /// 矛盾相交判定：A箭头与B箭头最终前进方向相反，且A箭头前进路径被B箭头占用同时B箭头前进路径也被A箭头占用
        /// </summary>
        private bool WouldConflictWithArrowDirection(List<Vector2Int> newPath)
        {
            if (newPath == null || newPath.Count < 2) return false;
            
            // 计算新箭头的最终前进方向
            Vector2Int newDirection = newPath[newPath.Count - 1] - newPath[newPath.Count - 2];
            Vector2Int newHeadPos = newPath[newPath.Count - 1];
            
            // 如果方向为零，不冲突
            if (newDirection == Vector2Int.zero) return false;
            
            // 计算新箭头的前进路径（延伸到边界）
            List<Vector2Int> newForwardPath = CalculateForwardPath(newHeadPos, newDirection, m_CurrentLevelData.validGrids);
            
            foreach (var existingArrow in m_CurrentLevelData.arrows)
            {
                if (existingArrow.path == null || existingArrow.path.Count < 2) continue;
                
                // 计算已有箭头的最终前进方向
                Vector2Int existingDirection = existingArrow.path[existingArrow.path.Count - 1] - 
                                               existingArrow.path[existingArrow.path.Count - 2];
                Vector2Int existingHeadPos = existingArrow.path[existingArrow.path.Count - 1];
                
                // 如果方向为零，跳过
                if (existingDirection == Vector2Int.zero) continue;
                
                // 只有方向相反时才检查矛盾相交
                if (existingDirection == -newDirection)
                {
                    // 计算已有箭头的前进路径
                    List<Vector2Int> existingForwardPath = CalculateForwardPath(existingHeadPos, existingDirection, m_CurrentLevelData.validGrids);
                    
                    // 检查是否矛盾相交：
                    // 1. A箭头前进路径被B箭头占用（B箭头的路径包含A箭头前进路径上的点）
                    // 2. B箭头前进路径被A箭头占用（A箭头的路径包含B箭头前进路径上的点）
                    bool newForwardBlockedByExisting = DoPathsIntersect(newForwardPath, existingArrow.path);
                    bool existingForwardBlockedByNew = DoPathsIntersect(existingForwardPath, newPath);
                    
                    // 只有两个条件都满足时，才视为矛盾
                    if (newForwardBlockedByExisting && existingForwardBlockedByNew)
                    {
                        return true; // 矛盾相交
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 与运行时玩法保持一致的「是否被阻挡」判断：
        /// - 仅考虑同一行/同一列、且在前进方向上的其他箭头格子；
        /// - 中间即便有镂空（无有效格），后方的箭头仍视为阻挡。
        /// 返回当前剩余箭头中「无阻挡」的箭头下标集合。
        /// </summary>
        private List<int> GetUnblockedArrowIndices(List<ArrowLineDef> arrows, List<int> remainingIndices, HashSet<Vector2Int> validGrids)
        {
            var unblocked = new List<int>();
            if (remainingIndices == null || remainingIndices.Count == 0)
                return unblocked;

            // 预构建每个剩余箭头的路径缓存，便于多次遍历
            var pathCache = new Dictionary<int, List<Vector2Int>>();
            foreach (int idx in remainingIndices)
            {
                var path = (idx >= 0 && idx < arrows.Count) ? arrows[idx].path : null;
                if (path != null && path.Count >= 2)
                    pathCache[idx] = path;
            }

            foreach (int i in remainingIndices)
            {
                if (!pathCache.TryGetValue(i, out var path) || path == null || path.Count < 2)
                    continue;

                Vector2Int head = path[path.Count - 1];
                Vector2Int step = path[path.Count - 1] - path[path.Count - 2];
                if (step.x != 0) step.x = step.x > 0 ? 1 : -1;
                if (step.y != 0) step.y = step.y > 0 ? 1 : -1;
                if (step == Vector2Int.zero)
                    continue;

                bool blocked = false;

                // 遍历其他剩余箭头的路径，只要有任意一个格子落在「同一行/列且前方」即视为阻挡
                foreach (int j in remainingIndices)
                {
                    if (j == i) continue;
                    if (!pathCache.TryGetValue(j, out var otherPath) || otherPath == null || otherPath.Count == 0)
                        continue;

                    foreach (var p in otherPath)
                    {
                        if (step.x != 0)
                        {
                            if (p.y != head.y) continue;
                            if (step.x > 0 && p.x > head.x) { blocked = true; break; }
                            if (step.x < 0 && p.x < head.x) { blocked = true; break; }
                        }
                        else if (step.y != 0)
                        {
                            if (p.x != head.x) continue;
                            if (step.y > 0 && p.y > head.y) { blocked = true; break; }
                            if (step.y < 0 && p.y < head.y) { blocked = true; break; }
                        }
                    }

                    if (blocked) break;
                }

                if (!blocked)
                    unblocked.Add(i);
            }

            return unblocked;
        }

        /// <summary>
        /// 模拟通关：每轮移除所有无阻挡的箭头，直到全部移除或没有可移除箭头。
        /// 返回 (是否全部移除, 仍无法移除的箭头下标列表)。
        /// </summary>
        private (bool allCleared, List<int> remainingIndices) SimulateClearance(List<ArrowLineDef> arrows, HashSet<Vector2Int> validGrids)
        {
            var remaining = new List<int>();
            for (int i = 0; i < arrows.Count; i++)
                remaining.Add(i);
            int maxRounds = arrows.Count + 1;
            for (int round = 0; round < maxRounds; round++)
            {
                var unblocked = GetUnblockedArrowIndices(arrows, remaining, validGrids);
                if (unblocked.Count == 0)
                    break;
                for (int k = unblocked.Count - 1; k >= 0; k--)
                    remaining.Remove(unblocked[k]);
                if (remaining.Count == 0)
                    break;
            }
            return (remaining.Count == 0, remaining);
        }

        /// <summary>
        /// 校验当前关卡中箭头是否存在互斥/死锁，并检测通关可能性。
        /// 通关检测：每轮将无阻挡的箭头移除，直到全部移除或没有可移除箭头；若无法全部移除则视为不合理。
        /// </summary>
        private void ValidateArrows()
        {
            if (m_CurrentLevelData == null || m_CurrentLevelData.arrows == null || m_CurrentLevelData.arrows.Count == 0)
            {
                m_ConflictingArrows.Clear();
                EditorUtility.DisplayDialog("箭头校验", "当前关卡中没有箭头可供校验。", "确定");
                return;
            }

            m_ConflictingArrows.Clear();

            var arrows = m_CurrentLevelData.arrows;
            var validGrids = m_CurrentLevelData.validGrids;
            int pairCount = 0;
            var cycleNodes = new HashSet<int>();

            // 1) 两两互斥：最终前进方向相反且前进路径相互占用
            for (int i = 0; i < arrows.Count; i++)
            {
                var a = arrows[i];
                if (a.path == null || a.path.Count < 2) continue;

                Vector2Int aDir = a.path[a.path.Count - 1] - a.path[a.path.Count - 2];
                if (aDir == Vector2Int.zero) continue;
                Vector2Int aHead = a.path[a.path.Count - 1];

                List<Vector2Int> aForward = CalculateForwardPath(aHead, aDir, validGrids);

                for (int j = i + 1; j < arrows.Count; j++)
                {
                    var b = arrows[j];
                    if (b.path == null || b.path.Count < 2) continue;

                    Vector2Int bDir = b.path[b.path.Count - 1] - b.path[b.path.Count - 2];
                    if (bDir == Vector2Int.zero) continue;

                    if (bDir != -aDir) continue;

                    Vector2Int bHead = b.path[b.path.Count - 1];
                    List<Vector2Int> bForward = CalculateForwardPath(bHead, bDir, validGrids);

                    bool aForwardBlockedByB = DoPathsIntersect(aForward, b.path);
                    bool bForwardBlockedByA = DoPathsIntersect(bForward, a.path);

                    if (aForwardBlockedByB && bForwardBlockedByA)
                    {
                        m_ConflictingArrows.Add(a);
                        m_ConflictingArrows.Add(b);
                        pairCount++;
                    }
                }
            }

            // 2) 多箭头循环死锁
            var graph = BuildBlockingGraph(arrows, null);
            cycleNodes = FindNodesInCycles(graph);
            for (int i = 0; i < arrows.Count; i++)
            {
                if (cycleNodes.Contains(i))
                    m_ConflictingArrows.Add(arrows[i]);
            }

            // 3) 通关可能性：模拟每轮移除无阻挡箭头，直到全部移除或死锁
            (bool allCleared, List<int> remainingIndices) = SimulateClearance(arrows, validGrids);
            if (!allCleared)
            {
                foreach (int i in remainingIndices)
                    m_ConflictingArrows.Add(arrows[i]);
            }

            Repaint();

            if (m_ConflictingArrows.Count == 0)
            {
                EditorUtility.DisplayDialog("箭头校验", "校验通过：未发现互斥或死锁，且通关检测通过（可全部移除）。", "确定");
            }
            else
            {
                string msg = "";
                if (!allCleared)
                    msg += "通关检测未通过：无法全部移除，存在死锁。";
                if (pairCount > 0)
                    msg += (msg.Length > 0 ? " " : "") + $"发现 {pairCount} 组两两互斥；";
                if (cycleNodes.Count > 0)
                    msg += (msg.Length > 0 ? " " : "") + $"发现 {cycleNodes.Count} 个箭头参与循环死锁。";
                msg += "\n已在网格中用红色高亮标出。";
                EditorUtility.DisplayDialog("箭头校验", msg.TrimStart(), "确定");
            }
        }
        
        /// <summary>
        /// 计算箭头的前进路径（从头部沿方向延伸到有效格包围盒边界）。
        /// 以有效格 XY 的最小最大值作为边界：同一行/列中的镂空不视为边界，射线穿过镂空继续延伸直到超出包围盒。
        /// </summary>
        private List<Vector2Int> CalculateForwardPath(Vector2Int headPos, Vector2Int direction, HashSet<Vector2Int> validGrids)
        {
            List<Vector2Int> forwardPath = new List<Vector2Int>();
            
            if (direction == Vector2Int.zero) return forwardPath;
            if (!GetValidGridBounds(validGrids, out int minX, out int maxX, out int minY, out int maxY))
                return forwardPath;
            
            Vector2Int currentPos = headPos;
            int maxSteps = 100; // 最大步数，防止无限循环
            
            for (int i = 0; i < maxSteps; i++)
            {
                Vector2Int nextPos = currentPos + direction;
                
                // 超出有效格包围盒（XY 最小最大值）则停止
                if (nextPos.x < minX || nextPos.x > maxX || nextPos.y < minY || nextPos.y > maxY)
                    break;
                
                if (validGrids.Contains(nextPos))
                    forwardPath.Add(nextPos);
                currentPos = nextPos;
            }
            
            return forwardPath;
        }
        
        /// <summary>
        /// 检查两条路径是否相交（有公共点）
        /// </summary>
        private bool DoPathsIntersect(List<Vector2Int> path1, List<Vector2Int> path2)
        {
            if (path1 == null || path2 == null) return false;
            
            HashSet<Vector2Int> path1Set = new HashSet<Vector2Int>(path1);
            
            foreach (var pos in path2)
            {
                if (path1Set.Contains(pos))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 生成箭头路径
        /// </summary>
        private List<Vector2Int> GenerateArrowPath(Vector2Int startPos, HashSet<Vector2Int> availableGrids)
        {
            List<Vector2Int> path = new List<Vector2Int> { startPos };
            Vector2Int currentPos = startPos;
            Vector2Int lastDirection = Vector2Int.zero;
            
            int targetLength = UnityEngine.Random.Range(m_FillConfig.minLength, m_FillConfig.maxLength + 1);
            
            for (int i = 1; i < targetLength; i++)
            {
                // 获取可用的相邻网格
                List<Vector2Int> candidates = GetAvailableNeighbors(currentPos, availableGrids, path, lastDirection);
                
                if (candidates.Count == 0)
                {
                    // 没有可用路径，返回当前路径
                    break;
                }
                
                // 选择下一个位置
                Vector2Int nextPos;
                if (m_FillConfig.allowTurn && candidates.Count > 1)
                {
                    // 允许拐弯，随机选择
                    nextPos = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                }
                else
                {
                    // 优先选择直线方向
                    if (lastDirection != Vector2Int.zero)
                    {
                        Vector2Int straightPos = currentPos + lastDirection;
                        if (candidates.Contains(straightPos))
                        {
                            nextPos = straightPos;
                        }
                        else
                        {
                            nextPos = candidates[0];
                        }
                    }
                    else
                    {
                        nextPos = candidates[0];
                    }
                }
                
                // 更新方向
                lastDirection = nextPos - currentPos;
                
                // 添加到路径
                path.Add(nextPos);
                currentPos = nextPos;
            }
            
            return path.Count >= m_FillConfig.minLength ? path : null;
        }
        
        /// <summary>
        /// 获取可用的相邻网格
        /// </summary>
        private List<Vector2Int> GetAvailableNeighbors(Vector2Int pos, HashSet<Vector2Int> availableGrids, 
            List<Vector2Int> currentPath, Vector2Int preferredDirection)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            
            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };
            
            // 如果 preferredDirection 不为零，优先检查该方向
            if (preferredDirection != Vector2Int.zero)
            {
                Vector2Int preferredPos = pos + preferredDirection;
                if (availableGrids.Contains(preferredPos) && !currentPath.Contains(preferredPos))
                {
                    neighbors.Add(preferredPos);
                }
            }
            
            // 检查其他方向
            foreach (var dir in directions)
            {
                if (dir == preferredDirection) continue; // 已检查过
                
                Vector2Int neighborPos = pos + dir;
                if (availableGrids.Contains(neighborPos) && !currentPath.Contains(neighborPos))
                {
                    neighbors.Add(neighborPos);
                }
            }
            
            return neighbors;
        }
    }
}
