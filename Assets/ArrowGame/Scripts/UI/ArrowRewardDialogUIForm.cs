using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework;
using GameFramework.Resource;
using System;
using DG.Tweening;
using Spine;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/RewardDialogUIForm")]
public partial class ArrowRewardDialogUIForm : UIFormBase
{
    private List<ArrowRewardDialogItem> m_RewardItems = new List<ArrowRewardDialogItem>();
    private RewardDialogParams m_RewardParams;
    private PlayerDataModel m_PlayerDataModel;
    private string m_TimerId = "RewardDialogAutoCollect";
    bool isCanClick = true;
    int levelUp;
    float rewardRate = 1;
    private float baseVaule = 0f;
    private float m_TargetRewardValue;
    private PlayerDataType m_RewardType;
    private Coroutine m_PresentationCoroutine;
    private Sequence m_PresentationSequence;
    private Tween m_AdClaimBreathTween;

    private const float ELEMENT_SHOW_DURATION = 0.3f;
    private const float ELEMENT_SHOW_START_SCALE = 0.3f;
    private const float REWARD_COUNT_ROLL_DURATION = 0.8f;
    private const float BUTTON_NEXT_SHOW_DELAY = 2f;
    private const float AD_CLAIM_BREATH_SCALE = 1.08f;
    private const float AD_CLAIM_BREATH_DURATION = 0.8f;
    private const float TITLE_TXT_START_SCALE = 0.6f;
    private const int DefaultRewardDialogFreeDoubleTimes = 3;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // m_TaskDesc = GF.Localization.GetString("RewardDialogUIForm.taskDesc");
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        isCanClick = false;
        m_PlayerDataModel = GF.DataModel.GetDataModel<PlayerDataModel>();

        if (Params?.RewardParams != null)
        {
            m_RewardParams = Params.RewardParams;
            // CommonHelper.LogEvent(AdjustEventCodeEvent.reward_enter);
            CommonHelper.LogEvent(AdjustEventCodeEvent.popu_enter, new Dictionary<string, string>() {{"popu", "normalRewardUI"}});

            // FirebaseMseeageManage.Instance.LogEvent(m_RewardParams.rewardSource);

            ShowRewards();

            if (m_RewardParams.isADDouble && !IsFreeDoubleRewardAvailable())
            {
                CommonHelper.LogAdButtonShowEvent(m_RewardParams.rewardSource);
            }
            GF.Sound.PlayEffect("arrowPlay/reward.mp3");
        }
    }

    private void AutoCollectCoroutine()
    {
        CommonHelper.LogEvent(AdjustEventCodeEvent.reward_cancel, new Dictionary<string, string>() {{"placement", "normal"}});
        CollectAllRewards();
        TimerManager.Instance.RemoveTimer(m_TimerId);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {

        StopPresentationAnimation();
        ResetTitleAnimation();
        m_RewardItems.Clear();
        m_RewardParams = null;
        levelUp = 0;
        rewardRate = 1;
        isCanClick = true;
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        if (!isCanClick) return;
        switch (btId)
        {
            case "Button_Claim":
                isCanClick = false;
                if (!m_RewardParams.isADDouble)
                {
                    ClaimReward();
                }
                else
                {
                    m_RewardParams.OnDialogClosed?.Invoke();
                }
                GF.UI.Close(this.UIForm);
                isCanClick = true;

                break;
            case "Button_AdClaim":
                isCanClick = false;
                if (m_RewardParams.isADDouble)
                {
                    if (IsFreeDoubleRewardAvailable())
                    {
                        CommonHelper.LogEvent(AdjustEventCodeEvent.reward_collect, new Dictionary<string, string>() {{"placement", "normal"}});
                        OnDoubleRewardClaimed();
                        CollectAllRewards(rewardRate);
                        GF.UI.Close(this.UIForm);
                        isCanClick = true;
                    }
                    else
                    {
                        CommonHelper.ShowVideoAd(m_RewardParams.rewardSource, isOK =>
                        {
                            if (isOK)
                            {
                                CommonHelper.LogEvent(AdjustEventCodeEvent.reward_collect, new Dictionary<string, string>() {{"placement", "normal"}});
                                OnDoubleRewardClaimed();
                                CollectAllRewards(rewardRate);
                                GF.UI.Close(this.UIForm);
                            }
                            isCanClick = true;
                        });
                    }
                }
                break;
            case "Button_Next":

                isCanClick = false;
                if (m_RewardParams.isADDouble)
                {
                    ClaimReward();
                }
                else
                {
                    m_RewardParams.OnDialogClosed?.Invoke();
                }
                GF.UI.Close(this.UIForm);
                isCanClick = true;
                break;
        }
    }

    /// <summary>
    /// 从 Config获取倍数（看广告翻倍时使用）
    /// </summary>
    private float GetAdMultiplier()
    {
        int value = GF.Config.GetInt("ComboRewardRate", 8);
        return value > 0 ? value : 8;
    }

    /// <summary>
    /// 从 Config获取倍数（胜利时使用）
    /// </summary>
    private float GetWinRewardMultiplier()
    {
        int value = GF.Config.GetInt("WinRewardRate", 10);
        return value > 0 ? value : 10;
    }

    private void ClaimReward(float Rate = 1f)
    {
        CommonHelper.LogEvent(AdjustEventCodeEvent.reward_cancel, new Dictionary<string, string>() {{"placement", "normal"}});
        CommonHelper.ShowInterstitial(RewardSourceConst.RewardInterstitial);
        CollectAllRewards(Rate);
    }

    private void CollectAllRewards(float Rate = 1f)
    {
        if (m_RewardParams == null) return;


        foreach (var reward in m_RewardParams.Rewards)
        {
            float value = baseVaule;
            if (reward.Type == PlayerDataType.Coins)
            {
                value = Mathf.Ceil(value * Rate);
            }
            else if (reward.Type == PlayerDataType.Diamond)
            {
                value = (float)Math.Round(value * Rate, 3);
            }

            // 触发奖励收集事件
            GF.Event.Fire(this, RewardCollectedEventArgs.Create(
            value,
            reward.WorldPosition ?? Vector3.zero,
            reward.Type));
            // 调用奖励收集回调
            reward.OnRewardCollected?.Invoke();
            // 调用弹窗关闭回调
            m_RewardParams.OnDialogClosed?.Invoke();
        }
    }

    private TrackEntry m_TitleTrackEntry;

    private void PlayTitleOpenAnimation()
    {
        if (varTitle == null) return;

        var animData = varTitle.Skeleton?.Data?.FindAnimation("animation");
        float animDuration = animData != null ? animData.Duration : ELEMENT_SHOW_DURATION;

        m_TitleTrackEntry = varTitle.AnimationState.SetAnimation(0, "animation", false);
        m_TitleTrackEntry.Complete += OnTitleAnimationComplete;
    }

    private void OnTitleAnimationComplete(TrackEntry trackEntry)
    {
        if (trackEntry != m_TitleTrackEntry) return;

        trackEntry.Complete -= OnTitleAnimationComplete;
        m_TitleTrackEntry = null;

        if (varTitle != null)
        {
            varTitle.AnimationState.SetAnimation(0, "01", true);
        }
    }

    private void ResetTitleAnimation()
    {
        if (m_TitleTrackEntry != null)
        {
            m_TitleTrackEntry.Complete -= OnTitleAnimationComplete;
            m_TitleTrackEntry = null;
        }
    }

    private void StopPresentationAnimation()
    {
        if (m_PresentationCoroutine != null)
        {
            StopCoroutine(m_PresentationCoroutine);
            m_PresentationCoroutine = null;
        }

        if (m_PresentationSequence != null)
        {
            m_PresentationSequence.Kill();
            m_PresentationSequence = null;
        }

        KillTransformTween(varArray != null ? varArray.transform : null);
        KillTransformTween(varRewardValue != null ? varRewardValue.transform : null);
        KillTransformTween(varButton_AdClaim != null ? varButton_AdClaim.transform : null);
        KillTransformTween(varButton_Next != null ? varButton_Next.transform : null);
        // KillTransformTween(varBalanceBg != null ? varBalanceBg.transform : null);
        StopAdClaimBreathEffect();
        varRewardDialogItem_1?.PrepareHidden();
        varRewardDialogItem_2?.PrepareHidden();
    }

    private static void KillTransformTween(Transform target)
    {
        if (target != null)
        {
            DOTween.Kill(target);
        }
    }

    /// <summary>
    /// 显示奖励列表
    /// </summary>
    private void ShowRewards()
    {
        if (m_RewardParams == null) return;

        m_RewardItems.Clear();
        PrepareRewardVisuals();
        StopPresentationAnimation();
        m_PresentationCoroutine = StartCoroutine(PlayPresentationSequence());
    }

    private void PrepareRewardVisuals()
    {
        SetElementActive(varRewardDialogItem_1, false);
        SetElementActive(varRewardDialogItem_2, false);
        SetElementActive(varDoubleObj, false);
        SetElementActive(varArray, false);
        SetElementActive(varRewardValue, false);
        SetElementActive(varRate, false);
        SetElementActive(varButton_AdClaim, false);
        SetElementActive(varButton_Next, false);
        // SetElementActive(varBalanceBg, false);
        UpdateAdClaimButtonVisual(true);

        var reward = m_RewardParams.Rewards[0];
        m_RewardType = reward.Type;
        bool isWinreward = m_RewardParams.rewardSource == RewardSourceConst.GamewinReward;
        rewardRate = isWinreward ? GetWinRewardMultiplier() : GetAdMultiplier();
        m_TargetRewardValue = reward.Value*rewardRate;

        varTitletxt.SetActive(!isWinreward);
        varWinTitle.SetActive(isWinreward);

        if (m_RewardParams.isADDouble)
        {
            baseVaule = reward.Value;

            varRewardDialogItem_2.InitReward(baseVaule, null, reward.Type == PlayerDataType.Diamond);
            varRewardDialogItem_2.EnableRewardGlow(true);
            varRewardDialogItem_2.PrepareHidden();

            varRateTxt.text = $"x{rewardRate}";
            varRewardCount.text = FormatRewardValueText(baseVaule*rewardRate, reward.Type);

            varNext.gameObject.SetActive(isWinreward);
            varContinue.gameObject.SetActive(!isWinreward);
            UpdateAdClaimButtonVisual();
        }
        else
        {
            varRewardDialogItem_1.InitReward(
                m_TargetRewardValue,
                GetRewardIconPath(reward.Type),
                reward.Type == PlayerDataType.Diamond);
            varRewardDialogItem_1.PrepareHidden();
        }
    }

    private IEnumerator PlayPresentationSequence()
    {
        PlayTitleOpenAnimation();

        float titleDuration = ELEMENT_SHOW_DURATION;
        // if (varTitle != null)
        // {
        //     var animData = varTitle.Skeleton?.Data?.FindAnimation("animation");
        //     if (animData != null)
        //     {
        //         titleDuration = animData.Duration;
        //     }
        // }
        yield return new WaitForSecondsRealtime(titleDuration);

        if (m_RewardParams.isADDouble)
        {
            yield return PlayDoubleRewardPresentation();
            yield return ShowActionButtons();
        }
        else
        {
            yield return PlayNormalRewardPresentation();
        }

        StartAutoCollectIfNeeded();
        isCanClick = true;
        m_PresentationCoroutine = null;
    }

    private IEnumerator PlayDoubleRewardPresentation()
    {
        SetElementActive(varDoubleObj, true);

        SetElementActive(varRewardDialogItem_2, true);
        yield return varRewardDialogItem_2.PlayShowAnimation(ELEMENT_SHOW_DURATION).WaitForCompletion();

        yield return ShowScaledElement(varArray != null ? varArray.transform : null, ELEMENT_SHOW_DURATION);

        SetElementActive(varRate, true);
        yield return ShowScaledElement(varRewardValue != null ? varRewardValue.transform : null, ELEMENT_SHOW_DURATION);

        yield return RollRewardCount(baseVaule, m_TargetRewardValue, REWARD_COUNT_ROLL_DURATION);
    }

    private IEnumerator PlayNormalRewardPresentation()
    {
        SetElementActive(varRewardDialogItem_1, true);
        yield return varRewardDialogItem_1.PlayShowAnimation(ELEMENT_SHOW_DURATION).WaitForCompletion();
    }

    private IEnumerator ShowScaledElement(Transform target, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        target.gameObject.SetActive(true);
        target.localScale = Vector3.one * ELEMENT_SHOW_START_SCALE;
        yield return target.DOScale(Vector3.one, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .WaitForCompletion();
    }

    private IEnumerator RollRewardCount(float fromValue, float toValue, float duration)
    {
        if (varRewardCount == null)
        {
            yield break;
        }

        GF.Sound.PlayEffect("arrowPlay/digitalScroll.mp3");
        varRewardCount.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float currentValue = Mathf.Lerp(fromValue, toValue, progress);
            varRewardCount.text = FormatRewardValueText(currentValue, m_RewardType);
            yield return null;
        }

        varRewardCount.text = FormatRewardValueText(toValue, m_RewardType);
    }

    private IEnumerator ShowActionButtons()
    {
        if (!m_RewardParams.isADDouble)
        {
            yield break;
        }

        UpdateBalanceDisplay();
        // ShowScaledButtons(varBalanceBg);
        ShowScaledButtons(varButton_AdClaim, true);
        isCanClick = true;

        yield return new WaitForSecondsRealtime(BUTTON_NEXT_SHOW_DELAY);
        ShowScaledButtons(varButton_Next);
    }

    private void StopAdClaimBreathEffect()
    {
        if (m_AdClaimBreathTween != null)
        {
            m_AdClaimBreathTween.Kill();
            m_AdClaimBreathTween = null;
        }
    }

    private void StartAdClaimBreathEffect(Transform target)
    {
        StopAdClaimBreathEffect();
        if (target == null)
        {
            return;
        }

        target.localScale = Vector3.one;
        m_AdClaimBreathTween = target
            .DOScale(Vector3.one * AD_CLAIM_BREATH_SCALE, AD_CLAIM_BREATH_DURATION)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void ShowScaledButtons(GameObject buttonRoot, bool playBreath = false)
    {
        if (buttonRoot == null)
        {
            return;
        }

        buttonRoot.SetActive(true);
        buttonRoot.transform.localScale = Vector3.one * ELEMENT_SHOW_START_SCALE;
        Tween showTween = buttonRoot.transform
            .DOScale(Vector3.one, ELEMENT_SHOW_DURATION)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        if (playBreath)
        {
            showTween.OnComplete(() => StartAdClaimBreathEffect(buttonRoot.transform));
        }
    }

    private void StartAutoCollectIfNeeded()
    {
        if (m_RewardParams == null || !m_RewardParams.AutoCollect)
        {
            return;
        }

        TimerManager.Instance.AddTimer(m_TimerId, m_RewardParams.AutoCollectDelay, AutoCollectCoroutine);
    }

    private static void SetElementActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private static void SetElementActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    // private float FormatRewardValue(float value, PlayerDataType type)
    // {
    //     if (type == PlayerDataType.Coins)
    //     {
    //         return Mathf.Ceil(value);
    //     }
    //
    //     if (type == PlayerDataType.Diamond)
    //     {
    //         return (float)Math.Round(value, 3);
    //     }
    //
    //     if (type == PlayerDataType.Gems)
    //     {
    //         return Mathf.Ceil(value);
    //     }
    //
    //     return value;
    // }

    private string FormatRewardValueText(float value, PlayerDataType type, bool needSymbol = true)
    {
        float formattedValue = value;
        if (type == PlayerDataType.Diamond)
        {
            return CommonHelper.GetDollarString(formattedValue, value >= 0.01f, needSymbol);
        }

        return formattedValue.ToString();
    }

    private void UpdateBalanceDisplay()
    {
        if (varBalance == null)
            return;

        // float balanceValue = m_TargetRewardValue * CommonHelper.GetMakeupRatio();
        varBalance.text = $"≈{FormatRewardValueText(m_TargetRewardValue, m_RewardType, true)}";
    }

    private void UpdateAdClaimButtonVisual(bool hideAll = false)
    {
        if (hideAll)
        {
            SetElementActive(varAd, false);
            SetElementActive(varNoAd, false);
            return;
        }

        bool isFree = IsFreeDoubleRewardAvailable();
        SetElementActive(varNoAd, isFree);
        SetElementActive(varAd, !isFree);
    }

    private bool IsFreeDoubleRewardAvailable()
    {
        if (m_PlayerDataModel == null)
            return false;

        int freeTimes = GF.Config.HasConfig("RewardDialogFreeDoubleTimes")
            ? GF.Config.GetInt("RewardDialogFreeDoubleTimes")
            : DefaultRewardDialogFreeDoubleTimes;
        return m_PlayerDataModel.RewardDialogDoubleTimes < freeTimes;
    }

    private void OnDoubleRewardClaimed()
    {
        if (m_PlayerDataModel == null)
            return;

        m_PlayerDataModel.RewardDialogDoubleTimes++;
        m_PlayerDataModel.Save();
    }

    private string GetRewardIconPath(PlayerDataType type)
    {
        switch (type)
        {
            case PlayerDataType.Diamond:
                return "Common/money_Big.png";
            default:
                return "Common/coin_Big.png";
        }
    }

}
