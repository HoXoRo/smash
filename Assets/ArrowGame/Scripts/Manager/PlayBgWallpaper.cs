using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// 玩法固定壁纸：挂在玩法相机下，Cover 铺满屏幕，不随关卡拖动相对屏幕移动。
    /// </summary>
    public sealed class PlayBgWallpaper : MonoBehaviour
    {
        public const string DefaultSpriteAsset = "Bg/bg.jpg";
        const int SortingOrder = -100;
        const float LocalZ = 10f;
        /// <summary> 0=居中裁切，0.5=贴顶保留上沿装饰 </summary>
        const float VerticalBias = 0.2f;

        static readonly Color NightTint = new Color(0.42f, 0.45f, 0.55f, 1f);
        /// <summary> 缝隙兜底色（接近草地） </summary>
        public static readonly Color DayClearColor = new Color(0.55f, 0.78f, 0.42f, 1f);

        SpriteRenderer m_Renderer;
        Sprite m_Sprite;
        float m_LastOrthoSize = -1f;
        float m_LastAspect = -1f;
        bool m_IsNight;
        bool m_Loading;

        public static PlayBgWallpaper Ensure(Camera camera)
        {
            if (camera == null)
                return null;

            var existing = camera.GetComponentInChildren<PlayBgWallpaper>(true);
            if (existing != null)
            {
                existing.EnsureInitialized();
                return existing;
            }

            var go = new GameObject("PlayBg");
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, LocalZ);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            ApplyPlayBgLayer(go, camera);

            var wallpaper = go.AddComponent<PlayBgWallpaper>();
            wallpaper.EnsureInitialized();
            return wallpaper;
        }

        /// <summary>
        /// 玩法相机只渲染 WorldUI（及可选 Bg）。Default 层会被裁掉导致只剩清屏色。
        /// </summary>
        static void ApplyPlayBgLayer(GameObject go, Camera camera)
        {
            int bgLayer = LayerMask.NameToLayer("Bg");
            int worldUiLayer = LayerMask.NameToLayer("WorldUI");
            int layer = bgLayer >= 0 ? bgLayer : (worldUiLayer >= 0 ? worldUiLayer : 3);
            go.layer = layer;
            if (camera != null)
                camera.cullingMask |= 1 << layer;
        }

        public void EnsureInitialized()
        {
            if (m_Renderer == null)
            {
                m_Renderer = GetComponent<SpriteRenderer>();
                if (m_Renderer == null)
                    m_Renderer = gameObject.AddComponent<SpriteRenderer>();
                m_Renderer.sortingOrder = SortingOrder;
                m_Renderer.color = m_IsNight ? NightTint : Color.white;
            }

            var parentCam = transform.parent != null ? transform.parent.GetComponent<Camera>() : null;
            ApplyPlayBgLayer(gameObject, parentCam);

            if (m_Sprite == null && !m_Loading)
                BeginLoadSprite();
            else if (m_Sprite != null)
                ApplySprite(m_Sprite);
        }

        public void SetDayNightMode(bool isNightMode)
        {
            m_IsNight = isNightMode;
            if (m_Renderer != null)
                m_Renderer.color = isNightMode ? NightTint : Color.white;
        }

        public void RefreshCoverIfNeeded(Camera camera)
        {
            if (camera == null || m_Renderer == null || m_Sprite == null)
                return;

            float ortho = camera.orthographicSize;
            float aspect = camera.aspect;
            if (Mathf.Approximately(ortho, m_LastOrthoSize) && Mathf.Approximately(aspect, m_LastAspect))
                return;

            m_LastOrthoSize = ortho;
            m_LastAspect = aspect;
            ApplyCover(ortho, aspect);
        }

        void LateUpdate()
        {
            var cam = transform.parent != null ? transform.parent.GetComponent<Camera>() : null;
            if (cam == null)
                cam = Camera.main;
            RefreshCoverIfNeeded(cam);
        }

        void BeginLoadSprite()
        {
            m_Loading = true;
            string path = UtilityBuiltin.AssetsPath.GetSpritesPath(DefaultSpriteAsset);
            if (GF.UI == null)
            {
                m_Loading = false;
                Log.Warning("PlayBgWallpaper: GF.UI 不可用，无法加载 {0}", path);
                return;
            }

            GF.UI.LoadSprite(path, sprite =>
            {
                m_Loading = false;
                if (this == null)
                    return;
                if (sprite == null)
                {
                    Log.Warning("PlayBgWallpaper: 加载失败 {0}", path);
                    return;
                }
                ApplySprite(sprite);
            });
        }

        void ApplySprite(Sprite sprite)
        {
            m_Sprite = sprite;
            if (m_Renderer == null)
                return;

            m_Renderer.sprite = sprite;
            m_LastOrthoSize = -1f;
            m_LastAspect = -1f;

            var cam = transform.parent != null ? transform.parent.GetComponent<Camera>() : null;
            if (cam != null)
                RefreshCoverIfNeeded(cam);
        }

        void ApplyCover(float orthographicSize, float aspect)
        {
            if (m_Sprite == null)
                return;

            float viewH = orthographicSize * 2f;
            float viewW = viewH * Mathf.Max(aspect, 0.0001f);

            Vector2 spriteSize = m_Sprite.bounds.size;
            if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f)
                return;

            float scale = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            float excessH = spriteSize.y * scale - viewH;
            float localY = excessH > 0f ? -excessH * VerticalBias : 0f;
            transform.localPosition = new Vector3(0f, localY, LocalZ);
        }
    }
}
