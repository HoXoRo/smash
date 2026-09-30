using UnityEngine;

using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PlayUIForm")]

public partial class PlayUIForm : UIFormBase
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnSetting.onClick.AddListener(OnSettingClicked);
    }

    void OnSettingClicked()
    {
        GF.Sound.PlayEffect("ui/ui_click.mp3");
        var uiParams = UIParams.Create();
        uiParams.Set<VarBoolean>(SettingPageUIForm.P_FromGameplay, true);
        OpenSubUIForm(UIViews.SettingPageUIForm, @params: uiParams);
    }
}
