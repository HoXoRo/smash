using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowMaze
{
    /// <summary>
    /// 网格定义
    /// </summary>
    [Serializable]
    public class GridDef
    {
        public int width = 15;
        public int height = 15;
        public float spacing = 1f;
        public Vector3 origin = Vector3.zero;
    }

    /// <summary>
    /// 可序列化的Vector2Int
    /// </summary>
    [Serializable]
    public class SerializableVector2Int
    {
        public int x;
        public int y;
        
        public SerializableVector2Int() { }
        
        public SerializableVector2Int(Vector2Int v)
        {
            x = v.x;
            y = v.y;
        }
        
        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }
    }
    
    /// <summary>
    /// 箭头线定义
    /// </summary>
    [Serializable]
    public class ArrowLineDef
    {
        /// <summary>
        /// 箭头路径（网格坐标）- 序列化时使用SerializableVector2Int列表
        /// </summary>
        [System.NonSerialized]
        public List<Vector2Int> path = new List<Vector2Int>();
        
        /// <summary>
        /// 用于序列化的路径数据
        /// </summary>
        public List<SerializableVector2Int> serializedPath = new List<SerializableVector2Int>();
        
        /// <summary>
        /// 每步移动时间
        /// </summary>
        public float stepTime = 0.05f;
        
        /// <summary>
        /// 线条颜色
        /// </summary>
        public Color lineColor = Color.white;
        
        /// <summary>
        /// 碰撞时的颜色
        /// </summary>
        public Color hitColor = Color.red;
        
        /// <summary>
        /// 是否占用所有节点
        /// </summary>
        public bool occupyAllNodes = true;
        
        /// <summary>
        /// 起始索引（用于显示箭头的一部分）
        /// </summary>
        public int startIndex = 0;

        /// <summary>
        /// 是否为特殊奖励箭头（绿色），运行时由关卡分配，不序列化
        /// </summary>
        [System.NonSerialized]
        public bool isRewardArrow;
        
        /// <summary>
        /// 序列化前调用：将path转换为serializedPath
        /// </summary>
        public void OnBeforeSerialize()
        {
            serializedPath.Clear();
            foreach (var v in path)
            {
                serializedPath.Add(new SerializableVector2Int(v));
            }
        }
        
        /// <summary>
        /// 反序列化后调用：将serializedPath转换为path
        /// </summary>
        public void OnAfterDeserialize()
        {
            path.Clear();
            foreach (var v in serializedPath)
            {
                path.Add(v.ToVector2Int());
            }
        }
    }

    /// <summary>
    /// 箭头填充配置
    /// </summary>
    [Serializable]
    public class ArrowFillConfig
    {
        /// <summary>
        /// 最小长度（网格单位）
        /// </summary>
        public int minLength = 2;
        
        /// <summary>
        /// 最大长度（网格单位）
        /// </summary>
        public int maxLength = 10;
        
        /// <summary>
        /// 是否允许拐弯
        /// </summary>
        public bool allowTurn = true;
        
        /// <summary>
        /// 填充模式：true=自动填充，false=手动填充
        /// </summary>
        public bool autoFill = true;
    }

    /// <summary>
    /// 关卡数据
    /// </summary>
    [Serializable]
    public class ArrowLevelData
    {
        /// <summary>
        /// 关卡名称
        /// </summary>
        public string levelName = "New Level";
        
        /// <summary>
        /// 网格定义
        /// </summary>
        public GridDef grid = new GridDef();
        
        /// <summary>
        /// 有效网格集合（网格坐标）
        /// </summary>
        public HashSet<Vector2Int> validGrids = new HashSet<Vector2Int>();
        
        /// <summary>
        /// 箭头线列表
        /// </summary>
        public List<ArrowLineDef> arrows = new List<ArrowLineDef>();
        
        /// <summary>
        /// 填充配置
        /// </summary>
        public ArrowFillConfig fillConfig = new ArrowFillConfig();
        
        /// <summary>
        /// 相机大小
        /// </summary>
        public float cameraSize = 5f;
        
        /// <summary>
        /// 关卡难度
        /// </summary>
        public int difficulty = 1;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public string createTime;
        
        /// <summary>
        /// 修改时间
        /// </summary>
        public string modifyTime;
    }
}
