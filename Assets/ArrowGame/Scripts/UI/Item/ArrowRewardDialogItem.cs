using System;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
[AddComponentMenu("UI/Item/RewardDialogItem")]

public partial class ArrowRewardDialogItem : UIItemBase
{
    private const float SHOW_START_SCALE = 0.6f;
    private const float SHOW_DURATION = 0.35f;

    protected override void OnInit()
    {
        base.OnInit();
        ResetState();
    }

    private void OnDestroy()
    {
        // 清理动画
        DOTween.Kill(transform);
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    private void ResetState()
    {
        DOTween.Kill(transform);


    }

    /// <summary>
    /// 初始化奖励道具
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="sprite">图标</param>
    public void InitReward(float count, string sprite = null, bool isMoney = false)
    {
        ResetState();
        // 设置数量
        varCount.text = isMoney ? CommonHelper.GetDollarString(count, count >= 0.01f) : count.ToString();
        // 设置图标
        if (sprite != null)
        {
            varRewardItem.SetSprite(sprite);
        }
        // 播放动画
        // PlayRewardAnimation();
        EnableRewardGlow();
    }

    public void PrepareHidden()
    {
        ResetState();
        transform.localScale = Vector3.one * SHOW_START_SCALE;
    }

    public Tween PlayShowAnimation(float duration = SHOW_DURATION)
    {
        return transform.DOScale(Vector3.one, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void EnableRewardGlow(bool isEnable = false)
    {
        varGlowSpine.gameObject.SetActive(isEnable);
    }
    /// <summary>
    /// 播放奖励动画
    /// </summary>
    private void PlayRewardAnimation()
    {
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();

        // 1. 旋转并放大
        // sequence.Append(transform.DOScale(Vector3.one * END_SCALE, ANIMATION_DURATION * 0.8f).SetEase(Ease.OutQuad));
        // sequence.Join(transform.DORotate(new Vector3(0, 360 * ROTATION_CYCLES, 0), ANIMATION_DURATION * 0.8f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

        // 2. 显示文本和图标
        sequence.AppendCallback(() =>
        {
            varCount.gameObject.SetActive(true);
            varRewardItem.gameObject.SetActive(true);
        });

        // 设置动画忽略时间缩放
        sequence.SetUpdate(true);
    }
}
