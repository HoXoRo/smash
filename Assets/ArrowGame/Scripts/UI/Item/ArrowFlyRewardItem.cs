using System;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/FlyRewardItem")]
public partial class ArrowFlyRewardItem : UIItemBase
{
    //道具图标
    public Image rewardIcon;
    public RectTransform rectTransform;  // 改为public以便外部访问
    
    // 动画参数
    private const float START_SCALE = 0.3f;  // 起始缩放
    private const float MAX_SCALE = 1.3f;    // 最大缩放
    private const float MIN_SCALE = 1.2f;    // 最小缩放
    private const float DISPERSE_RADIUS = 80f;  // 离散半径
    private const float APPEAR_DURATION = 0.2f;  // 出现动画时长
    private const float GATHER_DURATION = 0.8f;    // 聚集动画时长
    private const float FADE_DURATION = 0.2f;      // 淡出动画时长
    private const float TOTAL_DURATION = 1.3f;     // 总动画时长

    private Vector3 targetPosition;  // 目标位置
    private Action onComplete;       // 完成回调
    private float appearDelay;       // 出现延迟
    private int index;              // 货币索引

    protected override void OnInit()
    {
        base.OnInit();
        ResetState();
    }

    private void OnDestroy()
    {
        // 清理动画（添加空检查）
        if (rectTransform != null) DOTween.Kill(rectTransform);
        if (rewardIcon != null) DOTween.Kill(rewardIcon);
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    private void ResetState()
    {
        if (rectTransform != null) DOTween.Kill(rectTransform);
        if (rewardIcon != null) DOTween.Kill(rewardIcon);
        if (rectTransform != null) rectTransform.localScale = Vector3.zero;
        if (rewardIcon != null)
        {
            rewardIcon.gameObject.SetActive(true);
            rewardIcon.color = new Color(1, 1, 1, 0);
        }
    }

    /// <summary>
    /// 初始化奖励道具
    /// </summary>
    public void InitReward(PlayerDataType type, Vector3 targetPos, Action completeCallback = null, float delay = 0f, int idx = 0)
    {
        if (rectTransform == null || rewardIcon == null) return;
        ResetState();
        targetPosition = targetPos;
        onComplete = completeCallback;
        appearDelay = delay;
        index = idx;
        string iconPath = GetCurrIconPath(type);
        // 设置图标
        if (rewardIcon != null)
            rewardIcon.SetSprite(iconPath);
            
        // 播放动画
        PlayRewardAnimation();
    }
    
    private string GetCurrIconPath(PlayerDataType type)
    {
        switch (type)
        {
            case PlayerDataType.Diamond:
                return "Common/money_com.png";
            default:
                return "Common/coin_com.png";
        }
    }

    /// <summary>
    /// 初始化奖励道具
    /// </summary>
    public void InitReward(string sprite, Vector3 targetPos, Action completeCallback = null)
    {
        ResetState();
        targetPosition = targetPos;
        onComplete = completeCallback;

        // 设置图标
        rewardIcon.SetSprite(sprite);

        // 播放动画
        PlayMoveAnimation();
    }
    private void PlayMoveAnimation()
    {
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();

        // 设置动画忽略时间缩放
        sequence.SetUpdate(true);
        float randomScale = UnityEngine.Random.Range(MIN_SCALE, MAX_SCALE);

        // 出现动画
        sequence.Append(rectTransform.DOScale(Vector3.one * randomScale, APPEAR_DURATION).SetEase(Ease.OutBack));
        sequence.Join(rewardIcon.DOFade(1, APPEAR_DURATION));
        // 3. 飞向目标
        // Log.Info($"飞向目标: {targetPosition}");
        sequence.Append(rectTransform.DOMove(targetPosition, GATHER_DURATION).SetEase(Ease.InQuad));
        sequence.Join(rectTransform.DOScale(Vector3.one * 0.5f, GATHER_DURATION).SetEase(Ease.InQuad));

        // 4. 淡出
        sequence.Append(rewardIcon.DOFade(1, FADE_DURATION));

        // 完成回调
        sequence.OnComplete(() =>
        {
            rewardIcon.gameObject.SetActive(false);
            onComplete?.Invoke();
        });

    }
    /// <summary>
    /// 播放奖励动画
    /// </summary>
    private void PlayRewardAnimation()
    {
        if (rectTransform == null || rewardIcon == null) return;
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();

        // 设置动画忽略时间缩放
        sequence.SetUpdate(true);

        // 1. 出现阶段：从小变大 + 淡入
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * DISPERSE_RADIUS;
        Vector2 appearPosition = rectTransform.anchoredPosition + randomOffset;
        float randomScale = UnityEngine.Random.Range(MIN_SCALE, MAX_SCALE);

        // 设置初始位置（带随机偏移）
        rectTransform.anchoredPosition = appearPosition;

        // 添加出现延迟
        sequence.AppendInterval(appearDelay);

        // 出现动画
        sequence.Append(rectTransform.DOScale(Vector3.one * randomScale, APPEAR_DURATION).SetEase(Ease.OutBack));
        sequence.Join(rewardIcon.DOFade(1, APPEAR_DURATION));

        // 2. 等待其他货币出现
        float waitTime = TOTAL_DURATION - APPEAR_DURATION - GATHER_DURATION - FADE_DURATION;
        sequence.AppendInterval(waitTime);

        // 3. 飞向目标
        // Log.Info($"飞向目标: {targetPosition}");
        sequence.Append(rectTransform.DOAnchorPos(targetPosition, GATHER_DURATION).SetEase(Ease.InQuad));
        // sequence.Join(rectTransform.DOScale(Vector3.one * 0.5f, GATHER_DURATION).SetEase(Ease.InQuad));

        // 4. 淡出
        sequence.Append(rewardIcon.DOFade(1, FADE_DURATION));

        // 完成回调（添加空检查，防止UI关闭后回调执行时崩溃）
        sequence.OnComplete(() => {
            if (rewardIcon != null && rewardIcon.gameObject != null)
            {
                rewardIcon.gameObject.SetActive(false);
            }
            onComplete?.Invoke();
        });

        // 设置序列ID
        sequence.SetId(this);
    }
}
