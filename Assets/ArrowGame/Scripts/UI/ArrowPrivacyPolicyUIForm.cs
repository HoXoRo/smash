using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowPrivacyPolicyUIForm")]
public partial class ArrowPrivacyPolicyUIForm : UIFormBase
{
    const string LocalizationTitleKey = "ArrowPrivacyPolicyUIForm.Title";
    const string LocalizationBodyKey = "ArrowPrivacyPolicyUIForm.Body";

    static readonly string DefaultBody =
        "Privacy Policy\n\n" +
        "This screen shows your privacy policy text. Replace it by:\n" +
        "• Passing UIParams.PrivacyPolicyText when opening this form, or\n" +
        "• Adding localization for key \"ArrowPrivacyPolicyUIForm.Body\".\n";

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ApplyTexts();
        NormalizeScroll();
    }

    void ApplyTexts()
    {

    }

    void NormalizeScroll()
    {
        if (varScrollBody == null || varPolicyText == null)
            return;

        // RectTransform content = varScrollBody.content;
        // Canvas.ForceUpdateCanvases();
        // LayoutRebuilder.ForceRebuildLayoutImmediate(varPolicyText.rectTransform);
        // if (content != null)
        //     LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        // varScrollBody.verticalNormalizedPosition = 1f;
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        if (btId == "Button_Close")
            GF.UI.Close(this.UIForm);
    }
}
