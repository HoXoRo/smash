using System.Text.RegularExpressions;
using UnityEngine;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupInfoUIForm")]
public partial class MakeupInfoUIForm : UIFormBase
{
    const string HintMismatchKey = "makeupInfoHint1";
    const string HintEmailKey = "makeupInfoHint2";
    static readonly Regex EmailRegex = new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.CultureInvariant);

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
        if (varBtnConfirm != null)
            varBtnConfirm.onClick.AddListener(OnClickConfirm);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HideHint();
    }

    void OnClickConfirm()
    {
        GF.Sound.PlayEffect("ui/ui_click.mp3");

        string account = varInputAcct != null ? varInputAcct.text.Trim() : string.Empty;
        string confirm = varInputAcctConfirm != null ? varInputAcctConfirm.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(confirm) || account != confirm)
        {
            ShowHint(HintMismatchKey);
            return;
        }

        if (!EmailRegex.IsMatch(account))
        {
            ShowHint(HintEmailKey);
            return;
        }

        PlayerDataModel playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerDm.MakeupAccount = account;
        playerDm.Save();
        RefreshMakeupAccountDisplay();
        HideHint();
        GF.UI.Close(this.UIForm);
    }

    void ShowHint(string key)
    {
        if (varTxtHint == null)
            return;

        varTxtHint.text = GF.Localization.GetString(key);
        varTxtHint.gameObject.SetActive(true);
    }

    void HideHint()
    {
        if (varTxtHint != null)
            varTxtHint.gameObject.SetActive(false);
    }

    void RefreshMakeupAccountDisplay()
    {
        string assetName = GF.UI.GetUIFormAssetName(UIViews.MakeupUIForm);
        if (string.IsNullOrEmpty(assetName))
            return;

        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null)
            return;

        foreach (var uiForm in forms)
        {
            if (uiForm.Logic is MakeupUIForm makeup)
                makeup.RefreshAccountText();
        }
    }
}
