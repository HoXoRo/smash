using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class MakeupItem : UIItemBase
{
    string m_RewardText;

    protected override void OnInit()
    {
        base.OnInit();
        if (varBtnCashout != null)
            varBtnCashout.onClick.AddListener(OnClickCashout);
    }

    public void SetData(MakeupTask task)
    {
        if (task == null)
            return;

        if (varTxtLv != null)
            varTxtLv.text = task.Level.ToString();

        float value = task.Mon > 0f ? task.Mon : task.Reward;
        m_RewardText = CommonHelper.GetDollarString(value * task.Rate);
        if (varTxtReward != null)
            varTxtReward.text = m_RewardText;
    }

    void OnClickCashout()
    {
        if (GF.UI.HasUIForm(UIViews.MakeupPaymentUIForm) || GF.UI.IsLoadingUIForm(UIViews.MakeupPaymentUIForm))
            return;

        GF.Sound.PlayEffect("ui/ui_click.mp3");
        var uiParams = UIParams.Create();
        uiParams.Set<VarString>(MakeupPaymentUIForm.P_RewardText, m_RewardText);
        GF.UI.OpenUIForm(UIViews.MakeupPaymentUIForm, uiParams);
    }
}
