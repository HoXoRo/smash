using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Resource;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArrowMaze
{
    /// <summary>
    /// 关卡数据工具类（Json序列化）
    /// </summary>
    public static class ArrowLevelDataUtility
    {
        /// <summary>
        /// 保存关卡数据到Json文件
        /// </summary>
        public static bool SaveToJson(ArrowLevelData levelData, string filePath)
        {
            try
            {
                // 更新修改时间
                levelData.modifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 序列化有效网格（HashSet需要转换为List）
                var serializableData = new SerializableArrowLevelData(levelData);
                
                // 紧凑 JSON，不输出缩进/换行以减小文件体积
                string json = JsonUtility.ToJson(serializableData, false);
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 写入文件
                File.WriteAllText(filePath, json);
                
                Debug.Log($"关卡数据已保存到: {filePath}");
                
#if UNITY_EDITOR
                // 刷新资源数据库
                AssetDatabase.Refresh();
#endif
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存关卡数据失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 将已有 Json 文件重写为紧凑格式（不修改 modifyTime）。
        /// </summary>
        public static bool MinifyJsonFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"文件不存在: {filePath}");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                SerializableArrowLevelData serializableData = JsonUtility.FromJson<SerializableArrowLevelData>(json);
                if (serializableData == null)
                {
                    Debug.LogError($"反序列化失败: {filePath}");
                    return false;
                }

                string compactJson = JsonUtility.ToJson(serializableData, false);
                File.WriteAllText(filePath, compactJson);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"压缩关卡 JSON 失败: {filePath}, {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 从Json文件加载关卡数据
        /// </summary>
        public static ArrowLevelData LoadFromJson(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"文件不存在: {filePath}");
                    return null;
                }
                
                string json = File.ReadAllText(filePath);
                
                // 使用Unity的JsonUtility反序列化
                SerializableArrowLevelData serializableData = JsonUtility.FromJson<SerializableArrowLevelData>(json);
                
                if (serializableData == null)
                {
                    Debug.LogError("反序列化失败");
                    return null;
                }
                
                // 转换为ArrowLevelData
                ArrowLevelData levelData = serializableData.ToArrowLevelData();
                
                Log.Info($"关卡数据已加载: {filePath}");
                return levelData;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载关卡数据失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// 从Json文件加载关卡数据
        /// </summary>
        public static void LoadFromLevelDataJson(string levelName, Action<ArrowLevelData> onLoadSuccess, Action<string> onLoadFailure)
        {
            try
            {
                string filePath =  $"Assets/ArrowGame/ArrowDataLevel/{levelName}.json";
                GF.Resource.LoadAsset(filePath, new LoadAssetCallbacks((string assetName, object asset, float duration, object userData)=>
                {
                    string json = asset.ToString();
                    SerializableArrowLevelData serializableData = JsonUtility.FromJson<SerializableArrowLevelData>(json);
                    ArrowLevelData levelData = serializableData.ToArrowLevelData();
                    Log.Info($"关卡数据已加载: {filePath}");
                    onLoadSuccess?.Invoke(levelData);
                }, (string assetName, LoadResourceStatus status, string errorMessage, object userData)=>
                {
                    Debug.LogError($"加载关卡数据失败: {errorMessage}");
                    onLoadFailure?.Invoke(errorMessage);
                }));
            }
            catch (Exception e)
            {
                Debug.LogError($"加载关卡数据失败: {e.Message}\n{e.StackTrace}");
                onLoadFailure?.Invoke(e.Message);
            }
        }
        /// <summary>
        /// 可序列化的关卡数据（用于Json）
        /// </summary>
        [Serializable]
        private class SerializableArrowLevelData
        {
            public string levelName;
            public GridDef grid;
            public List<SerializableVector2Int> validGrids; // HashSet转为List
            public List<SerializableArrowLineDef> arrows; // 使用可序列化的箭头定义
            public ArrowFillConfig fillConfig;
            public float cameraSize;
            public int difficulty;
            public string createTime;
            public string modifyTime;
            
            public SerializableArrowLevelData(ArrowLevelData data)
            {
                levelName = data.levelName;
                grid = data.grid;
                validGrids = data.validGrids.Select(v => new SerializableVector2Int(v)).ToList();
                
                // 转换箭头路径
                arrows = new List<SerializableArrowLineDef>();
                foreach (var arrow in data.arrows)
                {
                    var serializedArrow = new SerializableArrowLineDef
                    {
                        serializedPath = arrow.path.Select(v => new SerializableVector2Int(v)).ToList(),
                        stepTime = arrow.stepTime,
                        lineColor = arrow.lineColor,
                        hitColor = arrow.hitColor,
                        occupyAllNodes = arrow.occupyAllNodes,
                        startIndex = arrow.startIndex
                    };
                    arrows.Add(serializedArrow);
                }
                
                fillConfig = data.fillConfig;
                cameraSize = data.cameraSize;
                difficulty = data.difficulty;
                createTime = data.createTime;
                modifyTime = data.modifyTime;
            }
            
            public ArrowLevelData ToArrowLevelData()
            {
                ArrowLevelData data = new ArrowLevelData
                {
                    levelName = levelName,
                    grid = grid,
                    validGrids = new HashSet<Vector2Int>(validGrids?.Select(v => v.ToVector2Int()) ?? new List<Vector2Int>()),
                    arrows = new List<ArrowLineDef>(),
                    fillConfig = fillConfig ?? new ArrowFillConfig(),
                    cameraSize = cameraSize,
                    difficulty = difficulty,
                    createTime = createTime,
                    modifyTime = modifyTime
                };
                
                // 转换箭头路径
                if (arrows != null)
                {
                    foreach (var arrow in arrows)
                    {
                        var deserializedArrow = new ArrowLineDef
                        {
                            path = arrow.serializedPath?.Select(v => v.ToVector2Int()).ToList() ?? new List<Vector2Int>(),
                            stepTime = arrow.stepTime,
                            lineColor = arrow.lineColor,
                            hitColor = arrow.hitColor,
                            occupyAllNodes = arrow.occupyAllNodes,
                            startIndex = arrow.startIndex
                        };
                        data.arrows.Add(deserializedArrow);
                    }
                }
                
                return data;
            }
        }
        
        /// <summary>
        /// 可序列化的箭头线定义
        /// </summary>
        [Serializable]
        private class SerializableArrowLineDef
        {
            public List<SerializableVector2Int> serializedPath;
            public float stepTime;
            public Color lineColor;
            public Color hitColor;
            public bool occupyAllNodes;
            public int startIndex;
        }
        
        /// <summary>
        /// 可序列化的Vector2Int（Unity的Vector2Int不能直接序列化）
        /// </summary>
        [Serializable]
        private class SerializableVector2Int
        {
            public int x;
            public int y;
            
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
    }
}
