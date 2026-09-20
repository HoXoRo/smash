using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityGameFramework.Runtime;
using System;
using System.Collections.Generic;

public partial class ArrowBubbleRewardItem : UIItemBase, IPointerClickHandler
{
    const float DiagonalSpawnPadding = 20f;
    const float DiagonalDirectionRandomAngle = 8f;
    const float MinUpwardDirectionY = 0.35f;
    const float InitialUpwardDirectionY = 0.75f;

    enum UpwardDiagonalPhase
    {
        Bouncing,
        Exiting
    }

    [Header("动画参数")]
    private float moveSpeed = 400f;
    private float boundaryOffset = 90f;

    private Vector2 parentBounds;
    private bool isMoving;
    private Action<ArrowBubbleRewardItem, bool> onBubbleClicked;
    private Action<ArrowBubbleRewardItem> onBubbleDisappear;
    public bool m_IsAD;

    private Tween m_MoveTween;
    private bool m_UseUpwardDiagonalMove;
    private UpwardDiagonalPhase m_UpwardDiagonalPhase;
    private Rect m_MoveBounds;
    private Vector2 m_Direction;
    private RectTransform m_RectTransform;

    /// <summary> 斜向移出屏幕后触发被动冷却。 </summary>
    public bool ApplyPassiveCooldownOnDisappear { get; private set; }

    public void StartHorizontalMove(float leftX, float rightX, float centerY, float amplitudeY, float cycleDuration)
    {
        StopMove();
        if (leftX >= rightX || cycleDuration <= 0f)
        {
            Log.Error("[气泡奖励] 水平运动参数无效");
            return;
        }

        isMoving = true;
        const int pathPoints = 24;
        var path = new List<Vector3>(pathPoints + 1);
        for (int i = 0; i <= pathPoints; i++)
        {
            float t = (float)i / pathPoints;
            float x = Mathf.Lerp(leftX, rightX, t);
            float y = centerY + amplitudeY * Mathf.Sin(t * Mathf.PI * 2f);
            path.Add(new Vector3(x, y, 0f));
        }

        transform.localPosition = path[0];
        m_MoveTween = transform.DOLocalPath(path.ToArray(), cycleDuration, PathType.Linear, PathMode.Sidescroller2D, 10, null)
            .SetEase(Ease.Linear)
            .SetLoops(-1);
    }

    /// <summary>
    /// 从左下/右下斜向上移动；左右墙斜角反弹，上下墙沿原轨迹斜向移出。
    /// </summary>
    public void StartUpwardDiagonalMove(Rect boundsLocal, bool spawnBottomLeft, float speed)
    {
        StopMove();
        m_RectTransform = transform as RectTransform;
        if (m_RectTransform == null)
        {
            Log.Error("[气泡奖励] RectTransform 为空，无法启动斜向上移动");
            return;
        }

        m_MoveBounds = boundsLocal;
        moveSpeed = speed;
        m_UpwardDiagonalPhase = UpwardDiagonalPhase.Bouncing;
        m_UseUpwardDiagonalMove = true;
        ApplyPassiveCooldownOnDisappear = false;

        Vector2 halfSize = GetHalfSize();
        float padX = halfSize.x + DiagonalSpawnPadding;
        float padY = halfSize.y + DiagonalSpawnPadding;
        Vector2 startPos = spawnBottomLeft
            ? new Vector2(boundsLocal.xMin + padX, boundsLocal.yMin + padY)
            : new Vector2(boundsLocal.xMax - padX, boundsLocal.yMin + padY);

        Vector2 baseDirection = spawnBottomLeft
            ? new Vector2(1f, InitialUpwardDirectionY)
            : new Vector2(-1f, InitialUpwardDirectionY);
        float randomAngle = UnityEngine.Random.Range(-DiagonalDirectionRandomAngle, DiagonalDirectionRandomAngle) * Mathf.Deg2Rad;
        m_Direction = EnsureUpwardDirection(Rotate(baseDirection, randomAngle).normalized);

        m_RectTransform.localPosition = new Vector3(startPos.x, startPos.y, 0f);
        isMoving = true;
    }

    public void StopMove()
    {
        if (m_MoveTween != null)
        {
            m_MoveTween.Kill();
            m_MoveTween = null;
        }

        m_UseUpwardDiagonalMove = false;
        isMoving = false;
    }

    protected override void OnInit()
    {
        base.OnInit();
        InitializeBubble();
    }

    void Update()
    {
        if (!m_UseUpwardDiagonalMove || !isMoving || m_RectTransform == null)
            return;

        float delta = Time.unscaledDeltaTime;
        Vector2 pos = m_RectTransform.localPosition;
        Vector2 halfSize = GetHalfSize();
        float minX = m_MoveBounds.xMin + halfSize.x;
        float maxX = m_MoveBounds.xMax - halfSize.x;
        float minY = m_MoveBounds.yMin + halfSize.y;
        float maxY = m_MoveBounds.yMax - halfSize.y;

        if (m_UpwardDiagonalPhase == UpwardDiagonalPhase.Exiting)
        {
            Vector2 exitNext = pos + m_Direction * moveSpeed * delta;
            m_RectTransform.localPosition = exitNext;
            if (IsFullyOutsideBounds(exitNext, halfSize))
                FinishExit();
            return;
        }

        Vector2 next = pos + m_Direction * moveSpeed * delta;

        if (next.y >= maxY || next.y <= minY)
        {
            m_RectTransform.localPosition = new Vector2(Mathf.Clamp(next.x, minX, maxX), Mathf.Clamp(next.y, minY, maxY));
            BeginExit();
            return;
        }

        if (next.x <= minX)
        {
            next.x = minX;
            m_Direction.x = Mathf.Abs(m_Direction.x);
        }
        else if (next.x >= maxX)
        {
            next.x = maxX;
            m_Direction.x = -Mathf.Abs(m_Direction.x);
        }

        m_Direction = EnsureUpwardDirection(m_Direction.normalized);
        m_RectTransform.localPosition = next;
    }

    void BeginExit()
    {
        m_UpwardDiagonalPhase = UpwardDiagonalPhase.Exiting;
        ApplyPassiveCooldownOnDisappear = true;
    }

    void FinishExit()
    {
        ApplyPassiveCooldownOnDisappear = true;
        HideBubble();
    }

    bool IsFullyOutsideBounds(Vector2 pos, Vector2 halfSize)
    {
        return pos.x + halfSize.x < m_MoveBounds.xMin
            || pos.x - halfSize.x > m_MoveBounds.xMax
            || pos.y + halfSize.y < m_MoveBounds.yMin
            || pos.y - halfSize.y > m_MoveBounds.yMax;
    }

    private void InitializeBubble()
    {
        if (varBgButton != null)
        {
            varBgButton.onClick.RemoveAllListeners();
            varBgButton.onClick.AddListener(OnBubbleClick);
        }

        moveSpeed = GF.Config.GetFloat("BubbleMoveSpeed");

        var parentRect = transform.parent?.GetComponent<RectTransform>();
        if (parentRect != null)
            parentBounds = new Vector2(parentRect.rect.width * 0.5f, parentRect.rect.height * 0.5f);
        else
            parentBounds = new Vector2(500f, 300f);

        ResetBubble();
    }

    private void ResetBubble()
    {
        DOTween.Kill(transform);
        StopMove();
        ApplyPassiveCooldownOnDisappear = false;
    }

    private void ShowBubble()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void HideBubble()
    {
        isMoving = false;
        StopMove();

        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            onBubbleDisappear?.Invoke(this);
        });
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnBubbleClick();
    }

    private void OnBubbleClick()
    {
        if (!isMoving) return;

        Log.Info("[气泡奖励] 气泡被点击");
        onBubbleClicked?.Invoke(this, m_IsAD);
    }

    public PlayerDataType m_rewardType;
    public float m_rewardValue;

    public void SetCallbacks(Action<ArrowBubbleRewardItem, bool> onClick, Action<ArrowBubbleRewardItem> onDisappear)
    {
        onBubbleClicked = onClick;
        onBubbleDisappear = onDisappear;
    }

    public void SetBubbleReward(PlayerDataType rewardType, float rewardValue, bool isAd = false)
    {
        ResetBubble();
        m_rewardType = rewardType;
        m_rewardValue = rewardValue;
        m_IsAD = isAd;
        varTxtValue.text = CommonHelper.GetDollarString(rewardValue, rewardValue>0.01f);
        ShowBubble();
    }

    public bool IsMoving => isMoving;

    Vector2 GetHalfSize()
    {
        if (m_RectTransform == null)
            m_RectTransform = transform as RectTransform;

        if (m_RectTransform != null)
            return m_RectTransform.rect.size * 0.5f;

        return new Vector2(boundaryOffset, boundaryOffset);
    }

    static Vector2 EnsureUpwardDirection(Vector2 direction)
    {
        if (direction.y < MinUpwardDirectionY)
            direction.y = MinUpwardDirectionY;

        return direction.normalized;
    }

    static Vector2 Rotate(Vector2 vector, float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos);
    }
}
