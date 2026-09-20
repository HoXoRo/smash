using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowScratchCardRewardUIForm")]
public partial class ArrowScratchCardRewardUIForm : UIFormBase
{
    public const string P_ScratchCardRewardValue = "ScratchCardRewardValue";

    const float ElementShowDuration = 0.3f;
    const float ElementShowStartScale = 0.3f;
    const float RewardCountRollDuration = 0.8f;
    const float CancelShowDelay = 2f;
    const float BreathScale = 1.08f;
    const float BreathDuration = 0.8f;

    float m_RewardValue;
    float m_TargetRewardValue;
    int m_RewardRate;
    bool m_IsClaiming;
    bool m_AllowAdClaim;
    bool m_AllowCancel;
    Coroutine m_PresentationCoroutine;
    Tween m_ImgTitleBreathTween;
    Tween m_AdClaimBreathTween;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ResolveUIReferences();
        m_RewardValue = Params.Get<VarSingle>(P_ScratchCardRewardValue)?.Value ?? 0f;
        m_RewardRate = GF.Config.GetInt("ScratchRewardRate", 10);
        m_IsClaiming = false;
        m_AllowAdClaim = false;
        m_AllowCancel = false;
        RefreshRewardDisplay();
        PreparePresentationVisuals();
        StopPresentationAnimation();
        m_PresentationCoroutine = StartCoroutine(PlayPresentationSequence());
        CommonHelper.LogAdButtonShowEvent(RewardSourceConst.scratchCardReward);
        GF.Sound.PlayEffect("arrowPlay/scratchCardReward.mp3");
        // CommonHelper.LogEvent(AdjustEventCodeEvent.scratchCard_reward_enter);
        CommonHelper.LogEvent(AdjustEventCodeEvent.popu_enter, new Dictionary<string, string>() {{"popu", "scratchCard_rewardUI"}});

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopPresentationAnimation();
        m_IsClaiming = false;
        m_AllowAdClaim = false;
        m_AllowCancel = false;
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Button_AdClaim":
                OnAdClaimButtonClick();
                break;
            case "Button_Next":
            case "Button_cancel":
                // CommonHelper.LogEvent(AdjustEventCodeEvent.scratchCard_reward_cancel);
                CommonHelper.LogEvent(AdjustEventCodeEvent.reward_cancel, new Dictionary<string, string>() {{"placement", "scratchCard_reward"}});
                OnCancelButtonClick();
                break;
        }
    }

    void ResolveUIReferences()
    {
        if (varRewardCount == null || varBalance == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (varRewardCount == null && texts[i].name == "RewardCount")
                    varRewardCount = texts[i];
                if (varBalance == null && texts[i].name == "balance")
                    varBalance = texts[i];
            }
        }

        varImgIcon = varImgIcon ?? FindPresentationObject("imgIcon");
        varRewardValue = varRewardValue ?? FindPresentationObject("rewardValue");
        varBalanceBg = varBalanceBg ?? FindPresentationObject("balanceBg");
        varImgTitle = varImgTitle ?? FindPresentationObject("imgTitle");
        varButton_AdClaim = varButton_AdClaim ?? FindPresentationObject("Button_AdClaim");
        varButton_cancel = varButton_cancel ?? FindPresentationObject("Button_cancel");
    }

    GameObject FindPresentationObject(string objectName)
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }

    void PreparePresentationVisuals()
    {
        SetElementActive(varImgIcon, false);
        SetElementActive(varRewardValue, false);
        // SetElementActive(varBalanceBg, false);
        SetElementActive(varImgTitle, false);
        SetElementActive(varButton_AdClaim, false);
        SetElementActive(varButton_cancel, false);

        if (varRewardCount != null)
            varRewardCount.gameObject.SetActive(false);
    }

    IEnumerator PlayPresentationSequence()
    {
        yield return ShowScaledElementsParallel(varImgIcon);

        yield return ShowScaledElementsParallel(varImgTitle, varRewardValue);
        StartBreathEffect(varImgTitle != null ? varImgTitle.transform : null, ref m_ImgTitleBreathTween);

        yield return RollRewardCount(m_RewardValue, m_TargetRewardValue, RewardCountRollDuration);

        yield return ShowScaledElementsParallel(null, varButton_AdClaim);
        StartBreathEffect(varButton_AdClaim != null ? varButton_AdClaim.transform : null, ref m_AdClaimBreathTween);
        m_AllowAdClaim = true;

        yield return new WaitForSecondsRealtime(CancelShowDelay);
        yield return ShowScaledElementsParallel(varButton_cancel);
        m_AllowCancel = true;
        m_PresentationCoroutine = null;
    }

    IEnumerator RollRewardCount(float fromValue, float toValue, float duration)
    {
        if (varRewardCount == null)
            yield break;

        GF.Sound.PlayEffect("arrowPlay/digitalScroll.mp3");
        varRewardCount.gameObject.SetActive(true);
        varRewardCount.text = FormatRewardValue(fromValue);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float currentValue = Mathf.Lerp(fromValue, toValue, progress);
            varRewardCount.text = FormatRewardValue(currentValue);
            yield return null;
        }

        varRewardCount.text = FormatRewardValue(toValue);
    }

    static IEnumerator ShowScaledElementsParallel(params GameObject[] elements)
    {
        int tweenCount = 0;
        for (int i = 0; i < elements.Length; i++)
        {
            GameObject element = elements[i];
            if (element == null)
                continue;

            element.SetActive(true);
            Transform target = element.transform;
            target.localScale = Vector3.one * ElementShowStartScale;
            target.DOScale(Vector3.one, ElementShowDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            tweenCount++;
        }

        if (tweenCount > 0)
            yield return new WaitForSecondsRealtime(ElementShowDuration);
    }

    void StartBreathEffect(Transform target, ref Tween breathTween)
    {
        StopBreathEffect(ref breathTween);
        if (target == null)
            return;

        target.localScale = Vector3.one;
        breathTween = target
            .DOScale(Vector3.one * BreathScale, BreathDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    static void StopBreathEffect(ref Tween breathTween)
    {
        if (breathTween != null && breathTween.IsActive())
            breathTween.Kill();

        breathTween = null;
    }

    void StopPresentationAnimation()
    {
        if (m_PresentationCoroutine != null)
        {
            StopCoroutine(m_PresentationCoroutine);
            m_PresentationCoroutine = null;
        }

        StopBreathEffect(ref m_ImgTitleBreathTween);
        StopBreathEffect(ref m_AdClaimBreathTween);
        KillTransformTween(varImgIcon != null ? varImgIcon.transform : null);
        KillTransformTween(varRewardValue != null ? varRewardValue.transform : null);
        // KillTransformTween(varBalanceBg != null ? varBalanceBg.transform : null);
        KillTransformTween(varImgTitle != null ? varImgTitle.transform : null);
        KillTransformTween(varButton_AdClaim != null ? varButton_AdClaim.transform : null);
        KillTransformTween(varButton_cancel != null ? varButton_cancel.transform : null);
    }

    static void KillTransformTween(Transform target)
    {
        if (target != null)
            DOTween.Kill(target);
    }

    static void SetElementActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    void RefreshRewardDisplay()
    {
        m_TargetRewardValue = m_RewardValue * m_RewardRate;

        if (varCount != null)
            varCount.text = FormatRewardValue(m_RewardValue);

        if (varRateTxt != null)
            varRateTxt.text = $"x{m_RewardRate}";

        if (varRewardCount != null)
            varRewardCount.text = FormatRewardValue(m_RewardValue);

        /*if (varBalance != null)
            varBalance.text = $"≈{FormatRewardValue(m_RewardValue * m_RewardRate * CommonHelper.GetMakeupRatio(), true)}";*/
    }

    static string FormatRewardValue(float value, bool needSymbol = true)
    {
        return CommonHelper.GetDollarString(value, value >= 0.01f, needSymbol);
    }

    void OnAdClaimButtonClick()
    {
        if (!m_AllowAdClaim || m_IsClaiming || m_RewardValue <= 0f)
            return;

        m_IsClaiming = true;
        CommonHelper.ShowVideoAd(RewardSourceConst.scratchCardReward, isOk =>
        {
            if (isOk)
            {
                CommonHelper.LogEvent(AdjustEventCodeEvent.reward_collect, new Dictionary<string, string>() {{"placement", "scratchCard"}});
                CollectReward(m_RewardRate);
                CloseRewardAndScratchCardForms();
            }

            m_IsClaiming = false;
        });
    }

    void OnCancelButtonClick()
    {
        if (!m_AllowCancel || m_IsClaiming)
            return;

        CommonHelper.ShowInterstitial(RewardSourceConst.scratchCardInterstitial);
        CollectReward(1);
        CloseRewardAndScratchCardForms();
    }

    void CloseRewardAndScratchCardForms()
    {
        GF.UI.Close(UIForm);
        GF.UI.CloseUIForms(UIViews.ArrowScratchCardUIForm);
    }

    void CollectReward(int rate)
    {
        float value = (float)Math.Round(m_RewardValue*rate, 3, MidpointRounding.AwayFromZero);
        if (value <= 0f)
            return;

        GF.Event.Fire(this, RewardCollectedEventArgs.Create(
            value,
            Vector3.zero,
            PlayerDataType.Diamond));
    }
}
