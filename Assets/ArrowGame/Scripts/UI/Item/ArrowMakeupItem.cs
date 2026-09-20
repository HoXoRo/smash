using System;
using GameFramework;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/MakeupItem")]
public partial class ArrowMakeupItem : UIItemBase
{
    private MakeupItemData m_ItemData;
    private PlayerDataModel m_PlayerData;
    private ArrowMakeupListBank m_ListBank;

    protected override void OnInit()
    {
        base.OnInit();
        m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();

        // 监听支付信息变更
        // ArrowMakeupListBank.OnPaymentInfoChanged += OnPaymentInfoChanged;

        // 监听支付信息变更事件
        // GF.Event.Subscribe(PaymentInfoChangedEventArgs.EventId, OnPaymentInfoChangedEvent);
        GF.Event.Subscribe(MakeupDataChangedEventArgs.EventId, OnMakeupDataChangedEvent);

        // 初始化按钮事件
        if (varMakeupBtn != null)
        {
            varMakeupBtn.onClick.RemoveListener(OnMakeupButtonClick);
            varMakeupBtn.onClick.AddListener(OnMakeupButtonClick);
        }
        if (varPlayBtn != null)
        {
            varPlayBtn.onClick.RemoveListener(OnPlayButtonClick);
            varPlayBtn.onClick.AddListener(OnPlayButtonClick);
        }
    }

    private void OnMakeupDataChangedEvent(object sender, GameEventArgs e)
    {
        if (this == null) return;
        UpdateUI();
    }

    public void SetData(object data, ArrowMakeupListBank listBank)
    {
        Log.Info($"SetData: {data}");
        if (data is MakeupItemData itemData)
        {
            m_ListBank = listBank;
            m_ItemData = itemData;
            UpdateUI();
        }
    }

    // 添加ScrollCellIndex方法，这是LoopVerticalScrollRect调用的标准方法
    public void ScrollCellIndex(int idx)
    {
        gameObject.name = $"MakeupItem_{idx}";

        // 数据现在通过SetData方法直接传入，这里只需要设置对象名称
        // 如果需要通过索引获取数据，可以在这里实现
        // 但建议使用SetData方法，因为它更直接和高效
    }

    private void UpdateUI()
    {
        if (m_ItemData == null) return;

        // 已开始提现状态
        UpdateTaskState();
        
        // 更新银行图标
        // UpdateBankIcon();
    }

    private void UpdateTaskState()
    {
        // 显示任务界面，隐藏提现界面
        varTaskObj.SetActive(true);

        // 根据提现步骤更新任务界面
        UpdateTaskStepUI();
        
    }

    private void UpdateTaskStepUI()
    {
        if (m_ItemData.MakeupData == null) return;
        switch (m_ItemData.MakeupData.makeupStep)
        {
            case MakeupStep.Step1:
                UpdateStep1UI();
                break;
            case MakeupStep.Step2:
                UpdateStep2UI();
                break;
            case MakeupStep.Success:
                UpdateSuccessUI();
                break;
            case MakeupStep.Fail:
                UpdateFailUI();
                break;
        }
    }

    private void UpdateStep1UI()
    {
        // 显示进度条
        if (varSlider != null)
        {
            varSlider.gameObject.SetActive(true);

            // 设置进度条上限
            float maxValue = 0;
            if (m_ItemData.Type == MakeupType.Mon && m_ItemData.Config != null)
            {
                maxValue = m_ItemData.Config.Step1_Lv;
            }
            
            varSlider.fillAmount = m_ItemData.MakeupData.step1TaskNum / maxValue;
            varSliderbar.text = $"{m_ItemData.MakeupData.step1TaskNum}/{maxValue}";
            varTaskToggle.text = string.Format(GF.Localization.GetString("MakeUpStep1"), maxValue);
            varTaskToggle.gameObject.SetActive(true);

            var isFinish = m_ItemData.MakeupData.step1TaskNum >= maxValue;
            varMakeupBtn.gameObject.SetActive(isFinish);
            varPlayBtn.gameObject.SetActive(!isFinish);
        }
    }
    

    private void UpdateStep2UI()
    {
        // 显示进度条
        if (varSlider != null)
        {
            varSlider.gameObject.SetActive(true);
        
            // 设置进度条上限
            float maxValue = 0;
            float diffValue = 0;
            if (m_ItemData.Type == MakeupType.Mon && m_ItemData.Config != null)
            {
                maxValue = CommonHelper.GetStep2TargetStored(m_ItemData.MakeupData, m_ItemData.Config);
                diffValue = Mathf.Max(0f, maxValue - m_PlayerData.Dollars);
            }
            varSlider.fillAmount = m_PlayerData.Dollars/maxValue;
            varSliderbar.text = $"{CommonHelper.GetDollarString(m_PlayerData.Dollars)}/{CommonHelper.GetDollarString(maxValue)}";
            varTaskToggle.text = string.Format(GF.Localization.GetString("MakeUpStep2"), CommonHelper.GetDollarString(diffValue));
            varTaskToggle.gameObject.SetActive(true);
            
            varMakeupBtn.gameObject.SetActive(true);
            varPlayBtn.gameObject.SetActive(false);
        }
    }

    private void UpdateSuccessUI()
    {
        // 隐藏进度条
        if (varSlider != null)
        {
            varSlider.gameObject.SetActive(false);
        }

    }

    private void UpdateFailUI()
    {
        // 隐藏进度条
        if (varSlider != null)
        {
            varSlider.gameObject.SetActive(false);
        }
        varTaskToggle.gameObject.SetActive(true);
        varTaskToggle.transform.GetComponentInChildren<TextMeshProUGUI>().text = GF.Localization.GetString("MakeUpFail");
    }

    private void OnMakeupButtonClick()
    {
        if (m_ItemData.CanMakeup && m_ListBank != null)
        {
            // 还没有输入账号信息
            if (string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.accountId))
            {
                GF.UI.OpenUIForm(UIViews.ArrowSelectPaymentUIForm);
                return;
            }
            else if (string.IsNullOrEmpty(m_ItemData.MakeupData?.paymentInfo?.accountId))
            {
                var uiParams = UIParams.Create();
                // uiParams.Set<VarInt32>("MakeupCount", m_ItemData.Type == MakeupType.Mon ? m_ItemData.Config.Step2_Money : 999999);
                uiParams.Set<VarInt32>("MakeupType", (int)m_ItemData.Type);
                uiParams.Set<VarInt32>("MakeupId", m_ItemData.Type == MakeupType.Mon ? m_ItemData.Config.Id : 1);
                VarAction MakeupAction = ReferencePool.Acquire<VarAction>();
                MakeupAction.Value = StartMakeup;
                uiParams.Set<VarAction>("StartMakeup", MakeupAction);
                GF.UI.OpenUIForm(UIViews.ArrowMakeupInfoUIForm, uiParams);
                return;
            }
            // m_ListBank.StartMakeup(m_ItemData.Type, m_ItemData.Type == MakeupType.Money ? m_ItemData.Config.Id : m_ItemData.CoinConfig.Id);
        }
        else if (!m_ItemData.CanMakeup)
        {
            GF.UI.ShowToast("Please collect more.");
        }
    }

    private void OnPlayButtonClick()
    {
        GF.UI.CloseUIForms(UIViews.ArrowMakeupUIForm);
    }

    private void StartMakeup()
    {
        m_ListBank.StartMakeup(m_ItemData.Type, m_ItemData.Config.Id);
    }

    public Transform GuideBgTransform => varBg != null ? varBg.transform : transform;

    public Transform GuidePlayBtnTransform => varPlayBtn != null ? varPlayBtn.transform : transform;

    private void OnTaskButtonClick()
    {
        if (m_ItemData.MakeupData == null) return;
        switch (m_ItemData.MakeupData.makeupStep)
        {
            case MakeupStep.Step1:
                // 任务奖励数
                float maxValue = 0;
                if (m_ItemData.Type == MakeupType.Mon && m_ItemData.Config != null)
                {
                    maxValue = m_ItemData.Config.Step1_Lv;
                }
                // else if (m_ItemData.Type == MakeupType.Coin && m_ItemData.CoinConfig != null)
                // {
                //     maxValue = m_ItemData.CoinConfig.Step1_num;
                // }
                if (m_ItemData.MakeupData.step1TaskNum >= maxValue)
                {
                    GF.UI.ShowToast("Please Wait");
                }
                else
                {
                    GF.UI.ShowToast("Please play the game to get more rewards.");
                }
                break;
            case MakeupStep.Step2:
                GF.UI.ShowToast("Please Wait");
                break;
            

        }
    }
}