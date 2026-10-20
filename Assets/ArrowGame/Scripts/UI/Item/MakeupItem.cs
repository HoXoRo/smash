using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class MakeupItem : UIItemBase
{
    string m_RewardText;
    MakeupTask m_Task;
    PlayerDataModel m_Player;
    float m_RequiredAmount;
    float m_NextRefreshTime;

    protected override void OnInit()
    {
        base.OnInit();
        if (varBtnCashout != null)
            varBtnCashout.onClick.AddListener(OnClickCashout);
    }

    public void SetData(MakeupTask task)
    {
        m_Task = task;
        m_Player = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (task == null)
        {
            RefreshCashoutState();
            return;
        }

        if (varTxtLv != null)
            varTxtLv.text = task.Level.ToString();

        float value = task.Mon > 0f ? task.Mon : task.Pcard;
        m_RequiredAmount = value * task.Rate;
        m_RewardText = CommonHelper.GetDollarString(m_RequiredAmount);
        if (varTxtReward != null)
            varTxtReward.text = m_RewardText;
        RefreshCashoutState();
    }

    bool CanCashout()
    {
        if (m_Task == null || m_Player == null || m_Player.LevelId < m_Task.Level
            || m_Task.Rate <= 0f || m_RequiredAmount <= 0f)
            return false;

        if (m_Task.Mon > 0f)
            return m_Player.Dollars >= m_RequiredAmount;
        if (m_Task.Pcard > 0f)
            return m_Player.Pcard / GF.Config.GetFloat("MakeupRatio") >= m_RequiredAmount;
        return false;
    }

    void RefreshCashoutState()
    {
        bool unavailable = !CanCashout();
        if (varImgNo != null && varImgNo.activeSelf != unavailable)
            varImgNo.SetActive(unavailable);
    }

    void Update()
    {
        if (m_Task == null || Time.unscaledTime < m_NextRefreshTime) return;
        m_NextRefreshTime = Time.unscaledTime + 0.25f;
        // Real balance can change without a display refresh or a data-change event.
        RefreshCashoutState();
    }

    void OnClickCashout()
    {
        if (m_Task == null) return;
        RefreshCashoutState();
        if (!CanCashout())
        {
            GF.UI.ShowToast(GF.Localization.GetString("cashoutHint", m_Task.Level, m_RewardText));
            return;
        }

        if (GF.UI.HasUIForm(UIViews.MakeupInfoUIForm) || GF.UI.IsLoadingUIForm(UIViews.MakeupInfoUIForm)
            || GF.UI.HasUIForm(UIViews.MakeupPaymentUIForm) || GF.UI.IsLoadingUIForm(UIViews.MakeupPaymentUIForm))
            return;

        if (string.IsNullOrWhiteSpace(m_Player.MakeupAccount))
        {
            GF.UI.OpenUIForm(UIViews.MakeupInfoUIForm);
            return;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarString>(MakeupPaymentUIForm.P_RewardText, m_RewardText);
        GF.UI.OpenUIForm(UIViews.MakeupPaymentUIForm, uiParams);
    }
}
