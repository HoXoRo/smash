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
        varBtnVideo.onClick.AddListener(ShowVideo);
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

    private void ShowVideo()
    {
        CommonHelper.ShowVideoAd("debug_video", null);
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
