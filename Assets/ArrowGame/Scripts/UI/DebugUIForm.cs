using UnityEngine;
using TMPro;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/DebugUIForm")]

public partial class DebugUIForm : UIFormBase
{

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        varAddDollar.onClick.AddListener(AddDollar);
        varReduceDollor.onClick.AddListener(ReduceDollar);
        varAddPcard.onClick.AddListener(AddPcard);
        varReducePcard.onClick.AddListener(ReducePcard);
        varBtnVideo.onClick.AddListener(ShowInterstitial);
        varBtnAddLv.onClick.AddListener(AddLevel);
        varBtnReduceLv.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ReduceLevel);
        varBtnResetTask.onClick.AddListener(ResetTasks);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    private void AddDollar()
    {
        float amount = GetInputDollar();
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerData.Dollars = playerData.Dollars + amount;
    }

    private void ReduceDollar()
    {
        float amount = GetInputDollar();
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerData.Dollars = Mathf.Max(0f, playerData.Dollars - amount);
    }

    private float GetInputDollar()
    {
        if (varInputDollar == null || !float.TryParse(varInputDollar.text, out float amount))
        {
            return 0f;
        }

        return Mathf.Max(0f, amount);
    }

    private void AddPcard()
    {
        ChangePcard(GetInputPcard());
    }

    private void ReducePcard()
    {
        ChangePcard(-GetInputPcard());
    }

    private void ChangePcard(float amount)
    {
        if (amount == 0f) return;

        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        ArrowUITopbar.CompletePcardDisplay(playerData);
        playerData.Pcard = Mathf.Max(0f, playerData.Pcard + amount);
        playerData.SetPcardDisplayValue(playerData.Pcard);
    }

    private float GetInputPcard()
    {
        if (varInputPcard == null || !float.TryParse(varInputPcard.text, out float amount)
            || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return 0f;
        }

        return Mathf.Max(0f, amount);
    }

    private void ShowInterstitial()
    {
        CommonHelper.ShowInterstitial("debug_interstitial");
    }

    private void AddLevel()
    {
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (playerData.LevelId >= int.MaxValue) return;
        playerData.LevelId++;
        playerData.Save();
    }

    private void ReduceLevel()
    {
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (playerData.LevelId <= 1) return;
        playerData.LevelId--;
        playerData.Save();
    }

    private void ResetTasks()
    {
        GF.DataModel.GetOrCreate<PlayerDataModel>().ResetTaskProgress();
    }
}
