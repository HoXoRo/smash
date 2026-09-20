using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowScratchCardUIForm")]
public partial class ArrowScratchCardUIForm : UIFormBase
{
    public const string P_OnScratchCardReward = "OnScratchCardReward";
    public const string P_ScratchCardTotalReward = "ScratchCardTotalReward";
    public const string P_SourceWorldPosition = "SourceWorldPosition";
    public const string P_SourceWorldSize = "SourceWorldSize";

    const float EnterMatchScaleDuration = 0.22f;
    const float EnterMoveScaleDuration = 0.48f;
    const float EnterButtonShowDuration = 0.24f;
    const float EnterStartScaleFactor = 0.45f;

    const int ScratchIconCount = 8;
    const int WinIconArrayIndex = 8;
    const int IconIndexMin = 1;
    const int IconIndexMax = 9;
    const int MinMatchCount = 1;
    const int MaxMatchCount = 3;
    const string ScratchIconPathFormat = "UI/ScratchCard/{0}.png";
    const string ScratchCardWinRateKey = "ScratchCardWinRate";
    const string ScratchCardMatchCountWeightsKey = "ScratchCardMatchCountWeights";
    const string DefaultScratchCardMatchCountWeights = "10,10,10";
    const float DecoyRewardMin = 50f;
    const float DecoyRewardMax = 500f;
    const float MatchIconBreathScale = 1.12f;
    const float MatchIconBreathDuration = 0.6f;
    const float RewardPopupDelaySeconds = 1f;
    const string MatchWinColorHex = "#C54B23";
    static readonly string[] s_ScratchIconNodeNames =
    {
        "icon1", "icon2", "icon3", "icon4", "icon5", "icon6", "icon7", "icon8", "winIcon"
    };
    static readonly string[] s_ScratchTxtNodeNames =
    {
        "txt1", "txt2", "txt3", "txt4", "txt5", "txt6", "txt7", "txt8"
    };

    ScratchCardCoatEraser m_CoatEraser;
    Action<ScratchCardRewardInfo> m_RewardCallback;
    TextMeshProUGUI[] m_TxtArr = new TextMeshProUGUI[ScratchIconCount];
    int[] m_ScratchIconIndices = new int[ScratchIconCount];
    float[] m_SlotRewardValues = new float[ScratchIconCount];
    float m_TotalRewardValue;
    int m_WinIconIndex;
    int m_MatchCount;
    bool m_ShouldWin;
    int m_PendingAssetLoads;
    bool m_IsSettled;
    bool m_DefaultColorsCached;
    Coroutine m_RewardPopupDelayCoroutine;
    readonly List<Tween> m_MatchIconBreathTweens = new List<Tween>(3);
    readonly Color[] m_DefaultIconColors = new Color[ScratchIconCount];
    readonly Color[] m_DefaultTxtColors = new Color[ScratchIconCount];
    readonly bool[] m_DefaultTxtVertexGradient = new bool[ScratchIconCount];
    static readonly Color s_MatchWinColor = ParseMatchWinColor();
    Sequence m_EnterSequence;
    Coroutine m_EnterAnimationCoroutine;
    Vector3 m_ScratchCardFinalWorldPosition;
    Vector3 m_ScratchCardFinalLocalScale;
    Vector3 m_ScratchCardLayoutLocalScale = Vector3.one;
    Vector2 m_ScratchCardFinalWorldSize;
    Vector3 m_ButtonCloseDefaultScale = Vector3.one;
    Vector3 m_ButtonCheckCardDefaultScale = Vector3.one;
    bool m_EnterAnimationCompleted;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        ResolveUIReferences();
        EnsureCoatEraser();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Sound.PlayEffect("arrowPlay/scratchCard.mp3");
        // CommonHelper.LogEvent(AdjustEventCodeEvent.scratchCard_enter);
        CommonHelper.LogEvent(AdjustEventCodeEvent.popu_enter, new Dictionary<string, string>() {{"popu", "scratchCardUI"}});


        m_IsSettled = false;
        m_EnterAnimationCompleted = false;
        m_RewardCallback = Params.Get<VarScratchCardRewardCallback>(P_OnScratchCardReward)?.Value;
        m_TotalRewardValue = Params.Get<VarSingle>(P_ScratchCardTotalReward)?.Value ?? 0f;
        ResolveUIReferences();
        PrepareScratchCardEnterHidden();
        EnsureCoatEraser();
        if (m_CoatEraser != null)
        {
            m_CoatEraser.RevealCompleted -= OnCoatRevealCompleted;
            m_CoatEraser.RevealCompleted += OnCoatRevealCompleted;
        }
        SetActionButtonsVisible(false);
        SetupScratchCard();
        m_EnterAnimationCoroutine = StartCoroutine(PlayEnterAnimationAfterLayout());
    }

    void PrepareScratchCardEnterHidden()
    {
        if (varScratchCard == null)
            return;

        RectTransform scratchCardRect = varScratchCard.transform as RectTransform;
        if (scratchCardRect == null)
            return;

        DOTween.Kill(scratchCardRect);
        m_ScratchCardLayoutLocalScale = scratchCardRect.localScale;
        if (Mathf.Approximately(m_ScratchCardLayoutLocalScale.x, 0f))
            m_ScratchCardLayoutLocalScale = Vector3.one;
        m_ScratchCardFinalWorldSize = MeasureRectWorldSize(scratchCardRect);
        scratchCardRect.localScale = Vector3.zero;
    }

    protected override void OnOpenAnimationComplete()
    {
    }

    void TryEnableScratchCardInteraction()
    {
        if (!m_EnterAnimationCompleted)
            return;
        if (m_CoatEraser != null && m_CoatEraser.IsInitialized)
            Interactable = true;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (m_CoatEraser != null)
            m_CoatEraser.RevealCompleted -= OnCoatRevealCompleted;

        StopMatchIconBreathEffect();
        StopRewardPopupDelay();
        StopEnterAnimation();
        m_RewardCallback = null;
        base.OnClose(isShutdown, userData);
    }

    IEnumerator PlayEnterAnimationAfterLayout()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        m_EnterAnimationCoroutine = null;
        if (!ApplyScratchCardEnterStartState())
        {
            RectTransform scratchCardRect = varScratchCard != null ? varScratchCard.transform as RectTransform : null;
            if (scratchCardRect != null)
                PlayFallbackEnterAnimation(scratchCardRect);
            else
                OnEnterAnimationComplete();
            yield break;
        }

        PlayEnterTween();
    }

    bool ApplyScratchCardEnterStartState()
    {
        if (varScratchCard == null)
            return false;

        RectTransform scratchCardRect = varScratchCard.transform as RectTransform;
        if (scratchCardRect == null)
            return false;

        m_ScratchCardFinalWorldPosition = scratchCardRect.position;
        m_ScratchCardFinalLocalScale = m_ScratchCardLayoutLocalScale;

        Vector3 sourceWorldPosition = Params.Get<VarVector3>(P_SourceWorldPosition)?.Value ?? Vector3.zero;
        Vector2 sourceWorldSize = Params.Get<VarVector2>(P_SourceWorldSize)?.Value ?? Vector2.zero;
        if (sourceWorldSize.x <= 0f || sourceWorldSize.y <= 0f
            || !TryGetScratchCardEnterStartState(
                sourceWorldPosition, sourceWorldSize, scratchCardRect,
                out Vector3 startMatchScale))
            return false;

        scratchCardRect.position = sourceWorldPosition;
        scratchCardRect.localScale = startMatchScale * EnterStartScaleFactor;
        return true;
    }

    void PlayEnterTween()
    {
        StopEnterAnimation();
        if (varScratchCard == null)
        {
            OnEnterAnimationComplete();
            return;
        }

        RectTransform scratchCardRect = varScratchCard.transform as RectTransform;
        if (scratchCardRect == null)
        {
            OnEnterAnimationComplete();
            return;
        }

        if (!TryGetScratchCardEnterStartState(
                Params.Get<VarVector3>(P_SourceWorldPosition)?.Value ?? Vector3.zero,
                Params.Get<VarVector2>(P_SourceWorldSize)?.Value ?? Vector2.zero,
                scratchCardRect,
                out Vector3 startMatchScale))
        {
            OnEnterAnimationComplete();
            return;
        }

        m_EnterSequence = DOTween.Sequence();
        m_EnterSequence.Append(
            scratchCardRect.DOScale(startMatchScale, EnterMatchScaleDuration)
                .SetEase(Ease.OutBack).SetUpdate(true));
        m_EnterSequence.Append(
            scratchCardRect.DOMove(m_ScratchCardFinalWorldPosition, EnterMoveScaleDuration)
                .SetEase(Ease.InOutCubic).SetUpdate(true));
        m_EnterSequence.Join(
            scratchCardRect.DOScale(m_ScratchCardFinalLocalScale, EnterMoveScaleDuration)
                .SetEase(Ease.OutCubic).SetUpdate(true));
        m_EnterSequence.OnComplete(OnEnterAnimationComplete);
        m_EnterSequence.SetUpdate(true);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Button_Close":
                CommonHelper.LogEvent(AdjustEventCodeEvent.reward_cancel, new Dictionary<string, string>() {{"placement", "scratchCard"}});

                GF.UI.Close(UIForm);
                break;
            case "Button_checkCard":
                OnCheckCardButtonClick();
                break;
        }
    }

    protected virtual void GrantScratchCardReward(ScratchCardRewardInfo rewardInfo)
    {
        m_RewardCallback?.Invoke(rewardInfo);
    }

    void EnsureCoatEraser()
    {
        if (varCoat == null)
            return;

        m_CoatEraser = varCoat.GetComponent<ScratchCardCoatEraser>();
        if (m_CoatEraser == null)
            m_CoatEraser = varCoat.AddComponent<ScratchCardCoatEraser>();
    }

    void PlayFallbackEnterAnimation(RectTransform scratchCardRect)
    {
        if (scratchCardRect == null)
        {
            OnEnterAnimationComplete();
            return;
        }

        if (Mathf.Approximately(scratchCardRect.localScale.x, 0f))
        {
            m_ScratchCardFinalWorldPosition = scratchCardRect.position;
            m_ScratchCardFinalLocalScale = m_ScratchCardLayoutLocalScale;
        }

        scratchCardRect.localScale = Vector3.zero;

        m_EnterSequence = DOTween.Sequence();
        m_EnterSequence.Append(
            scratchCardRect.DOScale(m_ScratchCardFinalLocalScale, EnterMoveScaleDuration)
                .SetEase(Ease.OutBack).SetUpdate(true));
        m_EnterSequence.OnComplete(OnEnterAnimationComplete);
        m_EnterSequence.SetUpdate(true);
    }

    void OnEnterAnimationComplete()
    {
        m_EnterAnimationCompleted = true;
        if (varScratchCard != null)
        {
            RectTransform scratchCardRect = varScratchCard.transform as RectTransform;
            if (scratchCardRect != null)
            {
                scratchCardRect.position = m_ScratchCardFinalWorldPosition;
                scratchCardRect.localScale = m_ScratchCardFinalLocalScale;
            }
        }

        ShowActionButtonsAnimated();
        TryEnableScratchCardInteraction();
    }

    public static bool TryCaptureWorldRect(RectTransform rectTransform, out Vector3 worldCenter, out Vector2 worldSize)
    {
        worldCenter = Vector3.zero;
        worldSize = Vector2.zero;
        if (rectTransform == null)
            return false;

        worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
        worldSize = GetRectWorldSize(rectTransform, rectTransform.localScale);
        return worldSize.x > 0f && worldSize.y > 0f;
    }

    bool TryGetScratchCardEnterStartState(
        Vector3 sourceWorldPosition,
        Vector2 sourceWorldSize,
        RectTransform scratchCardRect,
        out Vector3 startMatchScale)
    {
        startMatchScale = m_ScratchCardFinalLocalScale;
        if (sourceWorldSize.x <= 0f || sourceWorldSize.y <= 0f)
            return false;

        Vector2 finalWorldSize = m_ScratchCardFinalWorldSize;
        if (finalWorldSize.x <= 0f || finalWorldSize.y <= 0f)
            finalWorldSize = GetRectWorldSize(scratchCardRect, m_ScratchCardFinalLocalScale);
        if (finalWorldSize.x <= 0f || finalWorldSize.y <= 0f)
            return false;

        float matchScale = Mathf.Min(
            sourceWorldSize.x / finalWorldSize.x,
            sourceWorldSize.y / finalWorldSize.y);
        matchScale = Mathf.Max(0.01f, matchScale);
        startMatchScale = m_ScratchCardFinalLocalScale * matchScale;
        return true;
    }

    static Vector2 MeasureRectWorldSize(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return new Vector2(
            Vector3.Distance(corners[0], corners[3]),
            Vector3.Distance(corners[0], corners[1]));
    }

    static Vector2 GetRectWorldSize(RectTransform rectTransform, Vector3 localScale)
    {
        Vector3 savedScale = rectTransform.localScale;
        rectTransform.localScale = localScale;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 size = new Vector2(
            Vector3.Distance(corners[0], corners[3]),
            Vector3.Distance(corners[0], corners[1]));

        rectTransform.localScale = savedScale;
        return size;
    }

    void SetActionButtonsVisible(bool visible)
    {
        if (varButton_Close != null)
            varButton_Close.SetActive(visible);
        if (varButton_checkCard != null)
            varButton_checkCard.SetActive(visible);
    }

    void ShowActionButtonsAnimated()
    {
        ShowActionButtonAnimated(varButton_Close, m_ButtonCloseDefaultScale);
        ShowActionButtonAnimated(varButton_checkCard, m_ButtonCheckCardDefaultScale);
    }

    void ShowActionButtonAnimated(GameObject buttonObject, Vector3 targetScale)
    {
        if (buttonObject == null)
            return;

        buttonObject.SetActive(true);
        Transform buttonTransform = buttonObject.transform;
        buttonTransform.localScale = Vector3.zero;
        buttonTransform.DOScale(targetScale, EnterButtonShowDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    void StopEnterAnimation()
    {
        if (m_EnterAnimationCoroutine != null)
        {
            StopCoroutine(m_EnterAnimationCoroutine);
            m_EnterAnimationCoroutine = null;
        }

        if (m_EnterSequence != null && m_EnterSequence.IsActive())
            m_EnterSequence.Kill();
        m_EnterSequence = null;

        if (varScratchCard != null)
            DOTween.Kill(varScratchCard.transform);
        if (varButton_Close != null)
            DOTween.Kill(varButton_Close.transform);
        if (varButton_checkCard != null)
            DOTween.Kill(varButton_checkCard.transform);
    }

    void CacheActionButtonDefaultScales()
    {
        if (varButton_Close != null)
            m_ButtonCloseDefaultScale = varButton_Close.transform.localScale;
        if (varButton_checkCard != null)
            m_ButtonCheckCardDefaultScale = varButton_checkCard.transform.localScale;
    }

    void ResolveUIReferences()
    {
        if (varScratchCard == null)
        {
            Transform scratchCardTransform = transform.Find("doTween/scratchCard");
            if (scratchCardTransform == null)
                scratchCardTransform = transform.Find("scratchCard");

            if (scratchCardTransform != null)
                varScratchCard = scratchCardTransform.gameObject;
        }

        if (varCoat == null && varScratchCard != null)
        {
            Transform coatTransform = varScratchCard.transform.Find("coat");
            if (coatTransform != null)
                varCoat = coatTransform.gameObject;
        }

        if (varIcon1Arr == null || varIcon1Arr.Length <= WinIconArrayIndex)
            varIcon1Arr = BuildIconArrayFromHierarchy();

        if (!HasValidTxtArray())
            m_TxtArr = BuildTxtArrayFromHierarchy();

        CacheActionButtonDefaultScales();
    }

    bool HasValidTxtArray()
    {
        if (m_TxtArr == null || m_TxtArr.Length < ScratchIconCount)
            return false;

        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (m_TxtArr[i] == null)
                return false;
        }

        return true;
    }

    Image[] BuildIconArrayFromHierarchy()
    {
        if (varScratchCard == null)
            return varIcon1Arr;

        var icons = new Image[s_ScratchIconNodeNames.Length];
        for (int i = 0; i < s_ScratchIconNodeNames.Length; i++)
        {
            Transform iconTransform = varScratchCard.transform.Find(s_ScratchIconNodeNames[i]);
            icons[i] = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        return icons;
    }

    TextMeshProUGUI[] BuildTxtArrayFromHierarchy()
    {
        var txts = new TextMeshProUGUI[ScratchIconCount];
        if (varScratchCard == null)
            return txts;

        for (int i = 0; i < s_ScratchTxtNodeNames.Length; i++)
        {
            Transform iconRoot = varScratchCard.transform.Find(s_ScratchIconNodeNames[i]);
            Transform txtTransform = iconRoot != null
                ? iconRoot.Find(s_ScratchTxtNodeNames[i])
                : null;
            if (txtTransform == null)
                txtTransform = FindDeepChild(varScratchCard.transform, s_ScratchTxtNodeNames[i]);

            txts[i] = txtTransform != null ? txtTransform.GetComponent<TextMeshProUGUI>() : null;
            if (txts[i] == null)
                Log.Warning($"ArrowScratchCardUIForm: cannot find {s_ScratchTxtNodeNames[i]}.");
        }

        return txts;
    }

    static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    void SetupScratchCard()
    {
        if (varCoat == null)
        {
            Log.Error("ArrowScratchCardUIForm: varCoat is missing.");
            return;
        }

        varCoat.SetActive(true);
        EnsureCoatEraser();
        if (m_CoatEraser == null)
        {
            Log.Error("ArrowScratchCardUIForm: ScratchCardCoatEraser is missing.");
            return;
        }

        m_CoatEraser.Initialize();
        if (!m_CoatEraser.IsInitialized)
        {
            Log.Error("ArrowScratchCardUIForm: ScratchCardCoatEraser initialize failed.");
            return;
        }

        StopMatchIconBreathEffect();
        CacheDefaultSlotColors();
        ResetSlotVisualState();

        if (varIcon1Arr == null || varIcon1Arr.Length <= WinIconArrayIndex)
        {
            Log.Error("ArrowScratchCardUIForm: icon references are invalid.");
            return;
        }

        if (!HasValidTxtArray())
        {
            Log.Error("ArrowScratchCardUIForm: txt references are invalid.");
            return;
        }

        m_WinIconIndex = UnityEngine.Random.Range(IconIndexMin, IconIndexMax + 1);
        m_ShouldWin = RollScratchCardWin();
        m_MatchCount = m_ShouldWin ? RollMatchCount() : 0;
        GenerateScratchSlots();

        m_PendingAssetLoads = ScratchIconCount + 1;
        for (int i = 0; i < ScratchIconCount; i++)
            SetupScratchSlot(i);

        LoadScratchIcon(varIcon1Arr[WinIconArrayIndex], m_WinIconIndex);
    }

    bool RollScratchCardWin()
    {
        int winRate = GF.Config.GetInt(ScratchCardWinRateKey);
        winRate = Mathf.Clamp(winRate, 0, 100);
        return UnityEngine.Random.Range(0, 100) < winRate;
    }

    int RollMatchCount()
    {
        int[] weights = ParseIntArray(
            GF.Config.GetString(ScratchCardMatchCountWeightsKey, DefaultScratchCardMatchCountWeights));
        if (weights == null || weights.Length == 0)
            return UnityEngine.Random.Range(MinMatchCount, MaxMatchCount + 1);

        int totalWeight = 0;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += Mathf.Max(0, weights[i]);

        if (totalWeight <= 0)
            return UnityEngine.Random.Range(MinMatchCount, MaxMatchCount + 1);

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int accumulated = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            accumulated += Mathf.Max(0, weights[i]);
            if (roll < accumulated)
                return Mathf.Clamp(i + 1, MinMatchCount, MaxMatchCount);
        }

        return MaxMatchCount;
    }

    void GenerateScratchSlots()
    {
        Array.Clear(m_SlotRewardValues, 0, m_SlotRewardValues.Length);

        if (m_ShouldWin && m_MatchCount > 0)
        {
            var winSlots = PickRandomSlotIndices(m_MatchCount);
            float[] splitRewards = SplitRewardUnequally(
                Mathf.Max(0.01f, m_TotalRewardValue),
                m_MatchCount);

            for (int i = 0; i < ScratchIconCount; i++)
                m_ScratchIconIndices[i] = PickNonWinIconIndex();

            for (int i = 0; i < winSlots.Count; i++)
            {
                int slotIndex = winSlots[i];
                m_ScratchIconIndices[slotIndex] = m_WinIconIndex;
                m_SlotRewardValues[slotIndex] = splitRewards[i];
            }

            FillDecoySlotRewards();
            return;
        }

        m_MatchCount = 0;
        for (int i = 0; i < ScratchIconCount; i++)
            m_ScratchIconIndices[i] = PickNonWinIconIndex();

        FillDecoySlotRewards();
    }

    void FillDecoySlotRewards()
    {
        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (m_SlotRewardValues[i] > 0f)
                continue;

            m_SlotRewardValues[i] = RollDecoyRewardValue();
        }
    }

    static float RollDecoyRewardValue()
    {
        return RoundRewardValue(UnityEngine.Random.Range(DecoyRewardMin, DecoyRewardMax));
    }

    static float RoundRewardValue(float value)
    {
        return (float)Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    static List<int> PickRandomSlotIndices(int count)
    {
        var slotPool = new List<int>(ScratchIconCount);
        for (int i = 0; i < ScratchIconCount; i++)
            slotPool.Add(i);

        for (int i = slotPool.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (slotPool[i], slotPool[swapIndex]) = (slotPool[swapIndex], slotPool[i]);
        }

        return slotPool.GetRange(0, Mathf.Clamp(count, 0, ScratchIconCount));
    }

    int PickNonWinIconIndex()
    {
        int iconIndex;
        do
        {
            iconIndex = UnityEngine.Random.Range(IconIndexMin, IconIndexMax + 1);
        }
        while (iconIndex == m_WinIconIndex);

        return iconIndex;
    }

    static float[] SplitRewardUnequally(float total, int parts)
    {
        if (parts <= 0)
            return Array.Empty<float>();

        total = RoundRewardValue(Mathf.Max(0.01f * parts, total));
        if (parts == 1)
            return new[] { total };

        var weights = new float[parts];
        float weightSum = 0f;
        for (int i = 0; i < parts; i++)
        {
            weights[i] = UnityEngine.Random.Range(1f, 100f);
            weightSum += weights[i];
        }

        var result = new float[parts];
        float remaining = total;
        const float minValue = 0.01f;
        for (int i = 0; i < parts - 1; i++)
        {
            int slotsLeft = parts - i;
            float maxValue = RoundRewardValue(remaining - minValue * (slotsLeft - 1));
            float portion = RoundRewardValue(total * (weights[i] / weightSum));
            portion = Mathf.Clamp(portion, minValue, maxValue);
            result[i] = portion;
            remaining = RoundRewardValue(remaining - portion);
        }

        result[parts - 1] = RoundRewardValue(remaining);
        return result;
    }

    static int[] ParseIntArray(string configValue)
    {
        if (string.IsNullOrWhiteSpace(configValue))
            return Array.Empty<int>();

        string[] parts = configValue.Split(',');
        var result = new List<int>(parts.Length);
        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i].Trim(), out int value))
                result.Add(value);
        }

        return result.ToArray();
    }

    void SetupScratchSlot(int slotIndex)
    {
        Image icon = varIcon1Arr[slotIndex];
        TextMeshProUGUI txt = m_TxtArr[slotIndex];
        float rewardValue = m_SlotRewardValues[slotIndex];

        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.transform.localScale = Vector3.one;
            LoadScratchIcon(icon, m_ScratchIconIndices[slotIndex]);
        }
        
        if (txt == null)
            return;

        txt.gameObject.SetActive(true);
        txt.text = FormatSlotRewardText(rewardValue);
        txt.transform.localScale = Vector3.one;
    }

    static string FormatSlotRewardText(float rewardValue)
    {
        return CommonHelper.GetDollarString(rewardValue, rewardValue>=0.01f);
    }

    void LoadScratchIcon(Image target, int iconIndex)
    {
        if (target == null)
        {
            OnOneAssetLoaded();
            return;
        }

        string spritePath = UtilityBuiltin.AssetsPath.GetSpritesPath(
            string.Format(ScratchIconPathFormat, iconIndex));
        GF.UI.LoadSprite(spritePath, sprite =>
        {
            if (target != null && sprite != null)
                target.sprite = sprite;

            OnOneAssetLoaded();
        });
    }

    void OnOneAssetLoaded()
    {
        if (m_PendingAssetLoads <= 0)
            return;

        m_PendingAssetLoads--;
        TryEnableScratchCardInteraction();
    }

    void OnCheckCardButtonClick()
    {
        if (m_IsSettled || m_CoatEraser == null || !m_CoatEraser.IsInitialized)
            return;

        m_CoatEraser.SetInputEnabled(false);
        m_CoatEraser.RevealLeftToRight();
    }

    void OnCoatRevealCompleted()
    {
        if (m_IsSettled)
            return;

        m_IsSettled = true;
        m_CoatEraser.SetInputEnabled(false);

        ScratchCardRewardInfo rewardInfo = EvaluateReward();
        if (rewardInfo.IsWin)
        {
            PlayMatchIconBreathEffect();
            ApplyMatchWinHighlight();
            StopRewardPopupDelay();
            m_RewardPopupDelayCoroutine = StartCoroutine(DelayGrantReward(rewardInfo));
            return;
        }

        GrantScratchCardReward(rewardInfo);
    }

    IEnumerator DelayGrantReward(ScratchCardRewardInfo rewardInfo)
    {
        yield return new WaitForSecondsRealtime(RewardPopupDelaySeconds);
        m_RewardPopupDelayCoroutine = null;
        GrantScratchCardReward(rewardInfo);
    }

    void StopRewardPopupDelay()
    {
        if (m_RewardPopupDelayCoroutine == null)
            return;

        StopCoroutine(m_RewardPopupDelayCoroutine);
        m_RewardPopupDelayCoroutine = null;
    }

    void PlayMatchIconBreathEffect()
    {
        StopMatchIconBreathTweens();
        if (varIcon1Arr == null || varIcon1Arr.Length <= WinIconArrayIndex)
            return;

        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (m_ScratchIconIndices[i] != m_WinIconIndex)
                continue;

            StartIconBreath(varIcon1Arr[i]?.transform);
        }
    }

    void StartIconBreath(Transform target)
    {
        if (target == null)
            return;

        target.localScale = Vector3.one;
        Tween breathTween = target
            .DOScale(Vector3.one * MatchIconBreathScale, MatchIconBreathDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
        m_MatchIconBreathTweens.Add(breathTween);
    }

    void StopMatchIconBreathEffect()
    {
        StopMatchIconBreathTweens();
        ResetSlotVisualState();
    }

    void StopMatchIconBreathTweens()
    {
        for (int i = 0; i < m_MatchIconBreathTweens.Count; i++)
        {
            if (m_MatchIconBreathTweens[i] != null && m_MatchIconBreathTweens[i].IsActive())
                m_MatchIconBreathTweens[i].Kill();
        }

        m_MatchIconBreathTweens.Clear();

        if (varIcon1Arr == null)
            return;

        for (int i = 0; i < ScratchIconCount && i < varIcon1Arr.Length; i++)
        {
            if (varIcon1Arr[i] != null)
                varIcon1Arr[i].transform.localScale = Vector3.one;
        }
    }

    static Color ParseMatchWinColor()
    {
        return ColorUtility.TryParseHtmlString(MatchWinColorHex, out Color color)
            ? color
            : new Color(0.772549f, 0.294118f, 0.137255f, 1f);
    }

    void CacheDefaultSlotColors()
    {
        if (varIcon1Arr == null || !HasValidTxtArray())
            return;

        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (varIcon1Arr[i] != null)
                m_DefaultIconColors[i] = varIcon1Arr[i].color;

            if (m_TxtArr[i] != null)
            {
                m_DefaultTxtColors[i] = m_TxtArr[i].color;
                m_DefaultTxtVertexGradient[i] = m_TxtArr[i].enableVertexGradient;
            }
        }

        m_DefaultColorsCached = true;
    }

    void ApplyMatchWinHighlight()
    {
        if (varIcon1Arr == null || !HasValidTxtArray())
            return;

        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (m_ScratchIconIndices[i] != m_WinIconIndex)
                continue;

            Image icon = varIcon1Arr[i];
            if (icon != null)
                icon.color = s_MatchWinColor;

            TextMeshProUGUI txt = m_TxtArr[i];
            if (txt != null)
                ApplyTxtWinColor(txt);
        }
    }

    static void ApplyTxtWinColor(TextMeshProUGUI txt)
    {
        txt.enableVertexGradient = false;
        txt.color = s_MatchWinColor;
        txt.colorGradient = new VertexGradient(
            s_MatchWinColor,
            s_MatchWinColor,
            s_MatchWinColor,
            s_MatchWinColor);
        txt.ForceMeshUpdate();
    }

    void ResetSlotVisualState()
    {
        if (varIcon1Arr != null)
        {
            for (int i = 0; i < ScratchIconCount && i < varIcon1Arr.Length; i++)
            {
                if (varIcon1Arr[i] == null)
                    continue;

                varIcon1Arr[i].transform.localScale = Vector3.one;
                if (m_DefaultColorsCached)
                    varIcon1Arr[i].color = m_DefaultIconColors[i];
            }
        }

        if (m_TxtArr == null)
            return;

        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (m_TxtArr[i] == null)
                continue;

            m_TxtArr[i].transform.localScale = Vector3.one;
            if (!m_DefaultColorsCached)
                continue;

            m_TxtArr[i].enableVertexGradient = m_DefaultTxtVertexGradient[i];
            m_TxtArr[i].color = m_DefaultTxtColors[i];
        }
    }

    ScratchCardRewardInfo EvaluateReward()
    {
        int matchCount = 0;
        for (int i = 0; i < ScratchIconCount; i++)
        {
            if (m_ScratchIconIndices[i] == m_WinIconIndex)
                matchCount++;
        }

        bool isWin = matchCount >= MinMatchCount;
        return ScratchCardRewardInfo.Create(
            isWin,
            m_WinIconIndex,
            matchCount,
            m_TotalRewardValue,
            (float[])m_SlotRewardValues.Clone());
    }
}
