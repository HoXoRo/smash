using System;
using System.Collections;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowRewardUIForm")]
public partial class ArrowRewardUIForm : UIFormBase
{
    public const string P_RewardValue = "RewardValue";

    const float ElementShowDuration = 0.3f;
    const float ElementShowStartScale = 0.3f;
    const float RewardCountRollDuration = 0.8f;
    const float BreathScale = 1.08f;
    const float BreathDuration = 0.8f;

    float m_RewardValue;
    bool m_IsClaiming;
    bool m_AllowClaim;
    Coroutine m_PresentationCoroutine;
    Tween m_ClaimBreathTween;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ResolveUIReferences();
        m_RewardValue = Params.Get<VarSingle>(P_RewardValue)?.Value ?? 0f;
        m_IsClaiming = false;
        m_AllowClaim = false;
        RefreshBalanceDisplay();
        PreparePresentationVisuals();
        StopPresentationAnimation();
        m_PresentationCoroutine = StartCoroutine(PlayPresentationSequence());
        GF.Sound.PlayEffect("arrowPlay/reward.mp3");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopPresentationAnimation();
        m_IsClaiming = false;
        m_AllowClaim = false;
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Button_Claim":
            case "Button_AdClaim":
                OnClaimButtonClick();
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

        varImgTitle = varImgTitle ?? FindPresentationObject("imgTitle");
        varButton_Claim = varButton_Claim ?? FindPresentationObject("Button_Claim");
        varRewardValue = varRewardValue ?? FindPresentationObject("rewardValue");
        varBalanceBg = varBalanceBg ?? FindPresentationObject("balanceBg");
        if (varImgIcon == null)
        {
            var iconTransform = transform.Find("imgIcon");
            if (iconTransform == null)
            {
                var transforms = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == "imgIcon")
                    {
                        varImgIcon = transforms[i].GetComponent<UnityEngine.UI.Image>();
                        break;
                    }
                }
            }
            else
            {
                varImgIcon = iconTransform.GetComponent<UnityEngine.UI.Image>();
            }
        }
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

    GameObject GetImgIconObject()
    {
        return varImgIcon != null ? varImgIcon.gameObject : null;
    }

    void PreparePresentationVisuals()
    {
        SetElementActive(GetImgIconObject(), false);
        SetElementActive(varImgTitle, false);
        SetElementActive(varRewardValue, false);
        // SetElementActive(varBalanceBg, false);
        SetElementActive(varButton_Claim, false);

        if (varRewardCount != null)
            varRewardCount.text = FormatRewardValue(0f);
    }

    IEnumerator PlayPresentationSequence()
    {
        yield return ShowScaledElementsParallel(GetImgIconObject());
        yield return ShowScaledElementsParallel(
            varImgTitle,
            varRewardValue,
            // varBalanceBg,
            varButton_Claim);
        yield return RollRewardCount(0f, m_RewardValue);
        StartBreathEffect(varButton_Claim != null ? varButton_Claim.transform : null, ref m_ClaimBreathTween);
        m_AllowClaim = true;
        m_PresentationCoroutine = null;
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

    IEnumerator RollRewardCount(float fromValue, float toValue)
    {
        if (varRewardCount == null)
            yield break;

        GF.Sound.PlayEffect("arrowPlay/digitalScroll.mp3");
        varRewardCount.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < RewardCountRollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / RewardCountRollDuration);
            float currentValue = Mathf.Lerp(fromValue, toValue, progress);
            varRewardCount.text = FormatRewardValue(currentValue);
            yield return null;
        }

        varRewardCount.text = FormatRewardValue(toValue);
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

        StopBreathEffect(ref m_ClaimBreathTween);
        KillTransformTween(GetImgIconObject() != null ? GetImgIconObject().transform : null);
        KillTransformTween(varImgTitle != null ? varImgTitle.transform : null);
        KillTransformTween(varRewardValue != null ? varRewardValue.transform : null);
        // KillTransformTween(varBalanceBg != null ? varBalanceBg.transform : null);
        KillTransformTween(varButton_Claim != null ? varButton_Claim.transform : null);
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

    void RefreshBalanceDisplay()
    {
        if (varBalance != null)
            varBalance.text = $"≈{FormatRewardValue(m_RewardValue, true)}";
    }

    static string FormatRewardValue(float value, bool needSymbol = true)
    {
        return CommonHelper.GetDollarString(value, value >= 0.01f, needSymbol);
    }

    void OnClaimButtonClick()
    {
        if (!m_AllowClaim || m_IsClaiming || m_RewardValue <= 0f)
            return;

        m_IsClaiming = true;
        // CommonHelper.LogEvent(AdjustEventCodeEvent.bubble_reward_claim);
        CollectReward();
        GF.UI.Close(UIForm);
        m_IsClaiming = false;
    }

    void CollectReward()
    {
        float value = (float)Math.Round(m_RewardValue, 3, MidpointRounding.AwayFromZero);
        if (value <= 0f)
            return;

        GF.Event.Fire(this, RewardCollectedEventArgs.Create(
            value,
            Vector3.zero,
            PlayerDataType.Diamond));
    }
}
