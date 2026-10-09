using UnityEngine;

using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PlayUIForm")]

public partial class PlayUIForm : UIFormBase
{
    private static int s_RemainingBallCount;
    private static int s_RocketUseCount;

    public static void SetRocketUseCount(int count)
    {
        s_RocketUseCount = Mathf.Max(0, count);
        SetRemainingBallCount(s_RemainingBallCount);
    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnSetting.onClick.AddListener(OnSettingClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RefreshRemainingBallCount();
    }

    public static void SetRemainingBallCount(int count)
    {
        s_RemainingBallCount = Mathf.Max(0, count);

        string assetName = GF.UI.GetUIFormAssetName(UIViews.PlayUIForm);
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
            if (uiForm.Logic is PlayUIForm playUIForm)
            {
                playUIForm.RefreshRemainingBallCount();
            }
        }
    }

    private void RefreshRemainingBallCount()
    {
        if (varTxtCount != null)
        {
            varTxtCount.text = s_RemainingBallCount.ToString();
        }
    }

    void OnSettingClicked()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarBoolean>(SettingPageUIForm.P_FromGameplay, true);
        OpenSubUIForm(UIViews.SettingPageUIForm, @params: uiParams);
    }
}
