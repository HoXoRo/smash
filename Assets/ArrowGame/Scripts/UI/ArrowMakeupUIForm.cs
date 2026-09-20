using GameFramework;
using GameFramework.Resource;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityGameFramework.Runtime;
using System;
using DG.Tweening;

public enum MakeupType
{
    Mon,  // 美元提现
    Coin,   // 金币提现
    Order   // 提现记录
}
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowMakeupUIForm")]
public partial class ArrowMakeupUIForm : UIFormBase
{
    private MakeupType m_CurrentType = MakeupType.Mon;
    private PlayerDataModel m_PlayerData;
    private Dictionary<MakeupType, List<ArrowMakeupBanknotes>> m_MakeupConfigs = new Dictionary<MakeupType, List<ArrowMakeupBanknotes>>();
    // private Dictionary<MakeupType, List<MahjongMakeupCoin>> m_CoinConfigs = new Dictionary<MakeupType, List<MahjongMakeupCoin>>();
    private ArrowMakeupListBank m_ListBank;
    private InitOnStart m_InitOnStart;
    private ArrowMakeupOrdemListBank m_OrdemListBank;
    private InitOnStart m_OrdemInitOnStart;
    private ArrowPaymentListBank m_BottomCurrencyListBank;
    private InitOnStart m_BottomCurrencyInitOnStart;
    
    // 提现信息滚动相关
    private const float ITEM_HEIGHT = 125f;
    private const float ITEM_SPACING = 31f;
    private const float MASK_HEIGHT = 155f;
    private const float SCROLL_DURATION = 1f;
    private Sequence m_ScrollSequence;
    private const int MakeupGuideBalanceId = 5;
    private const int MakeupGuideItemBgId = 6;
    private const int MakeupGuideCloseBtnId = 7;
    private const int MakeupEnterGuideId = 3;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();

        // 初始化列表组件
        m_InitOnStart = varLoopVerticalScrollRect.GetComponent<InitOnStart>();
        m_ListBank = varLoopVerticalScrollRect.GetComponent<ArrowMakeupListBank>();
        if (m_ListBank == null)
        {
            m_ListBank = varLoopVerticalScrollRect.gameObject.AddComponent<ArrowMakeupListBank>();
        }

        // 设置LoopVerticalScrollRect使用MakeupListBank作为数据源
        var loopScrollRect = varLoopVerticalScrollRect.GetComponent<LoopVerticalScrollRect>();
        if (loopScrollRect != null)
        {
            // 创建自定义的数据源，使用MakeupListBank
            var customDataSource = new CustomMakeupDataSource(m_ListBank);
            loopScrollRect.dataSource = customDataSource;
            loopScrollRect.prefabSource = m_InitOnStart;
        }

        // 初始化提现记录列表组件
        m_OrdemInitOnStart = varLoopVerticalScrollRect_Ordem.GetComponent<InitOnStart>();
        m_OrdemListBank = varLoopVerticalScrollRect_Ordem.GetComponent<ArrowMakeupOrdemListBank>();
        var loopScrollRect_Ordem = varLoopVerticalScrollRect_Ordem.GetComponent<LoopVerticalScrollRect>();
        if (loopScrollRect_Ordem != null)
        {
            // 创建自定义的数据源，使用MakeupOrdemListBank
            var customDataSource = new CustomMakeupOrdemDataSource(m_OrdemListBank);
            loopScrollRect_Ordem.dataSource = customDataSource;
            loopScrollRect_Ordem.prefabSource = m_OrdemInitOnStart;
        }

        InitBottomCurrencyList();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // 监听支付信息变更事件
        GF.Event.Subscribe(PaymentInfoChangedEventArgs.EventId, OnPaymentInfoChangedEvent);
        GF.Event.Subscribe(MakeupDataChangedEventArgs.EventId, OnMakeupDataChangedEvent);
        if (string.IsNullOrEmpty(m_PlayerData.PlayerName))
        {
            m_PlayerData.PlayerName = GetRandomPlayerName();
            m_PlayerData.Save();
        }
        varPlayerName.text = m_PlayerData.PlayerName;
        varRatiotxt.text = $"{CommonHelper.GetMakeupRatio() * 100}%";
        
        // 加载配置数据
        LoadMakeupConfigs();
        OnMakeupTypeChanged(MakeupType.Mon);
        // 获取默认支付方式        
        GetPaymentDefault();
        RefreshBottomCurrencyList();
        // 初始化提现信息滚动组件
        InitWithdrawalInfoScroll();

        TimerManager.Instance.AddTimer("WithdrawalPlayerInfoChange", 15, OnWithdrawalPlayerInfoChange);
        Log.Info("OnOpen - UI初始化完成");
        MarkEnterMakeupGuideCompleted();
        CheckMakeupGuide();
        // MahjongMaxAdsManager.Instance.ShowBannerAd();
        
        // CommonHelper.LogEvent(AdjustEventCodeEvent.withdraw_enter);
        CommonHelper.LogEvent(AdjustEventCodeEvent.popu_enter, new Dictionary<string, string>() {{"popu", "makeupUI"}});

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理数据
        m_MakeupConfigs.Clear();
        // m_CoinConfigs.Clear();
        GF.Event.Unsubscribe(PaymentInfoChangedEventArgs.EventId, OnPaymentInfoChangedEvent);
        GF.Event.Unsubscribe(MakeupDataChangedEventArgs.EventId, OnMakeupDataChangedEvent);
        
        // 清理定时器和动画
        TimerManager.Instance.RemoveTimer("WithdrawalPlayerInfoChange");
        if (m_ScrollSequence != null)
        {
            m_ScrollSequence.Kill();
            m_ScrollSequence = null;
        }
        
        base.OnClose(isShutdown, userData);
        // MahjongMaxAdsManager.Instance.HideBannerAd();
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Button_Money":
                OnMakeupTypeChanged(MakeupType.Mon);
                break;
            case "Button_Ordem":
                GF.UI.OpenUIForm(UIViews.ArrowMakeupOrdemUIForm);
                break;
            case "Button_Close":
                OnCloseButtonClick();
                break;
            case "Button_Revise":
                OnReviseButtonClick();
                break;
            case "Button_Tips":
                GF.UI.OpenUIForm(UIViews.ArrowMakeupTipsUIForm);
                break;
        }
    }

    private void LoadMakeupConfigs()
    {
        // 加载美元提现配置
        var banknotesTable = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>();
        var banknotesRows = banknotesTable.GetAllDataRows();
        m_MakeupConfigs[MakeupType.Mon] = new List<ArrowMakeupBanknotes>(banknotesRows);

        // 加载金币提现配置
        // var coinTable = GF.DataTable.GetDataTable<MahjongMakeupCoin>();
        // var coinRows = coinTable.GetAllDataRows();
        // m_CoinConfigs[MakeupType.Coin] = new List<MahjongMakeupCoin>(coinRows);
    }

    private void OnMakeupTypeChanged(MakeupType type)
    {
        // if (m_CurrentType == type) return;
        m_CurrentType = type;
        // 更新界面显示
        UpdateUI();

        // 更新列表数据
        UpdateListData();
    }


    private void UpdateUI()
    {
        // 更新货币显示
        switch (m_CurrentType)
        {
            case MakeupType.Mon:
                varCurrencytxt.text = CommonHelper.GetDollarString(m_PlayerData.Dollars * CommonHelper.GetMakeupRatio(), true, true);
                varActualCurrencytxt.text = CommonHelper.GetDollarString(m_PlayerData.Dollars, true, false);
                // varCurrencyIcon.SetSprite("Common/moeny_com.png", varCurrencyIcon);
                break;
        }
        // 更新银行图标和信息
        UpdateBankIcon();
        UpdateBankInfoText();

        // 显示/隐藏相关UI元素
        // varCurrencyIcon.gameObject.SetActive(m_CurrentType != MakeupType.Order);
        varLoopVerticalScrollRect.gameObject.SetActive(m_CurrentType != MakeupType.Order);
        varLoopVerticalScrollRect_Ordem.gameObject.SetActive(m_CurrentType == MakeupType.Order);
        // TODO: 显示提现记录列表
    }
    /// <summary>
    /// 初始化提现信息滚动组件
    /// </summary>
    private void InitWithdrawalInfoScroll()
    {
        if(m_LocalizationMoney == null)
        GetPaymentDefault();
        // 设置初始位置
        RectTransform rect1 = varWithdrawalInfoItem.GetComponent<RectTransform>();
        RectTransform rect2 = varWithdrawalInfoItem_1.GetComponent<RectTransform>();
        
        rect1.anchoredPosition = new Vector2(rect1.anchoredPosition.x, 0);
        rect2.anchoredPosition = new Vector2(rect2.anchoredPosition.x, -(ITEM_HEIGHT + ITEM_SPACING));
        
        // 初始化第一个组件的数据
        UpdateWithdrawalItemData(varWithdrawalInfoItem);
        UpdateWithdrawalItemData(varWithdrawalInfoItem_1);
    }
    
    /// <summary>
    /// 定时刷新提现信息滚动
    /// </summary>
    public void OnWithdrawalPlayerInfoChange()
    {
        // 停止之前的动画
        if (m_ScrollSequence != null)
        {
            m_ScrollSequence.Kill();
        }
        
        RectTransform rect1 = varWithdrawalInfoItem.GetComponent<RectTransform>();
        RectTransform rect2 = varWithdrawalInfoItem_1.GetComponent<RectTransform>();
        
        // 创建滚动动画序列
        m_ScrollSequence = DOTween.Sequence();
        
        // 向上滚动 (ITEM_HEIGHT + ITEM_SPACING) 像素
        float scrollDistance = ITEM_HEIGHT + ITEM_SPACING;
        m_ScrollSequence.Append(rect1.DOAnchorPosY(rect1.anchoredPosition.y + scrollDistance, SCROLL_DURATION).SetEase(Ease.Linear));
        m_ScrollSequence.Join(rect2.DOAnchorPosY(rect2.anchoredPosition.y + scrollDistance, SCROLL_DURATION).SetEase(Ease.Linear));
        
        // 动画完成后检查是否需要重置位置
        m_ScrollSequence.OnComplete(() =>
        {
            // 检查第一个组件是否滚出遮罩顶部
            if (rect1.anchoredPosition.y > MASK_HEIGHT)
            {
                // 重置到底部并更新数据
                rect1.anchoredPosition = new Vector2(rect1.anchoredPosition.x, rect2.anchoredPosition.y - scrollDistance);
                UpdateWithdrawalItemData(varWithdrawalInfoItem);
            }
            // 检查第二个组件是否滚出遮罩顶部
            else if (rect2.anchoredPosition.y > MASK_HEIGHT)
            {
                // 重置到底部并更新数据
                rect2.anchoredPosition = new Vector2(rect2.anchoredPosition.x, rect1.anchoredPosition.y - scrollDistance);
                UpdateWithdrawalItemData(varWithdrawalInfoItem_1);
            }
        });
    }
    
    /// <summary>
    /// 更新提现信息组件数据
    /// </summary>
    private void UpdateWithdrawalItemData(ArrowWithdrawalInfoItem item)
    {
        string payment = GetRandomPlayerPayment();
        string name = $"<color=#FF0000>{GetRandomPlayerName()}</color>";
        string level = $"<color=#FF0000>Level {GetRandomPlayerLevel()}</color>";
        string money = $"<color=#FF0000>{GetRandomPlayerMoney()}</color>";
        
        item.UpdateUI(payment, name, level, money);
    }
    private string GetRandomPlayerPayment()
    {
            string payment = m_LocalizationMoney.Currency[UnityEngine.Random.Range(0, m_LocalizationMoney.Currency.Length)];
            return payment;
    }
    private string GetRandomPlayerName()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string name;

        if (string.IsNullOrEmpty(m_PlayerData.PlayerName))
        {
            // 直接随机生成5个字符
            char[] result = new char[5];
            for (int i = 0; i < 5; i++)
            {
                result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            name = new string(result);
        }
        else
        {
            // 生成随机代号，确保不与现有的PlayerName完全相同
            do
            {
                char[] result = new char[5];
                for (int i = 0; i < 5; i++)
                {
                    result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
                }
                name = new string(result);
            } while (name == m_PlayerData.PlayerName);
        }

        return name;
    }
    private int GetRandomPlayerLevel()
    {
        int level = 15;
        return level;
    }

    private string GetRandomPlayerMoney()
    {
        string money = CommonHelper.GetDollarString(UnityEngine.Random.Range(1200f, 1800f), true, true);
        return money;
    }

    private void UpdateListData()
    {
        switch (m_CurrentType)
        {
            case MakeupType.Mon:
                UpdateMoneyList();
                break;
            case MakeupType.Coin:
                UpdateCoinList();
                break;
            case MakeupType.Order:
                UpdateOrderList();
                break;
        }
    }

    private void UpdateMoneyList()
    {
        m_ListBank.SetMakeupType(MakeupType.Mon);
        var loopScrollRect = varLoopVerticalScrollRect.GetComponent<LoopVerticalScrollRect>();
        if (loopScrollRect != null)
        {
            loopScrollRect.totalCount = m_ListBank.GetListLength();
            Log.Info($"UpdateMoneyList: {loopScrollRect.totalCount}");
            loopScrollRect.RefillCells();
        }
    }

    private void UpdateCoinList()
    {
        m_ListBank.SetMakeupType(MakeupType.Coin);
        var loopScrollRect = varLoopVerticalScrollRect.GetComponent<LoopVerticalScrollRect>();
        if (loopScrollRect != null)
        {
            loopScrollRect.totalCount = m_ListBank.GetListLength();
            loopScrollRect.RefillCells();
        }
    }

    private void UpdateOrderList()
    {
        m_OrdemListBank.InitPaymentData();
        var loopScrollRect = varLoopVerticalScrollRect_Ordem.GetComponent<LoopVerticalScrollRect>();
        if (loopScrollRect != null)
        {
            loopScrollRect.totalCount = m_OrdemListBank.GetListLength();
            loopScrollRect.RefillCells();
        }
    }

    // 刷新列表数据
    public void RefreshList()
    {
        m_ListBank.Refresh();
        var loopScrollRect = varLoopVerticalScrollRect.GetComponent<LoopVerticalScrollRect>();
        if (loopScrollRect != null)
        {
            loopScrollRect.totalCount = m_ListBank.GetListLength();
            loopScrollRect.RefillCells();
        }
    }

    private void OnCloseButtonClick()
    {
        GF.UI.Close(UIForm);
        CommonHelper.ShowInterstitial(RewardSourceConst.closeBalance,true);
    }

    private void OnReviseButtonClick()
    {
        GF.UI.OpenUIForm(UIViews.ArrowSelectPaymentUIForm);
    }

    // 支付信息变更回调
    private void OnPaymentInfoChanged()
    {
        Log.Info("MakeupUIForm收到支付信息变更通知");
        
        // 更新银行图标显示
        UpdateBankIcon();
        
        // 更新银行信息文本
        UpdateBankInfoText();
        
        // 刷新列表数据，确保MakeupItem的银行图标也更新
        RefreshList();
        
    }

    private void UpdateBankIcon()
    {
        /*
        if (varBankIcon == null) return;
        m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.payment))
        {
            Log.Info($"大厅加载提现方式图标，Assets/MahjongGame/Sprites/UI/BankIcon/{m_PlayerData.MakeupPaymentInfo.payment}.png");
            varBankIcon_1.gameObject.SetActive(true);
            varBankIcon.SetSprite($"UI/BankIcon/{m_PlayerData.MakeupPaymentInfo.payment}.png");
        }
        else
        {
            varBankIcon.SetSprite($"UI/BankIcon/{GetPaymentDefault()}.png");
            varBankIcon_1.gameObject.SetActive(true);
        }
        */
    }
    private string language = "";
    private ArrowLocalizationMoney m_LocalizationMoney = null;
    private string GetPaymentDefault()
    {
        if (string.IsNullOrEmpty(language))
        {
            if (m_PlayerData != null && !string.IsNullOrEmpty(m_PlayerData.Language))
            {
                language = m_PlayerData.Language;
                var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
                var langRow = langTb.GetDataRow(row => row.LanguageKey == language);
                if (langRow == null)
                {
                    langRow = langTb.MinIdDataRow;
                    language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey).ToString();//不支持的语言默认用英文
                }
                m_PlayerData.Language = language;
                m_PlayerData.Save();
            }
            else
            {
                language = GFBuiltin.Localization.SystemLanguage.ToString();
                var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
                var langRow = langTb.GetDataRow(row => row.LanguageKey == language);
                if (langRow == null)
                {
                    langRow = langTb.MinIdDataRow;
                    language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey).ToString();//不支持的语言默认用英文
                }
                m_PlayerData.Language = language;
                m_PlayerData.Save();
            }

            if (m_LocalizationMoney == null)
            {
                var moneyTb = GF.DataTable.GetDataTable<ArrowLocalizationMoney>();
                m_LocalizationMoney = moneyTb.GetDataRow(row => row.LanguageName == language);
                if (m_LocalizationMoney == null)
                {
                    m_LocalizationMoney = moneyTb.MinIdDataRow;
                }
            }
        }
#if UNITY_EDITOR
        language = GF.Localization.Language.ToString(); //编辑器下使用设置语言方便测试;

        var moneyTb2 = GF.DataTable.GetDataTable<ArrowLocalizationMoney>();
        m_LocalizationMoney = moneyTb2.GetDataRow(row => row.LanguageName == language);
        if (m_LocalizationMoney == null)
        {
            m_LocalizationMoney = moneyTb2.MinIdDataRow;
        }
#endif
        return m_LocalizationMoney.Currency[0];
    }


    private void UpdateBankInfoText()
    {
        if (varCurBankInfo != null)
        {
            varCurBankInfo.text = string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.accountId) ? GF.Localization.GetString("ArrowMakeupUIForm.curBankInfo") : m_PlayerData.MakeupPaymentInfo.accountId;
        }
    }

    /// <summary>
    /// 支付信息变更事件处理
    /// </summary>
    private void OnPaymentInfoChangedEvent(object sender, GameEventArgs e)
    {
        if (e is PaymentInfoChangedEventArgs args)
        {
            m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
            Log.Info($"MakeupUIForm收到支付信息变更事件: {args.ChangeType}");

            OnPaymentInfoChanged();
        }
    }

    /// <summary>
    /// 提现数据变更事件处理
    /// </summary>
    private void OnMakeupDataChangedEvent(object sender, GameEventArgs e)
    {
        if (e is MakeupDataChangedEventArgs args)
        {
            m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
            Log.Info($"MakeupUIForm收到提现数据变更事件: {args.ChangeType}, Type={args.MakeupType}, ID={args.MakeupId}");

            // 刷新列表数据
            RefreshList();
        }
    }

    private void CheckMakeupGuide()
    {
        if (GF.GuideManager.IsGuideCompleted(MakeupGuideCloseBtnId))
            return;

        StartCoroutine(ShowMakeupGuideSequenceCoroutine());
    }

    /// <summary>
    /// 玩家已自行打开提现页时，跳过“引导点击 Topbar 进入提现页”（guide 3）并标记完成。
    /// </summary>
    private void MarkEnterMakeupGuideCompleted()
    {
        if (ArrowGuideUIForm.IsGuideCompleted(MakeupEnterGuideId))
            return;

        Log.Info("提现页已在引导3之前打开，跳过该步并标记已完成");
        ArrowGuideUIForm.AddCompletedGuide(MakeupEnterGuideId);
    }

    private IEnumerator ShowMakeupGuideSequenceCoroutine()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (GF.GuideManager.IsGuideCompleted(MakeupGuideCloseBtnId))
            yield break;

        var steps = new List<GuideStep>();

        // 去掉引导balance， makeupitem
        /*if (!GF.GuideManager.IsGuideCompleted(MakeupGuideBalanceId) && varImageBlance != null)
            steps.Add(new GuideStep(MakeupGuideBalanceId, varImageBlance, true));

        ArrowMakeupItem firstItem = null;
        for (int i = 0; i < 5 && firstItem == null; i++)
        {
            firstItem = GetFirstVisibleMakeupItem();
            if (firstItem == null)
                yield return null;
        }

        if (firstItem != null && !GF.GuideManager.IsGuideCompleted(MakeupGuideItemBgId))
            steps.Add(new GuideStep(MakeupGuideItemBgId, firstItem.GuideBgTransform, true));*/

        if (!GF.GuideManager.IsGuideCompleted(MakeupGuideCloseBtnId) && varButton_Close != null)
            steps.Add(new GuideStep(MakeupGuideCloseBtnId, varButton_Close.transform, true));

        if (steps.Count > 0)
            GF.GuideManager.ShowGuideSequence(steps.ToArray(), null);
    }

    private ArrowMakeupItem GetFirstVisibleMakeupItem()
    {
        if (varContent == null)
            return null;

        foreach (Transform child in varContent.transform)
        {
            var item = child.GetComponent<ArrowMakeupItem>();
            if (item != null && item.gameObject.activeInHierarchy)
                return item;
        }

        return null;
    }

    private void InitBottomCurrencyList()
    {
        if (varBottomCurrencyList == null)
            return;

        m_BottomCurrencyInitOnStart = varBottomCurrencyList.GetComponent<InitOnStart>();
        m_BottomCurrencyListBank = varBottomCurrencyList.GetComponent<ArrowPaymentListBank>();
        if (m_BottomCurrencyListBank == null)
            m_BottomCurrencyListBank = varBottomCurrencyList.gameObject.AddComponent<ArrowPaymentListBank>();

        m_BottomCurrencyListBank.SetHorizontalLayout(true);
        m_BottomCurrencyListBank.ReloadPaymentData();

        if (m_BottomCurrencyInitOnStart != null && varPaymentItem != null)
            m_BottomCurrencyInitOnStart.item = varPaymentItem;

        varBottomCurrencyList.dataSource = new CustomDisplayPaymentDataSource(m_BottomCurrencyListBank);
        varBottomCurrencyList.prefabSource = m_BottomCurrencyInitOnStart;
        varBottomCurrencyList.totalCount = m_BottomCurrencyListBank.GetListLength();
        varBottomCurrencyList.RefillCells();
    }

    private void RefreshBottomCurrencyList()
    {
        if (varBottomCurrencyList == null || m_BottomCurrencyListBank == null)
            return;

        m_BottomCurrencyListBank.ReloadPaymentData();
        varBottomCurrencyList.totalCount = m_BottomCurrencyListBank.GetListLength();
        varBottomCurrencyList.RefillCells();
    }
}

// 自定义数据源类，用于连接MakeupListBank和LoopVerticalScrollRect
public class CustomMakeupDataSource : LoopScrollDataSource
{
    private ArrowMakeupListBank m_ListBank;

    public CustomMakeupDataSource(ArrowMakeupListBank listBank)
    {
        m_ListBank = listBank;
    }

    public void ProvideData(Transform transform, int idx)
    {
        if (m_ListBank != null)
        {
            var bankData = m_ListBank.GetLoopListBankData(idx);
            Log.Info($"ProvideData: idx={idx}, bankData={bankData}, listLength={m_ListBank.GetListLength()}");
            if (bankData != null)
            {
                // 调用MakeupItem的SetData方法
                var MakeupItem = transform.GetComponent<ArrowMakeupItem>();
                if (MakeupItem != null)
                {
                    Log.Info($"设置MakeupItem数据: idx={idx}, content={bankData.Content}");
                    MakeupItem.SetData(bankData.Content, m_ListBank);
                }
                else
                {
                    Log.Warning($"未找到MakeupItem组件: idx={idx}");
                }
            }
            else
            {
                Log.Warning($"bankData为空: idx={idx}");
            }
        }
        else
        {
            Log.Error("m_ListBank为空");
        }

        // 同时调用ScrollCellIndex方法保持兼容性
        transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
    }
}


// 自定义数据源类，用于连接MakeupOrdemListBank和LoopVerticalScrollRect_Ordem
public class CustomMakeupOrdemDataSource : LoopScrollDataSource
{
    private ArrowMakeupOrdemListBank m_ListBank;

    public CustomMakeupOrdemDataSource(ArrowMakeupOrdemListBank listBank)
    {
        m_ListBank = listBank;
    }

    public void ProvideData(Transform transform, int idx)
    {
        if (m_ListBank != null)
        {
            var bankData = m_ListBank.GetLoopListBankData(idx);
            Log.Info($"ProvideData: idx={idx}, bankData={bankData}, listLength={m_ListBank.GetListLength()}");
            if (bankData != null)
            {
                // 调用MakeupItem的SetData方法
                var MakeupItem = transform.GetComponent<ArrowMakeupOrdemItem>();
                if (MakeupItem != null)
                {
                    Log.Info($"设置提现记录数据: idx={idx}, content={bankData.Content}");
                    MakeupItem.SetData(bankData.Content, m_ListBank);
                }
                else
                {
                    Log.Warning($"未找到MakeupOrdemItem组件: idx={idx}");
                }
            }
            else
            {
                Log.Warning($"bankData为空: idx={idx}");
            }
        }
        else
        {
            Log.Error("m_ListBank为空");
        }

        // 同时调用ScrollCellIndex方法保持兼容性
        transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
    }
}

// 底部货币展示列表数据源（只展示，不可点击）
public class CustomDisplayPaymentDataSource : LoopScrollDataSource
{
    private const float ItemScale = 0.7f;
    private ArrowPaymentListBank m_ListBank;

    public CustomDisplayPaymentDataSource(ArrowPaymentListBank listBank)
    {
        m_ListBank = listBank;
    }

    public void ProvideData(Transform transform, int idx)
    {
        if (m_ListBank == null)
            return;

        var bankData = m_ListBank.GetLoopListBankData(idx);
        if (bankData == null || bankData.IsEmpty())
            return;

        transform.localScale = Vector3.one * ItemScale;

        var layoutElement = transform.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = 230f * ItemScale;
            layoutElement.preferredHeight = 90f * ItemScale;
        }

        var paymentItem = transform.GetComponent<ArrowPaymentItem>();
        if (paymentItem != null)
            paymentItem.SetData(bankData.Content, m_ListBank, false);

        transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
    }
}
