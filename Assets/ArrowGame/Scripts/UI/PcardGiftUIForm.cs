using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PcardGiftUIForm")]
public partial class PcardGiftUIForm : UIFormBase
{
    private bool m_IsClaiming;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        varBtnClaim.onClick.AddListener(OnClaimClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(PcardGiftUIForm));
        m_IsClaiming = false;
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
        if (m_IsClaiming) return;
        if (PcardGiftRewardSession.Count <= 0)
        {
            GF.UI.Close(UIForm);
            return;
        }

        UIParams rewardParams = UIParams.Create();
        rewardParams.CloseCallback = Params.CloseCallback;
        if (GF.UI.OpenUIForm(UIViews.PcardRewardUIForm, rewardParams) < 0) return;
        m_IsClaiming = true;
        // Continue only after the reward form closes, not when switching to it.
        Params.CloseCallback = null;
        GF.UI.Close(UIForm);
    }
}
