using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupPaymentEndUIForm")]
public partial class MakeupPaymentEndUIForm : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(MakeupPaymentEndUIForm));
    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
    }
}