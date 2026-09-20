using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class ArrowWithdrawalInfoItem : UIItemBase
{
    protected override void OnInit()
    {
        base.OnInit();
    }
    public void UpdateUI(string payment, string name,string level, string money)
    {
        varBankIcon.SetSprite($"UI/BankIcon/{payment}.png");
        varInfoTxt.text = string.Format(GF.Localization.GetString("ArrowWithdrawalInfoItem.infoTxt"), name, level, money);
    }
}