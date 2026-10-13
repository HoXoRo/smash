using System.Collections;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowUITopbarSub")]

public partial class ArrowUITopbarSub : UIFormBase
{
    [SerializeField] private GameObject flyRewardItemPrefab;
    [SerializeField] private int minRewardCount = 5;
    [SerializeField] private int maxRewardCount = 10;
    [SerializeField] private float numberRollDuration = 0.5f;

    private static bool s_ShowCoin = true;
    private static bool s_ShowLife;
    private PlayerDataModel playerDm;
    private float pendingCoins;
    private float targetCoins;
    private float rollTarget;
    private int activeRewards;
    private int rewardGeneration;
    private bool rolling;
    private Coroutine rollCoroutine;

    public static void EnsureOpen()
    {
        if (GF.UI.HasUIForm(UIViews.ArrowUITopbarSub) || GF.UI.IsLoadingUIForm(UIViews.ArrowUITopbarSub)) return;
        GF.UI.OpenUIForm(UIViews.ArrowUITopbarSub);
    }

    public static bool CanAnimateCoins()
    {
        string assetName = GF.UI.GetUIFormAssetName(UIViews.ArrowUITopbarSub);
        if (string.IsNullOrEmpty(assetName)) return false;
        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null) return false;
        foreach (var form in forms)
        {
            if (form.Logic is ArrowUITopbarSub topbar && topbar.isActiveAndEnabled &&
                topbar.gameObject.activeInHierarchy && topbar.playerDm != null) return true;
        }
        return false;
    }

    public static void SetCoinVisible(bool visible)
    {
        s_ShowCoin = visible;
        string assetName = GF.UI.GetUIFormAssetName(UIViews.ArrowUITopbarSub);
        if (string.IsNullOrEmpty(assetName)) return;
        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null) return;
        foreach (var form in forms)
        {
            if (form.Logic is ArrowUITopbarSub topbar) topbar.RefreshCoinVisibility();
        }
    }

    public static void SetLifeVisible(bool visible)
    {
        s_ShowLife = visible;
        string assetName = GF.UI.GetUIFormAssetName(UIViews.ArrowUITopbarSub);
        if (string.IsNullOrEmpty(assetName)) return;
        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null) return;
        foreach (var form in forms)
        {
            if (form.Logic is ArrowUITopbarSub topbar) topbar.RefreshLifeVisibility();
        }
    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnLife.onClick.AddListener(OpenAddLife);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        pendingCoins = 0f;
        activeRewards = 0;
        rolling = false;
        targetCoins = playerDm.Coins;
        varTxtCoin.text = FormatCoins(targetCoins);
        playerDm.LifeChanged += RefreshLifeDisplay;
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        GF.Event.Subscribe(RewardCollectedEventArgs.EventId, OnRewardCollected);
        playerDm.RefreshLife();
        RefreshLifeDisplay();
        RefreshLifeVisibility();
        RefreshCoinVisibility();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(RewardCollectedEventArgs.EventId, OnRewardCollected);
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        playerDm.LifeChanged -= RefreshLifeDisplay;
        SettleCoins();
        base.OnClose(isShutdown, userData);
    }

    protected override void InternalSetVisible(bool visible)
    {
        base.InternalSetVisible(visible);
        if (!visible && playerDm != null) SettleCoins();
    }

    private void SettleCoins()
    {
        rewardGeneration++;
        if (rollCoroutine != null) StopCoroutine(rollCoroutine);
        rollCoroutine = null;
        float value = (rolling ? rollTarget : targetCoins) + pendingCoins;
        bool hasPendingRewards = rolling || pendingCoins > 0f || activeRewards > 0;
        if (hasPendingRewards)
        {
            if (value > playerDm.Coins) playerDm.SetData(PlayerDataType.Coins, value, false);
            targetCoins = playerDm.Coins;
            varTxtCoin.text = FormatCoins(targetCoins);
            playerDm.Save();
        }
        pendingCoins = 0f;
        activeRewards = 0;
        rolling = false;
        RefreshCoinVisibility();
    }

    private void OnPlayerDataChanged(object sender, GameEventArgs e)
    {
        if (!(e is PlayerDataChangedEventArgs args) || args.DataType != PlayerDataType.Coins) return;
        if (rolling) return;
        targetCoins = args.Value;
        if (activeRewards == 0) varTxtCoin.text = FormatCoins(targetCoins);
    }

    private void OnRewardCollected(object sender, GameEventArgs e)
    {
        if (!(e is RewardCollectedEventArgs args) || args.type != PlayerDataType.Coins) return;
        if (!gameObject.activeInHierarchy) return;

        pendingCoins += args.Value;
        RefreshCoinVisibility();
        if (flyRewardItemPrefab == null || varFlyRewardParent == null || varIcon_Coin == null || !varFlyRewardParent.gameObject.activeInHierarchy)
        {
            StartRoll();
            return;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, args.WorldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(varFlyRewardParent, screenPoint, null, out Vector2 start);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            varFlyRewardParent, RectTransformUtility.WorldToScreenPoint(null, varIcon_Coin.position), null, out Vector2 target);

        int count = args.Value == 1f ? 1 : Random.Range(minRewardCount, maxRewardCount);
        int generation = rewardGeneration;
        activeRewards += count;
        for (int i = 0; i < count; i++)
        {
            var item = SpawnItem<UIItemObject>(flyRewardItemPrefab, varFlyRewardParent).itemLogic as ArrowFlyRewardItem;
            if (item == null)
            {
                activeRewards--;
                continue;
            }
            Vector2 offset = count > 1 ? Random.insideUnitCircle * 110f : Vector2.zero;
            item.rectTransform.anchoredPosition = start + offset;
            var reward = item;
            item.InitReward(PlayerDataType.Coins, target, () =>
            {
                if (generation == rewardGeneration && activeRewards > 0)
                {
                    activeRewards--;
                    if (activeRewards == 0) StartRoll();
                }
                if (reward != null && reward.gameObject != null)
                    UnspawnItem<UIItemObject>(flyRewardItemPrefab, reward.gameObject);
            }, 0.02f * i, i);
        }
        if (activeRewards == 0) StartRoll();
    }

    private void StartRoll()
    {
        if (rolling || activeRewards > 0 || pendingCoins <= 0f) return;
        GF.Sound.PlayEffect("coinCollect.ogg");
        float amount = pendingCoins;
        pendingCoins = 0f;
        rollTarget = targetCoins + amount;
        rolling = true;
        rollCoroutine = StartCoroutine(RollCoins());
    }

    private IEnumerator RollCoins()
    {
        yield return CommonHelper.RollNumber(targetCoins, rollTarget, numberRollDuration,
            value => varTxtCoin.text = FormatCoins(value));
        targetCoins = rollTarget;
        varTxtCoin.text = FormatCoins(targetCoins);
        rollCoroutine = null;
        playerDm.SetData(PlayerDataType.Coins, targetCoins, true);
        playerDm.Save();
        rolling = false;
        StartRoll();
        RefreshCoinVisibility();
    }

    private void RefreshCoinVisibility()
    {
        if (varCoin != null) varCoin.SetActive(s_ShowCoin || activeRewards > 0 || rolling || pendingCoins > 0f);
    }

    private void RefreshLifeVisibility()
    {
        if (varBtnLife != null) varBtnLife.transform.parent.gameObject.SetActive(s_ShowLife);
    }

    private static string FormatCoins(float value)
    {
        return CommonHelper.FormatLargeNumber(value.ToString(), 0);
    }

    private void RefreshLifeDisplay()
    {
        varBtnLife.interactable = playerDm.LifeCount < PlayerDataModel.MaxLifeCount;
        varTxtLife.text = playerDm.LifeCount.ToString();
        int seconds = playerDm.GetSecondsUntilNextLife();
        varTxtTime.text = seconds < 0 ? "FULL" : $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void OpenAddLife()
    {
        playerDm.RefreshLife();
        if (playerDm.LifeCount >= PlayerDataModel.MaxLifeCount) return;
        if (!GF.UI.HasUIForm(UIViews.AddLifeUIForm) && !GF.UI.IsLoadingUIForm(UIViews.AddLifeUIForm))
            GF.UI.OpenUIForm(UIViews.AddLifeUIForm, UIParams.Create());
    }
}
