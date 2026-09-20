using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// 箭头迷宫游戏管理器（单例）
    /// </summary>
    public class ArrowMazeManager : MonoBehaviour
    {
        private static ArrowMazeManager s_Instance;
        /// <summary> 退出/销毁阶段不再创建新实例，避免关闭场景时 getter 被调用而 spawn 新对象 </summary>
        private static bool s_QuittingOrDestroyed;

        public static ArrowMazeManager Instance
        {
            get
            {
                if (s_QuittingOrDestroyed || !Application.isPlaying)
                    return null;
                if (s_Instance == null)
                {
                    GameObject go = new GameObject("ArrowMazeManager");
                    s_Instance = go.AddComponent<ArrowMazeManager>();
                }
                return s_Instance;
            }
        }

        [SerializeField]
        private GridManager m_GridManager;
        
        [SerializeField]
        private LevelManager m_LevelManager;
        
        [SerializeField]
        private CameraController m_CameraController;

        /// <summary>
        /// 网格管理器
        /// </summary>
        public GridManager GridManager => m_GridManager;
        
        /// <summary>
        /// 关卡管理器
        /// </summary>
        public LevelManager LevelManager => m_LevelManager;
        
        /// <summary>
        /// 相机控制器
        /// </summary>
        public CameraController CameraController => m_CameraController;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            // 查找或创建GridManager
            if (m_GridManager == null)
            {
                GameObject gridRoot = GameObject.Find("GridRoot");
                if (gridRoot == null)
                {
                    gridRoot = new GameObject("GridRoot");
                }
                m_GridManager = gridRoot.GetComponent<GridManager>();
                if (m_GridManager == null)
                {
                    m_GridManager = gridRoot.AddComponent<GridManager>();
                }
            }

            // 查找或创建LevelManager
            if (m_LevelManager == null)
            {
                m_LevelManager = FindObjectOfType<LevelManager>();
            }
            if (m_LevelManager == null)
            {
                GameObject levelRoot = new GameObject("LevelManager");
                m_LevelManager = levelRoot.AddComponent<LevelManager>();
            }

            // 查找或创建CameraController
            if (m_CameraController == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    GameObject cameraObj = new GameObject("GameCamera");
                    mainCamera = cameraObj.AddComponent<Camera>();
                    mainCamera.orthographic = true;
                    mainCamera.orthographicSize = 5f;
                }
                m_CameraController = mainCamera.GetComponent<CameraController>();
                if (m_CameraController == null)
                {
                    m_CameraController = mainCamera.gameObject.AddComponent<CameraController>();
                }
                
                // 同步UI相机设置
                m_CameraController.SyncWithUICamera();
            }

            Log.Info("ArrowMazeManager 初始化完成");
        }

        /// <summary>
        /// 清理资源（由流程 OnLeave 调用）
        /// </summary>
        public void Cleanup()
        {
            s_QuittingOrDestroyed = true;

            if (m_LevelManager != null)
            {
                m_LevelManager.Cleanup();
                m_LevelManager = null;
            }

            if (m_GridManager != null)
            {
                m_GridManager.Cleanup();
                m_GridManager = null;
            }

            m_CameraController = null;

            if (s_Instance == this)
                s_Instance = null;

            if (gameObject != null)
                Destroy(gameObject);
        }

        private void Awake()
        {
            s_QuittingOrDestroyed = false;
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
            s_QuittingOrDestroyed = true;
        }

        private void OnApplicationQuit()
        {
            s_QuittingOrDestroyed = true;
        }
    }
}
