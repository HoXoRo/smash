using System.Text.RegularExpressions;
using UnityEngine;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupInfoUIForm")]
public partial class MakeupInfoUIForm : UIFormBase
{
    const string HintMismatchKey = "makeupInfoHint1";
    const string HintEmailKey = "makeupInfoHint2";
    static readonly Regex EmailRegex = new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.CultureInvariant);

    [SerializeField] private UnityEngine.UI.Button[] avatarButtons;
    [SerializeField] private GameObject[] avatarSelections;
    private int selectedAvatarIndex;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
        if (varBtnConfirm != null)
            varBtnConfirm.onClick.AddListener(OnClickConfirm);
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i;
            avatarButtons[i].onClick.AddListener(() => OnClickAvatar(index));
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(MakeupInfoUIForm));
        HideHint();
        SelectAvatar(GF.DataModel.GetOrCreate<PlayerDataModel>().MakeupAvatarIndex);
    }

    void OnClickAvatar(int index)
    {
        SelectAvatar(index);
    }

    void SelectAvatar(int index)
    {
        selectedAvatarIndex = Mathf.Clamp(index, 0, avatarButtons.Length - 1);
        for (int i = 0; i < avatarSelections.Length; i++)
            avatarSelections[i].SetActive(i == selectedAvatarIndex);
    }

    void OnClickConfirm()
    {
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
        playerDm.MakeupAvatarIndex = selectedAvatarIndex;
        playerDm.Save();
        CommonHelper.LogEvent(AdjustEventCodeEvent.makeupinfo_confirm);
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
