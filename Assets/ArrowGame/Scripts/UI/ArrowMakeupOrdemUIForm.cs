using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/MakeupOrdemUIForm")]
public partial class ArrowMakeupOrdemUIForm : UIFormBase
{
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Button_Close":
                GF.UI.Close(this.UIForm);
                break;
        }
    }

}