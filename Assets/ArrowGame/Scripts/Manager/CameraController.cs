using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// 相机控制器 - 处理缩放和移动，并与UI相机同步适配
    /// 参考EscapeGame项目的相机控制器实现
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private Camera m_Camera;

        [Header("Zoom Settings")]
        [SerializeField]
        private float m_MinSize = 2f;   // 运行时由 SetupForLevel 按关卡动态设置
        
        [SerializeField]
        private float m_MaxSize = 2f;  // 运行时由 SetupForLevel 按关卡动态设置

        /// <summary> 双指捏合灵敏度：每像素间距变化占 Min~Max 区间的比例（相对屏幕高度） </summary>
        [SerializeField]
        private float m_ZoomSpeed = 1.2f;

        /// <summary> 滚轮每 notch 改变的 orthographicSize 比例（0~1 表示占 Min~Max 区间的比例），避免灵敏度异常 </summary>
        [SerializeField]
        private float m_ScrollWheelStep = 0.08f;

        [Header("Pan Settings")]
        [SerializeField]
        private bool m_EnablePan = true;

        [SerializeField]
        private bool m_EnablePanLimits = true;

        [SerializeField]
        private bool m_EnableZoom = true;

        /// <summary> 网格边缘安全区（世界单位），允许相机平移超出网格边界，用于观察可能超出网格的箭头 </summary>
        [SerializeField]
        private float m_GridEdgeSafetyZone = 4f;

        /// <summary> 计算相机初始缩放与移动边界时，在关卡网格宽度方向两侧各增加的距离（世界单位），避免边缘箭头被 UI 遮挡 </summary>
        [SerializeField]
        private float m_GridBoundsPaddingX = 1f;
        /// <summary> 计算相机初始缩放与移动边界时，在关卡网格高度方向两侧各增加的距离（世界单位），避免边缘箭头被 UI 遮挡 </summary>
        [SerializeField]
        private float m_GridBoundsPaddingY = 1f;

        /// <summary>
        /// 地图未超出可视区时使用的参考 orthographicSize（scale=1 基准）。
        /// &gt; 0 时作为所有关卡缩放换算的基准；≤ 0 时使用相机当前 orthographicSize。
        /// </summary>
        [SerializeField]
        private float m_ReferenceOrthographicSize = 28f;

        // 标准设计分辨率（竖屏）
        private const float DESIGN_WIDTH = 1080f;
        private const float DESIGN_HEIGHT = 1920f;
        private const float DESIGN_ASPECT = DESIGN_WIDTH / DESIGN_HEIGHT; // 0.5625

        private float m_TargetZoom;
        private Vector3 m_LastTouchPosition;
        private bool m_IsDragging = false;
        /// <summary> 本指针周期内是否发生过平移（按下后拖动了场景）。用于箭头点击判定：若为 true 则抬起不触发箭头点击，避免误触扣生命。 </summary>
        private static bool s_DidPanThisTouch = false;
        public static bool DidPanThisTouch => s_DidPanThisTouch;
        private Bounds m_GridBounds;     // 网格在世界空间中的 AABB，min/max 为真实世界下界/上界
        private Camera m_UICamera;      // 保存UI相机引用，用于清理
        /// <summary> 相机中心在世界空间中的可移动下界 (x=左界, y=下界)，由 m_GridBounds 与当前视口计算 </summary>
        private Vector2 m_CameraCenterMin;
        /// <summary> 相机中心在世界空间中的可移动上界 (x=右界, y=上界) </summary>
        private Vector2 m_CameraCenterMax;
        /// <summary> 界面可显示范围（顶部栏与底部栏之间的空白区域），用于适配 orthographicSize 与移动边界 </summary>
        private RectTransform m_CameraViewRect;

        /// <summary> 玩法固定壁纸（相机子节点，Cover 全屏） </summary>
        private PlayBgWallpaper m_PlayBgWallpaper;

        /// <summary> 关卡入场动画总时长（秒） </summary>
        private const float EntranceDuration = 1f;
        private bool m_EntranceAnimating;
        private float m_EntranceElapsed;
        private float m_EntranceStartSize;
        private float m_EntranceEndSize;

        /// <summary> 是否正在播放关卡入场缩放动画，入场期间应屏蔽箭头点击。 </summary>
        public bool IsEntranceAnimating => m_EntranceAnimating;

        /// <summary>
        /// 指针是否落在玩法可见区域（varCameraView）内。
        /// </summary>
        public bool IsPointerInCameraView(Vector2 screenPosition)
        {
            if (m_CameraViewRect == null)
                return true;

            Canvas canvas = m_CameraViewRect.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(m_CameraViewRect, screenPosition, uiCam);
        }

        private void Awake()
        {
            if (m_Camera == null)
            {
                m_Camera = GetComponent<Camera>();
            }
            
            m_TargetZoom = m_Camera.orthographicSize;
            
            // 同步UI相机设置
            SyncWithUICamera();
            EnsurePlayBgWallpaper();
        }

        private void Update()
        {
            if (m_EntranceAnimating)
            {
                m_EntranceElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(m_EntranceElapsed / EntranceDuration);
                float smoothT = t * t * (3f - 2f * t);
                if (m_Camera != null)
                {
                    m_Camera.orthographicSize = Mathf.Lerp(m_EntranceStartSize, m_EntranceEndSize, smoothT);
                    CalculatePanLimits();
                }
                if (t >= 1f)
                {
                    m_EntranceAnimating = false;
                    if (m_Camera != null)
                    {
                        m_Camera.orthographicSize = m_EntranceEndSize;
                        m_TargetZoom = m_EntranceEndSize;
                    }
                }
                return;
            }
            HandlePinchZoom();
            HandleMouseScrollZoom();
            HandlePanInput();
            ApplySmoothZoom();
        }

        void EnsurePlayBgWallpaper()
        {
            if (m_Camera == null)
                return;
            m_PlayBgWallpaper = PlayBgWallpaper.Ensure(m_Camera);
        }

        private void OnDestroy()
        {
            // 清理cameraStack，移除UI相机
            CleanupCameraStack();
        }

        /// <summary>
        /// 清理相机堆叠
        /// </summary>
        private void CleanupCameraStack()
        {
            if (m_Camera != null && m_UICamera != null)
            {
                var gameCameraData = m_Camera.GetUniversalAdditionalCameraData();
                if (gameCameraData != null && gameCameraData.cameraStack.Contains(m_UICamera))
                {
                    gameCameraData.cameraStack.Remove(m_UICamera);
                    Log.Info("已从玩法相机的cameraStack中移除UI相机");
                }
            }
        }

        /// <summary>
        /// 与主场景的UI相机同步适配
        /// 将玩法相机设置为主相机，UI相机设置为Overlay模式并添加到cameraStack
        /// </summary>
        public void SyncWithUICamera()
        {
            if (m_Camera == null) return;
            
            // 获取UI根Canvas的相机设置
            var uiRootCanvas = GFBuiltin.RootCanvas;
            if (uiRootCanvas != null && uiRootCanvas.worldCamera != null)
            {
                Camera uiCamera = uiRootCanvas.worldCamera;
                m_UICamera = uiCamera; // 保存引用用于清理
                
                // 1. 将玩法相机设置为主相机
                m_Camera.tag = "MainCamera";
                m_Camera.orthographic = true; // 箭头迷宫使用正交相机
                m_Camera.clearFlags = CameraClearFlags.SolidColor; // 清除背景色
                // m_Camera.backgroundColor = Color.black; // 设置背景色（可根据需要调整）
                
                // 2. 配置玩法相机为Base相机（URP主相机）
                var gameCameraData = m_Camera.GetUniversalAdditionalCameraData();
                gameCameraData.renderType = CameraRenderType.Base; // 设置为Base相机
                
                // 3. 配置UI相机为Overlay相机
                var uiCameraData = uiCamera.GetUniversalAdditionalCameraData();
                uiCameraData.renderType = CameraRenderType.Overlay; // 设置为Overlay相机
                
                // 4. 将UI相机添加到玩法相机的cameraStack中
                // 先清除可能存在的旧引用，避免重复添加
                if (gameCameraData.cameraStack.Contains(uiCamera))
                {
                    gameCameraData.cameraStack.Remove(uiCamera);
                }
                gameCameraData.cameraStack.Add(uiCamera);
                
                Log.Info($"玩法相机已设置为主相机 (Tag: {m_Camera.tag}, RenderType: Base)");
                Log.Info($"UI相机已设置为Overlay模式并添加到cameraStack (RenderType: Overlay)");
                Log.Info($"相机堆叠配置完成 - Base相机: {m_Camera.name}, Overlay相机: {uiCamera.name}");
            }
            else
            {
                // 如果没有UI相机，设置默认值
                m_Camera.tag = "MainCamera";
                m_Camera.orthographic = true;
                m_Camera.clearFlags = CameraClearFlags.SolidColor;
                m_Camera.backgroundColor = Color.black;
                
                var gameCameraData = m_Camera.GetUniversalAdditionalCameraData();
                gameCameraData.renderType = CameraRenderType.Base;
                
                Log.Warning("未找到UI相机，玩法相机已设置为主相机，使用默认设置");
            }

            EnsurePlayBgWallpaper();
        }

        /// <summary>
        /// 设置界面可显示范围（varCameraView），用于适配 orthographicSize 与移动边界。
        /// 应在 SetupForLevel 之前由 UI 界面调用。
        /// </summary>
        public void SetCameraViewRect(RectTransform cameraViewRect)
        {
            m_CameraViewRect = cameraViewRect;
        }

        /// <summary>
        /// 日间背景色 #FEEB9F
        /// </summary>
        public static readonly Color DayBgColor = new Color(254f / 255f, 235f / 255f, 159f / 255f, 1f);

        /// <summary>
        /// 夜间背景色 #232633
        /// </summary>
        public static readonly Color NightBgColor = new Color(0.137f, 0.149f, 0.2f, 1f);

        /// <summary>
        /// 设置玩法相机日/夜间：壁纸同图 tint，清屏色作缝隙兜底。true=夜间，false=日间。
        /// </summary>
        public void SetDayNightMode(bool isNightMode)
        {
            if (m_Camera == null) return;
            EnsurePlayBgWallpaper();
            m_Camera.backgroundColor = isNightMode ? NightBgColor : PlayBgWallpaper.DayClearColor;
            m_PlayBgWallpaper?.SetDayNightMode(isNightMode);
        }

        /// <summary>
        /// 地图初始缩放 scale（相对 referenceSize，scale=1 为基准）。
        /// 配置表键名：LevelInitialMapScale（可选，兼容旧键 LevelMaxZoomInScale）。
        /// </summary>
        [SerializeField]
        private float m_initialMaxZoomInScale = 1.5f;

        /// <summary>
        /// 地图可缩放变动档位数（min→max 共 N 档，UI 步进与双指范围均据此计算）。
        /// 配置表键名：LevelZoomVariationCount（可选）。
        /// </summary>
        [SerializeField]
        private int m_ZoomVariationCount = 5;

        /// <summary> 当前关卡允许的最小地图 scale（地图显示最小 / 最远 zoom out） </summary>
        private float m_LevelMinMapScale = 1f;
        /// <summary> 当前关卡允许的最大地图 scale（地图显示最大 / 最近 zoom in） </summary>
        private float m_LevelMaxMapScale = 1f;
        /// <summary> 当前关卡初始地图 scale </summary>
        private float m_LevelInitialMapScale = 1f;

        /// <summary>
        /// 小地图开局至少占满可视区域的比例（线性尺寸）。不足则放大到该比例。
        /// </summary>
        private const float MinInitialViewFillRatio = 0.75f;

        /// <summary>
        /// 根据关卡网格边界设置相机初始缩放与双指/滚轮缩放范围。
        /// <para><b>scale</b>：相对 m_ReferenceOrthographicSize，scale=1 为基准；越大地图越大。</para>
        /// <para><b>初始 scale</b> = 配置初始值，且不超过 maxScaleFullyVisible（保证开局完整可见）；
        /// 若仍不足可视区的 3/4，则放大到恰好占满 3/4。</para>
        /// <para><b>最小 scale</b>：初始 &gt;= 1 时为 1；初始 &lt; 1 时为初始值（不可再缩小）。</para>
        /// <para><b>最大 scale</b>：从初始向放大方向扩展 (变动档位数-1) 步，步长见 <see cref="CalculateMapScaleStep"/>。</para>
        /// </summary>
        public void SetupForLevel(string levelName, Bounds gridBounds, int gridCols = -1, int gridRows = -1)
        {
            if (m_Camera == null || (gridBounds.size.x <= 0f && gridBounds.size.y <= 0f))
            {
                Log.Warning("无法设置关卡相机：相机为空或网格边界无效");
                return;
            }

            m_GridBounds = gridBounds;
            m_GridBounds.Expand(new Vector3(m_GridBoundsPaddingX, m_GridBoundsPaddingY, 0f));

            float gridWidth = m_GridBounds.size.x;
            float gridHeight = m_GridBounds.size.y;
            float referenceSize = GetReferenceOrthographicSize();
            float fitSize = CalculateFitOrthographicSize(gridWidth, gridHeight);
            float configuredInitialScale = GetInitialMapScale();
            int variationCount = GetZoomVariationCount();
            // scale=maxScaleFullyVisible 时地图刚好铺满整个可视区
            float maxScaleFullyVisible = referenceSize / Mathf.Max(fitSize, 0.0001f);

            float initialMapScale = Mathf.Min(configuredInitialScale, maxScaleFullyVisible);
            // 小地图：若开局占不满可视区的 3/4，放大到 3/4（仍不超过完整可见上限）
            float minFillScale = maxScaleFullyVisible * MinInitialViewFillRatio;
            if (initialMapScale < minFillScale)
                initialMapScale = Mathf.Min(minFillScale, maxScaleFullyVisible);

            m_LevelInitialMapScale = initialMapScale;
            m_LevelMinMapScale = initialMapScale >= 1f ? 1f : initialMapScale;

            float scaleStep = CalculateMapScaleStep(initialMapScale, m_LevelMinMapScale, variationCount);
            m_LevelMaxMapScale = initialMapScale + (variationCount - 1) * scaleStep;
            if (m_LevelMaxMapScale < m_LevelMinMapScale)
                m_LevelMaxMapScale = m_LevelMinMapScale;

            float minOrtho = referenceSize / m_LevelMaxMapScale;
            float maxOrtho = referenceSize / m_LevelMinMapScale;
            m_MinSize = Mathf.Min(minOrtho, maxOrtho);
            m_MaxSize = Mathf.Max(minOrtho, maxOrtho);

            m_TargetZoom = Mathf.Clamp(referenceSize / initialMapScale, m_MinSize, m_MaxSize);
            m_EntranceEndSize = m_TargetZoom;

            Log.Info($"当前关卡：{levelName}  基准：{referenceSize:F2}  适配：{fitSize:F2}  配置初始scale：{configuredInitialScale:F2}  " +
                $"实际初始：{initialMapScale:F2}  可视占比：{(initialMapScale / maxScaleFullyVisible):P0}  可缩放scale：[{m_LevelMinMapScale:F2}, {m_LevelMaxMapScale:F2}]  档位数：{variationCount}  步长：{scaleStep:F3}");

            bool playEntrance = configuredInitialScale > initialMapScale + 0.01f &&
                Mathf.Abs(m_EntranceEndSize - fitSize * 1.06f) > 0.02f;
            if (playEntrance)
            {
                m_EntranceStartSize = Mathf.Clamp(fitSize * 1.06f, m_MinSize, m_MaxSize);
                m_EntranceElapsed = 0f;
                m_EntranceAnimating = true;
                m_Camera.orthographicSize = m_EntranceStartSize;
            }
            else
            {
                m_EntranceStartSize = m_TargetZoom;
                m_EntranceAnimating = false;
                m_Camera.orthographicSize = m_TargetZoom;
            }

            CalculatePanLimits();

            Vector3 centerWithOffset = m_GridBounds.center + (Vector3)GetCameraViewOffset(m_EntranceEndSize);
            FocusOnGridCenter(centerWithOffset, -1f);

            Log.Info($"相机适配完成 - Target: {m_TargetZoom:F2}, Min: {m_MinSize:F2}, Max: {m_MaxSize:F2}，入场 {(m_EntranceAnimating ? EntranceDuration : 0):F1}s");
            Log.Info($"网格尺寸 - Width: {gridWidth:F2}, Height: {gridHeight:F2}");
            Log.Info($"有效宽高比 - EffectiveAspect: {GetEffectiveAspect():F3}");
            Log.Info($"相机中心可移动范围 - Min: {m_CameraCenterMin}, Max: {m_CameraCenterMax}");
        }

        /// <summary>
        /// 固定步长
        /// </summary>
        private static float CalculateMapScaleStep(float initialScale, float minScale, int variationCount)
        {
            return 0.5f;
        }

        /// <summary> 当前关卡地图 scale（相对 referenceSize），scale 越大地图显示越大。 </summary>
        public float GetCurrentMapScale()
        {
            float referenceSize = GetReferenceOrthographicSize();
            if (referenceSize <= 0f || m_Camera == null)
                return 1f;
            return referenceSize / m_Camera.orthographicSize;
        }

        /// <summary> 当前关卡初始地图 scale。 </summary>
        public float GetLevelInitialMapScale() => m_LevelInitialMapScale;

        /// <summary> 当前关卡允许的最小地图 scale（最远 zoom out）。 </summary>
        public float GetLevelMinMapScale() => m_LevelMinMapScale;

        /// <summary> 当前关卡允许的最大地图 scale（最近 zoom in）。 </summary>
        public float GetLevelMaxMapScale() => m_LevelMaxMapScale;

        /// <summary>
        /// 读取地图初始 scale
        /// </summary>
        private float GetInitialMapScale()
        {
            return Mathf.Max(0.01f, m_initialMaxZoomInScale);
        }

        /// <summary>
        /// 读取缩放变动档位数
        /// </summary>
        private int GetZoomVariationCount()
        {
            return Mathf.Max(2, m_ZoomVariationCount);
        }

        /// <summary>
        /// 基准 orthographicSize（scale=1）。
        /// Inspector 中 m_ReferenceOrthographicSize &gt; 0 时使用该值，否则用相机当前 orthographicSize。
        /// </summary>
        private float GetReferenceOrthographicSize()
        {
            if (m_ReferenceOrthographicSize > 0f)
                return m_ReferenceOrthographicSize;
            return m_Camera != null ? m_Camera.orthographicSize : 5f;
        }

        /// <summary>
        /// 计算使网格完整落在 varCameraView 内所需的最小 orthographicSize。
        /// 固定间距下，地图越大 fitSize 越大（需拉远相机才能看全）。
        /// </summary>
        private float CalculateFitOrthographicSize(float gridWidth, float gridHeight)
        {
            float effectiveAspect = GetEffectiveAspect();
            float sizeByHeight = gridHeight * 0.5f;
            float sizeByWidth = (gridWidth / effectiveAspect) * 0.5f;
            float fitSize = Mathf.Max(sizeByHeight, sizeByWidth);
            fitSize *= GetViewHeightCoverageFactor();
            return fitSize;
        }

        private float GetViewHeightCoverageFactor()
        {
            if (TryGetCameraViewSizeInScreenPixels(out _, out float viewH) && viewH > 0.001f && Screen.height > 0.001f)
                return Screen.height / viewH;
            if (m_CameraViewRect == null || Screen.height <= 0.001f)
                return 1f;
            Canvas canvas = m_CameraViewRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return 1f;
            float viewHFromRect = m_CameraViewRect.rect.height * canvas.scaleFactor;
            return viewHFromRect > 0.001f ? Screen.height / viewHFromRect : 1f;
        }

        /// <summary>
        /// 获取有效宽高比（varCameraView 或屏幕）
        /// </summary>
        private float GetEffectiveAspect()
        {
            if (m_CameraViewRect != null)
            {
                Rect r = m_CameraViewRect.rect;
                if (r.height > 0.001f)
                    return r.width / r.height;
            }
            return (float)Screen.width / Screen.height;
        }

        /// <summary>
        /// 获取 varCameraView 在屏幕上的像素尺寸 (width, height)，用于计算 baseSize 的视口占比
        /// </summary>
        private bool TryGetCameraViewSizeInScreenPixels(out float widthPx, out float heightPx)
        {
            widthPx = heightPx = 0f;
            if (m_CameraViewRect == null) return false;
            Canvas canvas = m_CameraViewRect.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            Vector3[] corners = new Vector3[4];
            m_CameraViewRect.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                corners[2]);
            widthPx = Mathf.Abs(tr.x - bl.x);
            heightPx = Mathf.Abs(tr.y - bl.y);
            return widthPx > 0.001f && heightPx > 0.001f;
        }

        /// <summary>
        /// 获取相机偏移，使网格中心显示在 varCameraView 中心（屏幕中心与 varCameraView 中心不一致时）
        /// </summary>
        private Vector2 GetCameraViewOffset(float orthographicSize)
        {
            if (m_CameraViewRect == null || m_Camera == null)
                return Vector2.zero;

            Canvas canvas = m_CameraViewRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return Vector2.zero;

            Vector3[] corners = new Vector3[4];
            m_CameraViewRect.GetWorldCorners(corners);
            Vector3 centerWorld = (corners[0] + corners[2]) * 0.5f;
            Vector2 viewCenterScreen = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                centerWorld);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float worldUnitsPerPixel = (orthographicSize * 2f) / Screen.height;
            return new Vector2((screenCenter.x - viewCenterScreen.x) * worldUnitsPerPixel, (screenCenter.y - viewCenterScreen.y) * worldUnitsPerPixel);
        }

        /// <summary>
        /// 根据当前 orthographicSize 与 m_GridBounds 计算相机中心的可移动范围。
        /// 使用 varCameraView 的有效显示范围（varCameraView 外被不透明 UI 遮挡），即用户实际可见的世界空间视口尺寸。
        /// m_GridBounds.min/max 须为世界空间真实下界/上界（由 GetGridBounds 保证），否则 Y 会反、导致无法上下移动。
        /// </summary>
        private void CalculatePanLimits()
        {
            if (m_Camera == null || m_GridBounds.size.magnitude <= 0)
            {
                m_CameraCenterMin = Vector2.zero;
                m_CameraCenterMax = Vector2.zero;
                return;
            }

            float cameraHeight = m_Camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * m_Camera.aspect;
            float halfW = cameraWidth * 0.5f;
            float halfH = cameraHeight * 0.5f;
            // varCameraView 在屏幕上的占比，用户实际可见的世界空间视口小于全屏时需缩小 half 值以扩大平移范围
            if (TryGetCameraViewSizeInScreenPixels(out float viewW, out float viewH))
            {
                halfW *= viewW / Screen.width;
                halfH *= viewH / Screen.height;
            }
            float safetyZone = Mathf.Max(0f, m_GridEdgeSafetyZone);
            // 安全区：允许相机平移超出网格边界，以便观察可能超出网格边缘的箭头
            m_CameraCenterMin = new Vector2(
                m_GridBounds.min.x + halfW - safetyZone,
                m_GridBounds.min.y + halfH - safetyZone
            );
            m_CameraCenterMax = new Vector2(
                m_GridBounds.max.x - halfW + safetyZone,
                m_GridBounds.max.y - halfH + safetyZone
            );
            // 视口大于网格时收紧为网格中心，避免单轴锁死产生歧义
            if (m_CameraCenterMin.x > m_CameraCenterMax.x)
            {
                float cx = (m_GridBounds.min.x + m_GridBounds.max.x) * 0.5f;
                m_CameraCenterMin.x = m_CameraCenterMax.x = cx;
            }
            if (m_CameraCenterMin.y > m_CameraCenterMax.y)
            {
                float cy = (m_GridBounds.min.y + m_GridBounds.max.y) * 0.5f;
                m_CameraCenterMin.y = m_CameraCenterMax.y = cy;
            }
            // 叠加 varCameraView 偏移，使平移边界与初始位置一致，避免首次拖拽时偏移被 clamp 掉
            Vector2 viewOffset = GetCameraViewOffset(m_Camera.orthographicSize);
            m_CameraCenterMin += viewOffset;
            m_CameraCenterMax += viewOffset;
        }

        /// <summary>
        /// 指针是否在 UI 上（弹窗、按钮等），用于避免玩法输入穿透
        /// </summary>
        private static bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            return UtilityEx.IsPointerOverUIObject(screenPosition);
        }

        /// <summary>
        /// 处理双指捏合缩放（移动端）
        /// </summary>
        private void HandlePinchZoom()
        {
            if (!m_EnableZoom || m_Camera == null) return;

            // 检查双指触摸（捏合手势）
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                if (IsPointerOverUI(touch0.position) || IsPointerOverUI(touch1.position))
                    return;
                m_IsDragging = false; // 缩放时禁用平移

                // 获取上一帧的触摸位置
                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                // 计算距离
                float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                float touchDeltaMag = (touch0.position - touch1.position).magnitude;

                // 计算缩放增量（像素差 → orthographicSize，与帧率无关）
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
                float range = m_MaxSize - m_MinSize;
                if (range > 0f && Screen.height > 0)
                {
                    m_TargetZoom += deltaMagnitudeDiff * (range / Screen.height) * m_ZoomSpeed;
                    m_TargetZoom = Mathf.Clamp(m_TargetZoom, m_MinSize, m_MaxSize);
                }
            }
        }

        /// <summary>
        /// 处理鼠标滚轮缩放（桌面端）。用 m_ScrollWheelStep 表示每 notch 占 Min~Max 区间的比例，避免灵敏度异常。
        /// </summary>
        private void HandleMouseScrollZoom()
        {
            if (!m_EnableZoom || m_Camera == null) return;
            if (IsPointerOverUI(Input.mousePosition)) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                float range = m_MaxSize - m_MinSize;
                float step = range * Mathf.Clamp(m_ScrollWheelStep, 0.01f, 0.5f);
                m_TargetZoom -= scroll * step * 10f; // scroll 约 ±0.1 per notch
                m_TargetZoom = Mathf.Clamp(m_TargetZoom, m_MinSize, m_MaxSize);
            }
        }

        /// <summary>
        /// 处理平移输入
        /// </summary>
        private void HandlePanInput()
        {
            if (!m_EnablePan || m_Camera == null) return;

            // 处理触摸输入
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    if (IsPointerOverUI(touch.position)) return;
                    s_DidPanThisTouch = false;
                    m_IsDragging = true;
                    m_LastTouchPosition = GetWorldPosition(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved && m_IsDragging && touch.deltaPosition.magnitude > 10f)
                {
                    Vector3 currentTouchPosition = GetWorldPosition(touch.position);
                    // 取反：手指滑动方向与场景运动方向一致（手指向上滑则场景向上移）
                    Vector3 delta = m_LastTouchPosition - currentTouchPosition;

                    Vector3 newPosition = transform.position + delta;

                    if (m_EnablePanLimits)
                    {
                        newPosition.x = Mathf.Clamp(newPosition.x, m_CameraCenterMin.x, m_CameraCenterMax.x);
                        newPosition.y = Mathf.Clamp(newPosition.y, m_CameraCenterMin.y, m_CameraCenterMax.y);
                    }

                    transform.position = newPosition;
                    s_DidPanThisTouch = true;
                    m_LastTouchPosition = GetWorldPosition(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    m_IsDragging = false;
                }
            }

            // 处理鼠标输入（桌面测试）
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI(Input.mousePosition)) return;
                s_DidPanThisTouch = false;
                m_IsDragging = true;
                m_LastTouchPosition = GetWorldPosition(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && m_IsDragging)
            {
                Vector3 currentMousePosition = GetWorldPosition(Input.mousePosition);
                // 取反：拖拽方向与场景运动方向一致
                Vector3 delta = m_LastTouchPosition - currentMousePosition;

                Vector3 newPosition = transform.position + delta;

                if (m_EnablePanLimits)
                {
                    newPosition.x = Mathf.Clamp(newPosition.x, m_CameraCenterMin.x, m_CameraCenterMax.x);
                    newPosition.y = Mathf.Clamp(newPosition.y, m_CameraCenterMin.y, m_CameraCenterMax.y);
                }

                transform.position = newPosition;
                s_DidPanThisTouch = true;
                m_LastTouchPosition = GetWorldPosition(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_IsDragging = false;
            }
        }

        /// <summary>
        /// 应用平滑缩放
        /// </summary>
        private void ApplySmoothZoom()
        {
            if (m_Camera == null || m_EntranceAnimating) return;

            // 捏合时即时跟手，滚轮/按钮缩放仍平滑插值
            float previousSize = m_Camera.orthographicSize;
            if (Input.touchCount == 2)
                m_Camera.orthographicSize = m_TargetZoom;
            else
                m_Camera.orthographicSize = Mathf.Lerp(m_Camera.orthographicSize, m_TargetZoom, Time.deltaTime * 15f);
            
            // 缩放时更新平移限制（如果缩放值有明显变化）
            if (Mathf.Abs(previousSize - m_Camera.orthographicSize) > 0.01f)
            {
                CalculatePanLimits();
                
                // 确保当前相机位置在限制范围内
                Vector3 currentPos = transform.position;
                if (m_EnablePanLimits)
                {
                    currentPos.x = Mathf.Clamp(currentPos.x, m_CameraCenterMin.x, m_CameraCenterMax.x);
                    currentPos.y = Mathf.Clamp(currentPos.y, m_CameraCenterMin.y, m_CameraCenterMax.y);
                    transform.position = currentPos;
                }
            }
        }

        /// <summary>
        /// 屏幕坐标转世界坐标（用于平移）。用射线与 Z=0 平面求交，保证正交相机下 X/Y 与拖拽方向一致。
        /// </summary>
        private Vector3 GetWorldPosition(Vector3 screenPosition)
        {
            Ray ray = m_Camera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 p = ray.GetPoint(enter);
                p.z = transform.position.z;
                return p;
            }
            return transform.position;
        }

        /// <summary>
        /// 设置相机大小（兼容旧接口）
        /// </summary>
        public void SetCameraSize(float size)
        {
            if (m_Camera != null)
            {
                m_TargetZoom = Mathf.Clamp(size, m_MinSize, m_MaxSize);
                m_Camera.orthographicSize = m_TargetZoom;
            }
        }

        /// <summary> 地图可缩小到的最小缩放进度（0=最远/最小显示） </summary>
        public float ZoomProgressMin => 0f;

        /// <summary> 地图可放大到的最大缩放进度（1=最近/最大显示） </summary>
        public float ZoomProgressMax => 1f;

        /// <summary> 缩放进度步长（总进度的一档，档位数见 m_ZoomVariationCount） </summary>
        public float ZoomProgressStep => 1f / Mathf.Max(GetZoomVariationCount() - 1, 1);

        /// <summary> 缩放变动档位数 </summary>
        public int ZoomVariationCount => GetZoomVariationCount();

        /// <summary> 当前缩放进度，0=最远，1=最近 </summary>
        public float GetZoomProgress()
        {
            float range = m_MaxSize - m_MinSize;
            if (range <= 0f) return 0f;
            float zoom = m_Camera != null ? m_Camera.orthographicSize : m_TargetZoom;
            return Mathf.Clamp01((m_MaxSize - zoom) / range);
        }

        /// <summary> 按缩放进度设置相机（0=最远，1=最近） </summary>
        public void SetZoomProgress(float progress, bool immediate = false)
        {
            if (!m_EnableZoom || m_Camera == null) return;
            float range = m_MaxSize - m_MinSize;
            if (range <= 0f) return;
            progress = Mathf.Clamp(progress, ZoomProgressMin, ZoomProgressMax);
            m_TargetZoom = m_MaxSize - progress * range;
            m_TargetZoom = Mathf.Clamp(m_TargetZoom, m_MinSize, m_MaxSize);
            if (immediate)
            {
                m_Camera.orthographicSize = m_TargetZoom;
                CalculatePanLimits();
            }
        }

        /// <summary> 放大一档（进度增加总范围的五分之一；非整步时取下一档） </summary>
        public void AddZoom()
        {
            if (!m_EnableZoom || m_Camera == null) return;
            float min = ZoomProgressMin;
            float max = ZoomProgressMax;
            float step = ZoomProgressStep;
            float current = GetZoomProgress();
            float target = min + Mathf.Ceil((current - min + 1e-6f) / step) * step;
            if (Mathf.Approximately(target, current))
                target += step;
            SetZoomProgress(Mathf.Min(target, max));
        }

        /// <summary> 缩小一档（进度减少总范围的五分之一；非整步时取上一档） </summary>
        public void SubZoom()
        {
            if (!m_EnableZoom || m_Camera == null) return;
            float min = ZoomProgressMin;
            float max = ZoomProgressMax;
            float step = ZoomProgressStep;
            float current = GetZoomProgress();
            float target = min + Mathf.Floor((current - min - 1e-6f) / step) * step;
            if (Mathf.Approximately(target, current))
                target -= step;
            SetZoomProgress(Mathf.Max(target, min));
        }

        /// <summary> 当前缩放在 Min~Max 中的比例，0=最小(最远)，1=最大(最近)，用于进度条 fillAmount </summary>
        public float GetZoomProportion()
        {
            return GetZoomProgress();
        }

        /// <summary> 缩放百分比，0~100，100 表示最近（Max），0 表示最远（Min） </summary>
        public float GetZoomPercent()
        {
            return GetZoomProportion() * 100f;
        }

        /// <summary>
        /// 设置网格边界（用于限制相机移动范围）
        /// </summary>
        public void SetGridBounds(Bounds bounds)
        {
            m_GridBounds = bounds;
            CalculatePanLimits();
        }

        /// <summary>
        /// 聚焦到指定位置
        /// </summary>
        public void FocusOn(Vector3 worldPos, float size = -1)
        {
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            
            if (size > 0)
            {
                m_TargetZoom = Mathf.Clamp(size, m_MinSize, m_MaxSize);
                m_Camera.orthographicSize = m_TargetZoom;
            }
        }

        /// <summary>
        /// 聚焦到网格中心
        /// </summary>
        public void FocusOnGridCenter(Vector3 center, float size = -1)
        {
            FocusOn(center, size);
        }

        /// <summary>
        /// 重置相机（用于新关卡）
        /// </summary>
        public void ResetCamera(Vector3 position, float zoom)
        {
            transform.position = position;
            m_TargetZoom = Mathf.Clamp(zoom, m_MinSize, m_MaxSize);
            m_Camera.orthographicSize = m_TargetZoom;
        }
    }
}
