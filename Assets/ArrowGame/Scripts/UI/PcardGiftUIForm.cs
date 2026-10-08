using UnityEngine;
using UnityEngine.UI;
using TMPro;


#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PcardGiftUIForm")]
public partial class PcardGiftUIForm : UIFormBase
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        varBtnClaim.onClick.AddListener(OnClaimClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        PcardGiftRewardSession.Changed += RefreshCount;
        RefreshCount();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        PcardGiftRewardSession.Changed -= RefreshCount;
        base.OnClose(isShutdown, userData);
    }

    private void RefreshCount()
    {
        if (varTxtGiftCount != null)
        {
            varTxtGiftCount.text = $"x{PcardGiftRewardSession.Count}";
        }
    }

    private void OnClaimClicked()
    {
        if (PcardGiftRewardSession.Count <= 0 ||
            GF.UI.HasUIForm(UIViews.PcardRewardUIForm) ||
            GF.UI.IsLoadingUIForm(UIViews.PcardRewardUIForm))
        {
            return;
        }

        GF.Sound.PlayEffect("ui/ui_click.mp3");
        GF.UI.OpenUIForm(UIViews.PcardRewardUIForm);
        GF.UI.Close(UIForm);
    }
}
