using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/AddBallsUIForm")]
public partial class AddBallsUIForm : UIFormBase
{
    public const string P_OnRefill = "OnRefill";
    public const string P_OnQuit = "OnQuit";

    private PlayerDataModel playerData;
    private Action onRefill;
    private Action onQuit;
    private int ballsCoin;
    private bool isBusy;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnAd.onClick.AddListener(AddBallsByAd);
        varBtnCoin.onClick.AddListener(AddBallsByCoin);
        varBtnClose.onClick.AddListener(ReturnToHall);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        isBusy = false;
        onRefill = Params.Get<VarAction>(P_OnRefill)?.Value;
        onQuit = Params.Get<VarAction>(P_OnQuit)?.Value;
        playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        ballsCoin = GF.Config.GetInt("ballsCoin");
        varTxtCoin.text = ballsCoin.ToString();
        RefreshButtons();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        onRefill = null;
        onQuit = null;
        base.OnClose(isShutdown, userData);
    }

    private void RefreshButtons()
    {
        varBtnAd.interactable = !isBusy && onRefill != null;
        varBtnCoin.interactable = !isBusy && onRefill != null && ballsCoin >= 0 && playerData.Coins >= ballsCoin;
        varBtnClose.interactable = !isBusy;
    }

    private void AddBallsByCoin()
    {
        if (isBusy || onRefill == null) return;
        if (!playerData.TrySpendCoins(ballsCoin))
        {
            RefreshButtons();
            return;
        }
        CompleteRefill();
    }

    private void AddBallsByAd()
    {
        if (isBusy || onRefill == null) return;
        isBusy = true;
        RefreshButtons();
        bool completed = false;
        CommonHelper.ShowVideoAd("balls_refill", success =>
        {
            if (completed) return;
            completed = true;
            if (success)
            {
                CompleteRefill();
                return;
            }
            isBusy = false;
            RefreshButtons();
        });
    }

    private void CompleteRefill()
    {
        isBusy = true;
        RefreshButtons();
        Action refill = onRefill;
        GF.UI.CloseUIForm(Id);
        refill?.Invoke();
    }

    private void ReturnToHall()
    {
        if (isBusy) return;
        ReturnToHallAsync().Forget();
    }

    private async UniTaskVoid ReturnToHallAsync()
    {
        isBusy = true;
        RefreshButtons();
        onQuit?.Invoke();
        onQuit = null;
        try
        {
            string sceneAssetName = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
            if (!await GF.Scene.UnLoadSceneAwait(sceneAssetName))
            {
                isBusy = false;
                RefreshButtons();
                return;
            }
        }
        catch (Exception exception)
        {
            isBusy = false;
            RefreshButtons();
            Log.Error(exception.ToString());
            return;
        }

        GF.UI.CloseUIForm(Id);
        GF.UI.CloseUIForms(UIViews.SettingPageUIForm);
        GF.UI.CloseUIForms(UIViews.PlayUIForm);
        if (!GF.UI.HasUIForm(UIViews.HallTabUIForm) &&
            !GF.UI.IsLoadingUIForm(UIViews.HallTabUIForm))
        {
            GF.UI.OpenUIForm(UIViews.HallTabUIForm);
        }
    }
}
