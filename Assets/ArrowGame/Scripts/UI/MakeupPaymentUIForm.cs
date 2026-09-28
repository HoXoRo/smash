using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupPaymentUIForm")]
public partial class MakeupPaymentUIForm : UIFormBase
{
    public const string P_RewardText = "RewardText";

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        if (varTxtReward == null)
            return;

        varTxtReward.text = Params.Get<VarString>(P_RewardText);
    }
}