using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 刮刮卡涂层擦除：通过遮罩纹理控制 coat 透明度，支持手指刮开与笔刷轨迹一键擦除。
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ScratchCardCoatEraser : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler
{
    static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");

    [SerializeField] Shader m_ScratchShader;
    [SerializeField] int m_MaskWidth = 512;
    [SerializeField] int m_MaskHeight = 512;
    [SerializeField] float m_BrushRadius = 36f;
    [SerializeField] float m_AutoRevealDuration = 0.35f;
    [SerializeField] float m_AutoRevealCompleteThreshold = 0.995f;
    [SerializeField] float m_ManualAutoCompleteThreshold = 0.70f;
    const string AutoErasureSoundPath = "arrowPlay/auto_erasure.mp3";
    const string ErasureSoundPath = "arrowPlay/erasure.mp3";
    const float ScratchDirectionChangeDotThreshold = 0.707106781f;
    static Shader s_FallbackScratchShader;

    float BrushStep => m_BrushRadius * 0.45f;

    Image m_Image;
    RectTransform m_RectTransform;
    Material m_Material;
    Texture2D m_MaskTexture;
    Color32[] m_MaskPixels;
    bool m_Initialized;
    bool m_InputEnabled = true;
    bool m_IsRevealing;
    bool m_IsFingerScratching;
    Vector2? m_LastScratchLocalPoint;
    Vector2? m_LastScratchMoveDirection;
    Coroutine m_AutoRevealCoroutine;
    readonly List<AutoRevealStroke> m_AutoRevealStrokes = new List<AutoRevealStroke>(32);
    Vector2? m_AutoRevealLastPoint;
    int m_AutoRevealLastStrokeIndex = -1;

    public bool IsFullyRevealed { get; private set; }
    public float RevealProgress { get; private set; }
    public bool IsInitialized => m_Initialized;

    public event Action RevealCompleted;

    public void Initialize()
    {
        if (m_Initialized)
        {
            ResetCoat();
            return;
        }

        m_Image = GetComponent<Image>();
        m_RectTransform = transform as RectTransform;
        m_Image.raycastTarget = true;
        transform.SetAsLastSibling();

        Shader shader = ResolveScratchShader();
        if (shader == null)
        {
            Debug.LogError("ScratchCardCoatEraser: shader UI/ScratchCard not found.");
            return;
        }

        m_Material = new Material(shader);
        m_Image.material = m_Material;

        m_MaskWidth = Mathf.Max(32, m_MaskWidth);
        m_MaskHeight = Mathf.Max(32, m_MaskHeight);
        m_MaskTexture = new Texture2D(m_MaskWidth, m_MaskHeight, TextureFormat.RGBA32, false);
        m_MaskTexture.wrapMode = TextureWrapMode.Clamp;
        m_MaskTexture.filterMode = FilterMode.Bilinear;
        m_MaskPixels = new Color32[m_MaskWidth * m_MaskHeight];
        m_Material.SetTexture(MaskTexId, m_MaskTexture);

        m_Initialized = true;
        ResetCoat();
    }

    public void ResetCoat()
    {
        if (!m_Initialized || m_MaskPixels == null)
            return;

        if (m_AutoRevealCoroutine != null)
        {
            StopCoroutine(m_AutoRevealCoroutine);
            m_AutoRevealCoroutine = null;
        }

        FillMaskOpaque();
        m_LastScratchLocalPoint = null;
        ResetFingerScratchDirection();
        ResetAutoRevealScratchState();
        m_IsFingerScratching = false;
        m_InputEnabled = true;
        m_IsRevealing = false;
        IsFullyRevealed = false;
        RevealProgress = 0f;
        gameObject.SetActive(true);
    }

    public void SetInputEnabled(bool enabled)
    {
        m_InputEnabled = enabled && !IsFullyRevealed;
    }

    /// <summary> 一键擦除：沿左上到右下方向，使用与手指相同的圆形笔刷轨迹揭开涂层。 </summary>
    public void RevealLeftToRight(Action onComplete = null)
    {
        if (!m_Initialized || IsFullyRevealed)
        {
            onComplete?.Invoke();
            return;
        }

        if (m_AutoRevealCoroutine != null)
            StopCoroutine(m_AutoRevealCoroutine);

        m_InputEnabled = false;
        m_IsRevealing = true;
        GF.Sound.PlayEffect(AutoErasureSoundPath);
        m_AutoRevealCoroutine = StartCoroutine(AutoRevealDiagonalCoroutine(onComplete));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanScratch())
            return;

        if (TryGetLocalPoint(eventData, out Vector2 localPoint))
        {
            m_IsFingerScratching = true;
            ResetFingerScratchDirection();
            m_LastScratchLocalPoint = localPoint;
            ScratchAt(localPoint, true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!m_Initialized || IsFullyRevealed || m_IsRevealing)
            return;

        bool wasScratching = m_IsFingerScratching;
        m_IsFingerScratching = false;
        m_LastScratchLocalPoint = null;
        ResetFingerScratchDirection();

        if (!wasScratching || !m_InputEnabled)
            return;

        RevealProgress = CalculateRevealProgress();
        if (RevealProgress >= m_ManualAutoCompleteThreshold)
            RevealLeftToRight();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanScratch())
            return;

        m_IsFingerScratching = true;
        ResetFingerScratchDirection();
        if (TryGetLocalPoint(eventData, out Vector2 localPoint))
            m_LastScratchLocalPoint = localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanScratch())
            return;

        if (!TryGetLocalPoint(eventData, out Vector2 localPoint))
            return;

        if (m_LastScratchLocalPoint.HasValue)
        {
            TryPlayErasureSoundOnDirectionChange(m_LastScratchLocalPoint.Value, localPoint);
            ScratchLine(m_LastScratchLocalPoint.Value, localPoint, true);
        }
        else
            ScratchAt(localPoint, true);

        m_LastScratchLocalPoint = localPoint;
    }

    bool CanScratch()
    {
        return m_Initialized && m_InputEnabled && !m_IsRevealing && !IsFullyRevealed;
    }

    bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        Camera eventCamera = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_RectTransform, eventData.position, eventCamera, out localPoint);
    }

    static Shader ResolveScratchShader(Shader serializedShader = null)
    {
        if (serializedShader != null)
            return serializedShader;

        if (s_FallbackScratchShader == null)
            s_FallbackScratchShader = Shader.Find("UI/ScratchCard");

        return s_FallbackScratchShader;
    }

    Shader ResolveScratchShader()
    {
        return ResolveScratchShader(m_ScratchShader);
    }

    void ResetFingerScratchDirection()
    {
        m_LastScratchMoveDirection = null;
    }

    void TryPlayErasureSoundOnDirectionChange(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float minDistanceSqr = BrushStep * BrushStep;
        if (delta.sqrMagnitude < minDistanceSqr)
            return;

        Vector2 direction = delta.normalized;
        if (!m_LastScratchMoveDirection.HasValue)
        {
            m_LastScratchMoveDirection = direction;
            return;
        }

        if (Vector2.Dot(m_LastScratchMoveDirection.Value, direction) >= ScratchDirectionChangeDotThreshold)
            return;

        m_LastScratchMoveDirection = direction;
        // GF.Sound.PlayEffect(ErasureSoundPath);
    }

    void ScratchLine(Vector2 from, Vector2 to, bool applyImmediately)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / BrushStep));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            ScratchAt(Vector2.Lerp(from, to, t), false);
        }

        if (applyImmediately)
            FlushScratchChanges();
    }

    void ScratchAt(Vector2 localPoint, bool applyImmediately)
    {
        Rect rect = m_RectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float normalizedX = (localPoint.x - rect.xMin) / rect.width;
        float normalizedY = (localPoint.y - rect.yMin) / rect.height;
        if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
            return;

        int centerX = Mathf.RoundToInt(normalizedX * (m_MaskWidth - 1));
        int centerY = Mathf.RoundToInt(normalizedY * (m_MaskHeight - 1));
        PaintCircle(centerX, centerY, m_BrushRadius);

        if (applyImmediately)
            FlushScratchChanges();
    }

    void FlushScratchChanges()
    {
        ApplyMaskChanges();
        UpdateRevealProgress();
    }

    void PaintCircle(int centerX, int centerY, float radius)
    {
        int radiusInt = Mathf.CeilToInt(radius);
        int minX = Mathf.Max(0, centerX - radiusInt);
        int maxX = Mathf.Min(m_MaskWidth - 1, centerX + radiusInt);
        int minY = Mathf.Max(0, centerY - radiusInt);
        int maxY = Mathf.Min(m_MaskHeight - 1, centerY + radiusInt);
        float radiusSqr = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            int row = y * m_MaskWidth;
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                if (dx * dx + dy * dy > radiusSqr)
                    continue;

                m_MaskPixels[row + x].a = 0;
            }
        }
    }

    struct AutoRevealStroke
    {
        public Vector2 TopLeftEnd;
        public Vector2 BottomRightEnd;
        public float PerpSortKey;
    }

    void BuildAutoRevealStrokes()
    {
        m_AutoRevealStrokes.Clear();
        Rect rect = m_RectTransform.rect;
        Vector2 topLeft = new Vector2(rect.xMin, rect.yMax);
        Vector2 bottomRight = new Vector2(rect.xMax, rect.yMin);
        Vector2 diagonal = bottomRight - topLeft;
        float diagonalLength = diagonal.magnitude;
        if (diagonalLength <= 0.001f)
        {
            m_AutoRevealStrokes.Add(new AutoRevealStroke
            {
                TopLeftEnd = topLeft,
                BottomRightEnd = bottomRight,
                PerpSortKey = 0f
            });
            return;
        }

        Vector2 diagonalDir = diagonal / diagonalLength;
        Vector2 perpDir = new Vector2(-diagonalDir.y, diagonalDir.x);
        Vector2 rectCenter = rect.center;

        Vector2[] corners =
        {
            topLeft,
            new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.xMin, rect.yMin),
            bottomRight
        };

        float perpExtent = 0f;
        for (int i = 0; i < corners.Length; i++)
            perpExtent = Mathf.Max(perpExtent, Mathf.Abs(Vector2.Dot(corners[i] - rectCenter, perpDir)));

        perpExtent += m_BrushRadius;
        float strokeSpacing = m_BrushRadius * 0.55f;
        int strokeCount = Mathf.Max(1, Mathf.CeilToInt((perpExtent * 2f) / strokeSpacing));

        for (int i = 0; i < strokeCount; i++)
        {
            float perpOffset = -perpExtent + i * strokeSpacing;
            Vector2 strokeCenter = rectCenter + perpDir * perpOffset;
            if (!TryClipStrokeToRect(rect, strokeCenter, diagonalDir, out Vector2 clipA, out Vector2 clipB))
                continue;

            Vector2 tlEnd = clipA;
            Vector2 brEnd = clipB;
            if (GetDiagonalProgress(tlEnd, topLeft, diagonalDir) > GetDiagonalProgress(brEnd, topLeft, diagonalDir))
            {
                tlEnd = clipB;
                brEnd = clipA;
            }

            m_AutoRevealStrokes.Add(new AutoRevealStroke
            {
                TopLeftEnd = tlEnd,
                BottomRightEnd = brEnd,
                PerpSortKey = Vector2.Dot(tlEnd - topLeft, perpDir)
            });
        }

        if (m_AutoRevealStrokes.Count == 0)
        {
            m_AutoRevealStrokes.Add(new AutoRevealStroke
            {
                TopLeftEnd = topLeft,
                BottomRightEnd = bottomRight,
                PerpSortKey = 0f
            });
            return;
        }

        m_AutoRevealStrokes.Sort((a, b) => a.PerpSortKey.CompareTo(b.PerpSortKey));
    }

    void ScratchAutoRevealProgress(float progress)
    {
        if (m_AutoRevealStrokes.Count == 0)
            return;

        progress = Mathf.Clamp01(progress);
        float scaled = progress * m_AutoRevealStrokes.Count;
        int strokeIndex = Mathf.Min(Mathf.FloorToInt(scaled), m_AutoRevealStrokes.Count - 1);
        float strokeT = scaled - strokeIndex;

        AutoRevealStroke stroke = m_AutoRevealStrokes[strokeIndex];
        Vector2 targetPoint = Vector2.Lerp(stroke.TopLeftEnd, stroke.BottomRightEnd, strokeT);

        if (m_AutoRevealLastStrokeIndex != strokeIndex)
        {
            m_AutoRevealLastStrokeIndex = strokeIndex;
            m_AutoRevealLastPoint = stroke.TopLeftEnd;
            ScratchAt(stroke.TopLeftEnd, false);
        }

        if (m_AutoRevealLastPoint.HasValue &&
            (targetPoint - m_AutoRevealLastPoint.Value).sqrMagnitude > 0.0001f)
        {
            ScratchLine(m_AutoRevealLastPoint.Value, targetPoint, false);
            m_AutoRevealLastPoint = targetPoint;
        }
    }

    void ResetAutoRevealScratchState()
    {
        m_AutoRevealLastPoint = null;
        m_AutoRevealLastStrokeIndex = -1;
    }

    static float GetDiagonalProgress(Vector2 point, Vector2 topLeft, Vector2 diagonalDir)
    {
        return Vector2.Dot(point - topLeft, diagonalDir);
    }

    static bool TryClipStrokeToRect(Rect rect, Vector2 center, Vector2 dir, out Vector2 start, out Vector2 end)
    {
        start = center;
        end = center;
        if (dir.sqrMagnitude <= 0.0001f)
            return false;

        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;

        if (!ClipAxis(center.x, dir.x, rect.xMin, rect.xMax, ref tMin, ref tMax))
            return false;
        if (!ClipAxis(center.y, dir.y, rect.yMin, rect.yMax, ref tMin, ref tMax))
            return false;
        if (tMin > tMax)
            return false;

        start = center + dir * tMin;
        end = center + dir * tMax;
        return true;
    }

    static bool ClipAxis(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
    {
        if (Mathf.Abs(direction) <= 0.0001f)
            return origin >= min && origin <= max;

        float t1 = (min - origin) / direction;
        float t2 = (max - origin) / direction;
        if (t1 > t2)
        {
            float temp = t1;
            t1 = t2;
            t2 = temp;
        }

        tMin = Mathf.Max(tMin, t1);
        tMax = Mathf.Min(tMax, t2);
        return tMin <= tMax;
    }

    void FillRemainingMaskWithBrush()
    {
        for (int i = 0; i < m_AutoRevealStrokes.Count; i++)
        {
            AutoRevealStroke stroke = m_AutoRevealStrokes[i];
            ScratchLine(stroke.TopLeftEnd, stroke.BottomRightEnd, false);
        }

        FlushScratchChanges();
    }

    IEnumerator AutoRevealDiagonalCoroutine(Action onComplete)
    {
        BuildAutoRevealStrokes();
        ResetAutoRevealScratchState();
        float elapsed = 0f;

        while (elapsed < m_AutoRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ScratchAutoRevealProgress(Mathf.Clamp01(elapsed / m_AutoRevealDuration));
            FlushScratchChanges();
            yield return null;
        }

        ScratchAutoRevealProgress(1f);
        FlushScratchChanges();

        if (CalculateRevealProgress() < m_AutoRevealCompleteThreshold)
            FillRemainingMaskWithBrush();

        MarkFullyRevealed();
        m_AutoRevealCoroutine = null;
        onComplete?.Invoke();
    }

    void FillMaskOpaque()
    {
        Color32 opaque = new Color32(255, 255, 255, 255);
        for (int i = 0; i < m_MaskPixels.Length; i++)
            m_MaskPixels[i] = opaque;

        ApplyMaskChanges();
    }

    void ApplyMaskChanges()
    {
        m_MaskTexture.SetPixels32(m_MaskPixels);
        m_MaskTexture.Apply(false, false);
    }

    void UpdateRevealProgress()
    {
        RevealProgress = CalculateRevealProgress();
        if (m_IsRevealing || m_IsFingerScratching)
            return;

        if (RevealProgress >= m_AutoRevealCompleteThreshold)
            MarkFullyRevealed();
    }

    float CalculateRevealProgress()
    {
        if (m_MaskPixels == null || m_MaskPixels.Length == 0)
            return 0f;

        int erasedCount = 0;
        for (int i = 0; i < m_MaskPixels.Length; i++)
        {
            if (m_MaskPixels[i].a == 0)
                erasedCount++;
        }

        return erasedCount / (float)m_MaskPixels.Length;
    }

    void MarkFullyRevealed()
    {
        if (IsFullyRevealed)
            return;

        IsFullyRevealed = true;
        RevealProgress = 1f;
        m_InputEnabled = false;
        m_IsRevealing = false;
        RevealCompleted?.Invoke();
    }

    void OnDestroy()
    {
        if (m_MaskTexture != null)
        {
            Destroy(m_MaskTexture);
            m_MaskTexture = null;
        }

        if (m_Material != null)
        {
            Destroy(m_Material);
            m_Material = null;
        }
    }
}
