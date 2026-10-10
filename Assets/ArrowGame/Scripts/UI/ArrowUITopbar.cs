using GameFramework.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;
using TMPro;
using GameFramework;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
// [AddComponentMenu("UI/UITopbar")]
public partial class ArrowUITopbar : UIFormBase
{
    [Header("货币收集动画")]
    [SerializeField] private GameObject flyRewardItemPrefab = null;  // 飞行货币预制体
    [SerializeField] private Transform flyRewardParent = null;  // 飞行货币父节点
    public int minRewardCount = 5;  // 最小生成数量
    public int maxRewardCount = 10;  // 最大生成数量
    public float numberRollDuration = 0.5f;  // 数字滚动时长
    private const float GEM_UP_FLOAT_DURATION = 1f;
    private const float GEM_UP_FLOAT_OFFSET_Y = 60f;
    public const string P_EnableBG = "EnableBG";
    public const string P_EnableSettingBtn = "EnableSettingBtn";
    public const string P_OnCloseAction = "P_OnCloseAction";
    public const string P_EnableLv = "P_EnableLv";
    static bool s_ShowCoin = true;
    
    private Dictionary<PlayerDataType, float> pendingRewards = new Dictionary<PlayerDataType, float>();
    private Dictionary<PlayerDataType, Coroutine> numberRollCoroutines = new Dictionary<PlayerDataType, Coroutine>();
    private Dictionary<PlayerDataType, float> targetValues = new Dictionary<PlayerDataType, float>();
    private Dictionary<PlayerDataType, bool> isRolling = new Dictionary<PlayerDataType, bool>();
    private Dictionary<PlayerDataType, int> activeRewardCounts = new Dictionary<PlayerDataType, int>();  // 每种货币类型的活跃奖励数量
    private Vector2 m_TxtUpGemRestPos;
    private Vector2 m_TxtUpPcardRestPos;
    private Tween m_TxtUpGemTween;
    private Tween m_TxtUpPcardTween;
    public PlayerDataModel playerDm;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnLife != null)
            varBtnLife.onClick.AddListener(OpenAddLife);
        if (varBtnDebug != null)
        {
            varBtnDebug.onClick.AddListener(OpenDebugUIForm);
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        GF.Event.Subscribe(RewardCollectedEventArgs.EventId, OnRewardCollected);
        GF.Event.Subscribe(LargeDollarRewardEventArgs.EventId, OnLargeDollarReward);
        GF.Event.Subscribe(TreasureBoxRewardEventArgs.EventId, OnTreasureBoxReward);
        GF.Event.Subscribe(UserTypeChangeEventArgs.EventId, OnUserTypeChange);
        PcardGiftRewardSession.Changed += OnGiftRewardsChanged;
        varBg.enabled = Params.Get<VarBoolean>(P_EnableBG, true);
       

        playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerDm.LifeChanged += RefreshLifeDisplay;
        playerDm.RefreshLife();
        varTxtCoin.text = CommonHelper.FormatLargeNumber(playerDm.Coins.ToString(), 0);
        varTxtGem.text = CommonHelper.GetDollarString(playerDm.Dollars);
        RefreshPcardText();
        varLvobj.SetActive(Params.Get<VarBoolean>(P_EnableLv, false));
        varLv.text = playerDm.LevelId.ToString();

        // 初始化计数器和目标值
        activeRewardCounts[PlayerDataType.Coins] = 0;
        activeRewardCounts[PlayerDataType.Diamond] = 0;
        activeRewardCounts[PlayerDataType.Pcard] = 0;
        targetValues[PlayerDataType.Coins] = playerDm.Coins;
        targetValues[PlayerDataType.Diamond] = playerDm.Dollars;
        targetValues[PlayerDataType.Pcard] = playerDm.Pcard;
        varGem.SetActive(CommonHelper.IsSpec());

        if (varGuideSpine != null)
            varGuideSpine.SetActive(false);
        if (varTxtUpGem != null)
        {
            m_TxtUpGemRestPos = varTxtUpGem.rectTransform.anchoredPosition;
            varTxtUpGem.gameObject.SetActive(false);
        }
        m_TxtUpPcardRestPos = varTxtUpPcard.rectTransform.anchoredPosition;
        varTxtUpPcard.gameObject.SetActive(false);
        RefreshGiftCount();
        ApplyCoinVisible(s_ShowCoin);
        TryOpenGiftForm();
        //适配刘海屏
        // float safeArea = Mathf.Max(0, Screen.height - Screen.safeArea.yMax);
        // bool isLiuhai = safeArea != 0;
        // varBg.rectTransform.sizeDelta = new Vector2(0, isLiuhai ? 178 + safeArea : 178);
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        playerDm.LifeChanged -= RefreshLifeDisplay;
        //如果正在动画中，直接累加奖励值
        if ((activeRewardCounts.ContainsKey(PlayerDataType.Diamond) && activeRewardCounts[PlayerDataType.Diamond] > 0) ||
        (isRolling.ContainsKey(PlayerDataType.Diamond) && isRolling[PlayerDataType.Diamond]))
        {
            if (pendingRewards.ContainsKey(PlayerDataType.Diamond) && pendingRewards[PlayerDataType.Diamond] > 0 && playerDm.Dollars < targetValues[PlayerDataType.Diamond] + pendingRewards[PlayerDataType.Diamond])
                playerDm.SetData(PlayerDataType.Diamond, pendingRewards[PlayerDataType.Diamond] + playerDm.Dollars, false);
        }
        if ((activeRewardCounts.ContainsKey(PlayerDataType.Coins) && activeRewardCounts[PlayerDataType.Coins] > 0) ||
        (isRolling.ContainsKey(PlayerDataType.Coins) && isRolling[PlayerDataType.Coins]))
        {
            if (pendingRewards.ContainsKey(PlayerDataType.Coins) && pendingRewards[PlayerDataType.Coins] > 0 && playerDm.Coins < targetValues[PlayerDataType.Coins] + pendingRewards[PlayerDataType.Coins])
                playerDm.SetData(PlayerDataType.Coins, pendingRewards[PlayerDataType.Coins] + playerDm.Coins, false);
        }
        if ((activeRewardCounts.ContainsKey(PlayerDataType.Pcard) && activeRewardCounts[PlayerDataType.Pcard] > 0) ||
        (isRolling.ContainsKey(PlayerDataType.Pcard) && isRolling[PlayerDataType.Pcard]))
        {
            if (pendingRewards.ContainsKey(PlayerDataType.Pcard) && pendingRewards[PlayerDataType.Pcard] > 0 && playerDm.Pcard < targetValues[PlayerDataType.Pcard] + pendingRewards[PlayerDataType.Pcard])
                playerDm.SetData(PlayerDataType.Pcard, pendingRewards[PlayerDataType.Pcard] + playerDm.Pcard, false);
        }
        // 清理所有计数器
        activeRewardCounts.Clear();
        pendingRewards.Clear();
        numberRollCoroutines.Clear();
        targetValues.Clear();
        isRolling.Clear();
        StopGemUpFloatTween();
        StopPcardUpFloatTween();
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        GF.Event.Unsubscribe(RewardCollectedEventArgs.EventId, OnRewardCollected);
        GF.Event.Unsubscribe(LargeDollarRewardEventArgs.EventId, OnLargeDollarReward);
        GF.Event.Unsubscribe(TreasureBoxRewardEventArgs.EventId, OnTreasureBoxReward);
        GF.Event.Unsubscribe(UserTypeChangeEventArgs.EventId, OnUserTypeChange);
        PcardGiftRewardSession.Changed -= OnGiftRewardsChanged;
        base.OnClose(isShutdown, userData);
    }
    protected override void InternalSetVisible(bool visible)
    {
        base.InternalSetVisible(visible);
        if (!visible)
        {
            //如果正在动画中，直接累加奖励值
            if ((activeRewardCounts.ContainsKey(PlayerDataType.Diamond) && activeRewardCounts[PlayerDataType.Diamond] > 0) ||
            (isRolling.ContainsKey(PlayerDataType.Diamond) && isRolling[PlayerDataType.Diamond]))
            {
                if (pendingRewards.ContainsKey(PlayerDataType.Diamond) && pendingRewards[PlayerDataType.Diamond] > 0 && playerDm.Dollars < targetValues[PlayerDataType.Diamond] + pendingRewards[PlayerDataType.Diamond])
                    playerDm.SetData(PlayerDataType.Diamond, pendingRewards[PlayerDataType.Diamond] + playerDm.Dollars, false);
            }
            if ((activeRewardCounts.ContainsKey(PlayerDataType.Coins) && activeRewardCounts[PlayerDataType.Coins] > 0) ||
            (isRolling.ContainsKey(PlayerDataType.Coins) && isRolling[PlayerDataType.Coins]))
            {
                if (pendingRewards.ContainsKey(PlayerDataType.Coins) && pendingRewards[PlayerDataType.Coins] > 0 && playerDm.Coins < targetValues[PlayerDataType.Coins] + pendingRewards[PlayerDataType.Coins])
                    playerDm.SetData(PlayerDataType.Coins, pendingRewards[PlayerDataType.Coins] + playerDm.Coins, false);
            }
            if ((activeRewardCounts.ContainsKey(PlayerDataType.Pcard) && activeRewardCounts[PlayerDataType.Pcard] > 0) ||
            (isRolling.ContainsKey(PlayerDataType.Pcard) && isRolling[PlayerDataType.Pcard]))
            {
                if (pendingRewards.ContainsKey(PlayerDataType.Pcard) && pendingRewards[PlayerDataType.Pcard] > 0 && playerDm.Pcard < targetValues[PlayerDataType.Pcard] + pendingRewards[PlayerDataType.Pcard])
                    playerDm.SetData(PlayerDataType.Pcard, pendingRewards[PlayerDataType.Pcard] + playerDm.Pcard, false);
            }
            // 清理所有计数器
            activeRewardCounts.Clear();
            pendingRewards.Clear();
            numberRollCoroutines.Clear();
            targetValues.Clear();
            isRolling.Clear();
        }

    }
    public static void EnsureOpen()
    {
        if (GF.UI.HasUIForm(UIViews.ArrowUITopbar) || GF.UI.IsLoadingUIForm(UIViews.ArrowUITopbar))
        {
            return;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarBoolean>(P_EnableBG, true);
        GF.UI.OpenUIForm(UIViews.ArrowUITopbar, uiParams);
    }

    public static void SetCoinVisible(bool visible)
    {
        s_ShowCoin = visible;
        string assetName = GF.UI.GetUIFormAssetName(UIViews.ArrowUITopbar);
        if (string.IsNullOrEmpty(assetName))
        {
            return;
        }

        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null)
        {
            return;
        }

        foreach (var uiForm in forms)
        {
            if (uiForm.Logic is ArrowUITopbar topbar)
            {
                topbar.ApplyCoinVisible(visible);
            }
        }
    }

    public static bool TryGetPcardTargetPosition(out Vector3 position)
    {
        position = Vector3.zero;
        string assetName = GF.UI.GetUIFormAssetName(UIViews.ArrowUITopbar);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null)
        {
            return false;
        }

        foreach (var uiForm in forms)
        {
            if (uiForm.Logic is ArrowUITopbar topbar)
            {
                RectTransform target = topbar.GetTargetIcon(PlayerDataType.Pcard);
                if (target != null)
                {
                    position = target.position;
                    return true;
                }
            }
        }

        return false;
    }

    void ApplyCoinVisible(bool visible)
    {
        if (varCoin != null)
        {
            varCoin.SetActive(visible);
        }
    }

    void OpenMakeupUIForm()
    {
        GF.UI.OpenUIForm(UIViews.MakeupUIForm);
    }

    private void OpenDebugUIForm()
    {
        
        GF.UI.OpenUIForm(UIViews.DebugUIForm);
    }

    void RefreshPcardText()
    {
        if (playerDm == null)
            playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        RefreshPcardText(playerDm.Pcard);
    }

    void RefreshPcardText(float value)
    {
        if (varTxtPcard == null)
            return;

        varTxtPcard.text = CommonHelper.GetDollarString(value);
    }

    private void OnGiftRewardsChanged()
    {
        RefreshGiftCount();
    }

    private void RefreshGiftCount()
    {
        if (varTxtGiftCount != null)
        {
            varTxtGiftCount.text = $"x{PcardGiftRewardSession.Count}";
        }
    }

    private void TryOpenGiftForm()
    {
        if (PcardGiftRewardSession.Count <= 0)
        {
            return;
        }

        GF.UI.OpenUIForm(UIViews.PcardGiftUIForm);
    }

    private void OnUserTypeChange(object sender, GameEventArgs e)
    {
        var args = e as UserTypeChangeEventArgs;
        if (args == null) return;
        varGem.SetActive(CommonHelper.IsSpec());
        // varCoin.SetActive(!CommonHelper.IsSpec());
    }
    
    private void OnRewardCollected(object sender, GameEventArgs e)
    {
        var args = e as RewardCollectedEventArgs;
        if (args == null) return;
        //Log.Info("this.gameObject.activeInHierarchy: {0}", this.gameObject.activeInHierarchy);
        if (!this.gameObject.activeInHierarchy) return;

        // 根据奖励类型确定货币类型
        PlayerDataType currencyType = args.type;
        float value = args.Value;
        if (currencyType == PlayerDataType.Coins)
        {
            ApplyCoinVisible(true);
        }

        // 获取奖励位置并转换为本地坐标
        Vector3 startPosition = args.WorldPosition;
        //Log.Info($"奖励位置世界坐标: {startPosition},类型: {currencyName},值: {value}");

        // 将世界坐标转换为屏幕坐标，再转换为本地坐标
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, startPosition);
        //Log.Info($"奖励位置屏幕坐标: {screenPoint},值: {value}");

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            flyRewardParent as RectTransform,
            screenPoint,
            null,
            out Vector2 localPoint
        );
        //Log.Info($"转换后本地坐标: {localPoint},类型: {currencyName},值: {value}");
        startPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        // 添加奖励到待处理列表
        AddPendingReward(currencyType, value, startPosition);
    }

    private void OnLargeDollarReward(object sender, GameEventArgs e)
    {
        var args = e as LargeDollarRewardEventArgs;
        if (args == null) return;

        Vector3 startPosition = args.WorldPosition;
        if (startPosition == Vector3.zero)
        {
            startPosition = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        }

        AddPendingReward(PlayerDataType.Diamond, args.Value, startPosition);
    }

    private void OnTreasureBoxReward(object sender, GameEventArgs e)
    {
        var args = e as TreasureBoxRewardEventArgs;
        if (args == null) return;

        Vector3 startPosition = args.WorldPosition;
        if (startPosition == Vector3.zero)
        {
            startPosition = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        }

        AddPendingReward(PlayerDataType.Diamond, args.Value, startPosition);
    }

    private void AddPendingReward(PlayerDataType type, float value, Vector3 startPosition)
    {
        if (!pendingRewards.ContainsKey(type))
        {
            pendingRewards[type] = 0f;
        }
        pendingRewards[type] += value;

        // 确保该类型的计数器存在
        if (!activeRewardCounts.ContainsKey(type))
        {
            activeRewardCounts[type] = 0;
        }

        // 生成多个飞行货币
        int rewardCount = UnityEngine.Random.Range(minRewardCount, maxRewardCount);
        if (type == PlayerDataType.Coins && value == 1f)
        {
            rewardCount = 1;
        }
        activeRewardCounts[type] += rewardCount;  // 更新对应类型的计数器

        // 防止崩溃：在UI关闭或销毁时不再创建飞行货币
        if (flyRewardItemPrefab == null || flyRewardParent == null || !flyRewardParent.gameObject.activeInHierarchy)
        {
            return;
        }

        // Sprite rewardSprite = type == PlayerDataType.Coins ? coinSprite : dollarSprite;

        // 获取正确的目标位置
        Vector2 targetPosition;
        RectTransform targetIcon = GetTargetIcon(type);
        if (targetIcon == null)
        {
            return;
        }

        // 将目标图标的坐标转换为flyRewardParent下的世界坐标
        Vector3[] corners = new Vector3[4];
        targetIcon.GetWorldCorners(corners);
        Vector3 iconWorldPos = (corners[0] + corners[2]) * 0.5f; // 获取图标中心点的世界坐标

        // 将世界坐标转换为flyRewardParent下的局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            flyRewardParent as RectTransform,
            RectTransformUtility.WorldToScreenPoint(null, iconWorldPos),
            null,
            out targetPosition
        );

        Log.Info($"目标图标: {targetIcon.name}, 世界坐标: {iconWorldPos}, 转换后坐标: {targetPosition}");

        // 计算每个货币的出现延迟
        float baseDelay = 0.02f; // 基础延迟时间
        bool useDollarEffect = type == PlayerDataType.Diamond || type == PlayerDataType.Pcard;
        varCpEff.gameObject.SetActive(useDollarEffect);
        if(useDollarEffect)
        {
            varCpEff.transform.localPosition = startPosition;
            varCpEff.Play();
        }

        // 创建飞行货币
        for (int i = 0; i < rewardCount; i++)
        {
            // 计算出现延迟，让货币逐个出现
            float delay = baseDelay * i;

            // 在起始位置周围随机生成位置
            Vector2 randomOffset = rewardCount > 1 ? UnityEngine.Random.insideUnitCircle * 110f : Vector2.zero;
            Vector2 randomStartPos = new Vector2(
                startPosition.x + randomOffset.x,
                startPosition.y + randomOffset.y
            );

            // 使用对象池创建飞行货币
            var flyReward = SpawnItem<UIItemObject>(flyRewardItemPrefab, flyRewardParent).itemLogic as ArrowFlyRewardItem;
            if (flyReward != null)
            {
                // 设置初始位置
                flyReward.rectTransform.anchoredPosition = randomStartPos;
                // Log.Info($"目标位置: {targetPosition}");
                // 初始化飞行货币，传入延迟和索引
                flyReward.InitReward(
                    type,
                    targetPosition,
                    () =>
                    {
                        if (activeRewardCounts.ContainsKey(type))
                        {
                            activeRewardCounts[type]--;
                            if (activeRewardCounts[type] <= 0)
                            {
                                if (type == PlayerDataType.Diamond)
                                {
                                    float delta = pendingRewards.ContainsKey(type) ? pendingRewards[type] : 0f;
                                    if (delta > 0f)
                                        PlayGemUpFloatText(delta);
                                }
                                else if (type == PlayerDataType.Pcard)
                                {
                                    float delta = pendingRewards.ContainsKey(type) ? pendingRewards[type] : 0f;
                                    if (delta > 0f)
                                        PlayPcardUpFloatText(delta);
                                }
                                UpdateNumberRoll(type);
                            }
                        }
                        // 防止崩溃：UI关闭后回调执行时flyReward可能已被销毁
                        if (flyReward != null && flyReward.gameObject != null && flyRewardItemPrefab != null)
                        {
                            UnspawnItem<UIItemObject>(flyRewardItemPrefab, flyReward.gameObject);
                        }
                    },
                    delay,
                    i
                );
            }
        }
    }

    private void UpdateNumberRoll(PlayerDataType type)
    {
        GF.Sound.PlayEffect("coinCollect.ogg");

        // 如果正在滚动，不做任何处理，等待当前协程自然结束后会检查是否有新奖励
        if (isRolling.ContainsKey(type) && isRolling[type])
        {
            // pendingRewards已经在AddPendingReward中累加过了，这里不需要再处理
            return;
        }

        // 确保字典中有该类型的记录
        if (!isRolling.ContainsKey(type))
        {
            isRolling[type] = false;
        }
        if (!targetValues.ContainsKey(type))
        {
            targetValues[type] = GetCurrentValue(type);
        }

        // 检查是否有待处理的奖励
        if (!pendingRewards.ContainsKey(type) || pendingRewards[type] <= 0f)
        {
            return;
        }

        // 标记开始滚动
        isRolling[type] = true;

        if (gameObject.activeInHierarchy && type != PlayerDataType.Gems)
        {
            // 启动新的滚动协程
            if (numberRollCoroutines.ContainsKey(type))
            {
                StopCoroutine(numberRollCoroutines[type]);
                //Log.Info($"停止协程滚动 {type}");
                numberRollCoroutines.Remove(type);
            }
            //Log.Info($"启动协程滚动 {type}");
            numberRollCoroutines[type] = StartCoroutine(NumberRollCoroutine(type));
        }
        else
        {
            // UI未激活时直接更新数值
            TextMeshProUGUI targetText = GetTargetText(type);
            float startValue = targetValues[type];
            float targetValue = startValue + pendingRewards[type];
            if (type != PlayerDataType.Gems)
                targetText.text = FormatCurrencyValue(type, targetValue);
            playerDm.SetData(type, targetValue, true);
            targetValues[type] = targetValue;
            pendingRewards[type] = 0f;
            isRolling[type] = false;
        }
    }

    private IEnumerator NumberRollCoroutine(PlayerDataType type)
    {
        TextMeshProUGUI targetText = GetTargetText(type);
        if (targetText == null)
        {
            isRolling[type] = false;
            yield break;
        }

        // 【关键修复】在协程开始时，保存当前待处理的奖励值并立即清零
        // 这样后续新增的奖励会继续累加到pendingRewards，不会被本次协程清零导致数据丢失
        float localPendingValue = pendingRewards.ContainsKey(type) ? pendingRewards[type] : 0f;
        pendingRewards[type] = 0f;
        
        if (localPendingValue <= 0f)
        {
            isRolling[type] = false;
            yield break;
        }

        float startValue = targetValues[type];
        float targetValue = startValue + localPendingValue;
        float elapsedTime = 0f;

        //Log.Info($"[数字滚动] 类型:{type}, 起始值:{startValue}, 本次奖励:{localPendingValue}, 目标值:{targetValue}");

        while (elapsedTime < numberRollDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / numberRollDuration;
            float currentValue = Mathf.Lerp(startValue, targetValue, progress);

            if (type == PlayerDataType.Coins)
            {
                // Log.Debug($"金币数值滚动 {startValue}，{currentValue}/{targetValue}，{progress}，{elapsedTime}");
                targetText.text = targetValue.ToString();
            }
            else
            {
                targetText.text = Math.Round(currentValue, 3).ToString();
                // Log.Debug($"钻石数值滚动 {startValue}，{currentValue}/{targetValue}，{progress}，{elapsedTime}");
            }

            yield return null;
        }

        // 确保显示最终值
        targetText.text = FormatCurrencyValue(type, targetValue);
        targetValues[type] = targetValue;
        numberRollCoroutines.Remove(type);
        
        // 【关键修复】先更新玩家数据，再标记滚动结束
        playerDm.SetData(type, targetValue, true);
        isRolling[type] = false;  // 标记滚动结束
        
        //Log.Info($"[数字滚动完成] 类型:{type}, 最终值:{targetValue}, 剩余待处理:{pendingRewards[type]}");
        
        // 【关键修复】检查是否有新的奖励等待处理，如果有则重新启动滚动
        if (pendingRewards.ContainsKey(type) && pendingRewards[type] > 0f)
        {
            //Log.Info($"[数字滚动] 检测到新奖励，重新启动滚动, 类型:{type}, 新奖励值:{pendingRewards[type]}");
            UpdateNumberRoll(type);
        }
        else if (type == PlayerDataType.Coins)
        {
            ApplyCoinVisible(s_ShowCoin);
        }
        
        if (type == PlayerDataType.Diamond)
        {
            // CheckGuide();
        }
    }

    private void OnPlayerDataChanged(object sender, GameEventArgs e)
    {
        var args = e as PlayerDataChangedEventArgs;
        if (args == null) return;

        // 只有在没有进行中的动画时才直接更新数值
        if (!isRolling.ContainsKey(args.DataType) || !isRolling[args.DataType])
        {
            switch (args.DataType)
            {
                case PlayerDataType.Coins:
                    varTxtCoin.text = CommonHelper.FormatLargeNumber(args.Value.ToString(), 0);
                    targetValues[PlayerDataType.Coins] = args.Value;
                    break;
                case PlayerDataType.Diamond:
                    varTxtGem.text = CommonHelper.GetDollarString(args.Value);
                    targetValues[PlayerDataType.Diamond] = args.Value;
                    break;
                case PlayerDataType.Pcard:
                    RefreshPcardText(args.Value);
                    targetValues[PlayerDataType.Pcard] = args.Value;
                    break;
            }
        }
        varLv.text = playerDm.LevelId.ToString();
    }

    private void RefreshLifeDisplay()
    {
        if (varBtnLife != null) varBtnLife.interactable = playerDm.LifeCount < PlayerDataModel.MaxLifeCount;
        if (varTxtLife != null) varTxtLife.text = playerDm.LifeCount.ToString();
        int seconds = playerDm.GetSecondsUntilNextLife();
        if (varTxtTime != null)
            varTxtTime.text = seconds < 0 ? "FULL" : $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void OpenAddLife()
    {
        playerDm.RefreshLife();
        if (playerDm.LifeCount >= PlayerDataModel.MaxLifeCount) return;
        if (!GF.UI.HasUIForm(UIViews.AddLifeUIForm) && !GF.UI.IsLoadingUIForm(UIViews.AddLifeUIForm))
            GF.UI.OpenUIForm(UIViews.AddLifeUIForm, UIParams.Create());
    }

    private RectTransform GetTargetIcon(PlayerDataType type)
    {
        switch (type)
        {
            case PlayerDataType.Coins:
                return varIcon_Coin;
            case PlayerDataType.Diamond:
                return varIcon_Gem;
            case PlayerDataType.Pcard:
                return varPcard != null ? varPcard.transform.Find("Icon_Pcard") as RectTransform : null;
            default:
                return null;
        }
    }

    private TextMeshProUGUI GetTargetText(PlayerDataType type)
    {
        switch (type)
        {
            case PlayerDataType.Coins:
                return varTxtCoin;
            case PlayerDataType.Diamond:
                return varTxtGem;
            case PlayerDataType.Pcard:
                return varTxtPcard;
            default:
                return null;
        }
    }

    private float GetCurrentValue(PlayerDataType type)
    {
        switch (type)
        {
            case PlayerDataType.Coins:
                return playerDm.Coins;
            case PlayerDataType.Diamond:
                return playerDm.Dollars;
            case PlayerDataType.Pcard:
                return playerDm.Pcard;
            default:
                return 0f;
        }
    }

    private string FormatCurrencyValue(PlayerDataType type, float value)
    {
        return type == PlayerDataType.Diamond || type == PlayerDataType.Pcard
            ? CommonHelper.GetDollarString(value)
            : CommonHelper.FormatLargeNumber(value.ToString(), 0);
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
      if (btSelf == varBtnCoin)
        {
            
        }
        else if (btSelf == varGembutton)
        {
            // B 面美元仅作游戏内货币展示，不再打开提现界面。
        }
        else if (btSelf != null && btSelf.name == "Pcardbutton")
        {
            OpenMakeupUIForm();
        }
        else if (btSelf != null && btSelf.name == "GiftButton")
        {
            TryOpenGiftForm();
        }
    }
    protected override void OnButtonClick(object sender, string btSelf)
    {
        base.OnButtonClick(sender, btSelf);
    }
    

    private void PlayGemUpFloatText(float deltaValue)
    {
        if (varTxtUpGem == null) return;

        StopGemUpFloatTween();

        varTxtUpGem.text = $"+{CommonHelper.GetDollarString(deltaValue, deltaValue >= 0.01f)}";
        RectTransform rect = varTxtUpGem.rectTransform;
        rect.anchoredPosition = m_TxtUpGemRestPos;

        Color textColor = varTxtUpGem.color;
        textColor.a = 1f;
        varTxtUpGem.color = textColor;

        varTxtUpGem.gameObject.SetActive(true);

        m_TxtUpGemTween = DOTween.Sequence()
            .SetUpdate(true)
            .Join(rect.DOAnchorPosY(m_TxtUpGemRestPos.y + GEM_UP_FLOAT_OFFSET_Y, GEM_UP_FLOAT_DURATION).SetEase(Ease.OutQuad))
            .Join(varTxtUpGem.DOFade(0f, GEM_UP_FLOAT_DURATION).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                varTxtUpGem.gameObject.SetActive(false);
                m_TxtUpGemTween = null;
            });
    }

    private void StopGemUpFloatTween()
    {
        if (m_TxtUpGemTween != null)
        {
            m_TxtUpGemTween.Kill();
            m_TxtUpGemTween = null;
        }

        if (varTxtUpGem != null)
        {
            DOTween.Kill(varTxtUpGem.rectTransform);
            DOTween.Kill(varTxtUpGem);
        }
    }

    private void PlayPcardUpFloatText(float deltaValue)
    {
        if (varTxtUpPcard == null) return;

        StopPcardUpFloatTween();

        varTxtUpPcard.text = $"+{CommonHelper.GetDollarString(deltaValue, deltaValue >= 0.01f)}";
        RectTransform rect = varTxtUpPcard.rectTransform;
        rect.anchoredPosition = m_TxtUpPcardRestPos;

        Color textColor = varTxtUpPcard.color;
        textColor.a = 1f;
        varTxtUpPcard.color = textColor;
        varTxtUpPcard.gameObject.SetActive(true);

        m_TxtUpPcardTween = DOTween.Sequence()
            .SetUpdate(true)
            .Join(rect.DOAnchorPosY(m_TxtUpPcardRestPos.y + GEM_UP_FLOAT_OFFSET_Y, GEM_UP_FLOAT_DURATION).SetEase(Ease.OutQuad))
            .Join(varTxtUpPcard.DOFade(0f, GEM_UP_FLOAT_DURATION).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                varTxtUpPcard.gameObject.SetActive(false);
                m_TxtUpPcardTween = null;
            });
    }

    private void StopPcardUpFloatTween()
    {
        if (m_TxtUpPcardTween != null)
        {
            m_TxtUpPcardTween.Kill();
            m_TxtUpPcardTween = null;
        }

        if (varTxtUpPcard != null)
        {
            DOTween.Kill(varTxtUpPcard.rectTransform);
            DOTween.Kill(varTxtUpPcard);
        }
    }
}
