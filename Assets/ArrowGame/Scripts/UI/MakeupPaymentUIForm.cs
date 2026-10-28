using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupPaymentUIForm")]
public partial class MakeupPaymentUIForm : UIFormBase
{
    public const string P_RewardText = "RewardText";
    public const string P_RequiredPcard = "RequiredPcard";

    private RequestHandle request;
    private float requiredPcard;
    private bool isRequesting;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
        if (varBtnCard != null)
            varBtnCard.onClick.AddListener(() => StartPayment(false));
        if (varBtnICRC != null)
            varBtnICRC.onClick.AddListener(() => StartPayment(true));
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(MakeupPaymentUIForm));
        request?.Cancel();
        request = null;
        SetRequesting(false);
        requiredPcard = Params.Get<VarSingle>(P_RequiredPcard)?.Value ?? 0f;
        if (varTxtReward != null)
            varTxtReward.text = Params.Get<VarString>(P_RewardText);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        request?.Cancel();
        request = null;
        base.OnClose(isShutdown, userData);
    }

    public override void OnClickClose()
    {
        if (isRequesting) return;
        base.OnClickClose();
    }

    private void SetRequesting(bool value)
    {
        isRequesting = value;
        if (varBtnClose != null) varBtnClose.interactable = !value;
        if (varBtnCard != null) varBtnCard.interactable = !value;
        if (varBtnICRC != null) varBtnICRC.interactable = !value;
    }

    private void StartPayment(bool shouldSucceed)
    {
        if (isRequesting) return;
        SetRequesting(true);
        request = SimulatedRequestHelper.Send(
            onSuccess: () =>
            {
                if (shouldSucceed) CompletePayment();
                else OnPaymentFailed(RequestFailure.Failed);
            },
            onFailure: OnPaymentFailed,
            showLoading: true);
    }

    private void OnPaymentFailed(RequestFailure reason)
    {
        SetRequesting(false);
    }

    private void CompletePayment()
    {
        PlayerDataModel player = GF.DataModel.GetOrCreate<PlayerDataModel>();
        // Validate again after the request, as the balance may have changed.
        if (float.IsNaN(requiredPcard) || float.IsInfinity(requiredPcard) || requiredPcard <= 0f)
        {
            OnPaymentFailed(RequestFailure.Failed);
            return;
        }
        if (player.Pcard < requiredPcard)
        {
            SetRequesting(false);
            GF.UI.ShowToast("Not enough");
            return;
        }

        ArrowUITopbar.CompletePcardDisplay(player);
        player.Pcard -= requiredPcard;
        player.SetPcardDisplayValue(player.Pcard);
        GF.UI.CloseUIForm(Id);
        GF.UI.OpenUIForm(UIViews.MakeupPaymentEndUIForm);
    }
}
