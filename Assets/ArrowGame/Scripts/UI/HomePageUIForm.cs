using Cysharp.Threading.Tasks;
using UnityEngine;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/HomePageUIForm")]

public partial class HomePageUIForm : UIFormBase
{
    private static bool isEnteringGameplay;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnPlay.onClick.AddListener(OnPlayClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        int currentLevel = GetCurrentLevel();
        varLevelTxt.text = GF.Localization.GetString("level", currentLevel);
    }

    static int GetCurrentLevel()
    {
        PlayerDataModel playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        return Mathf.Max(1, playerData?.LevelId ?? 1);
    }

    void OnPlayClicked()
    {
        if (isEnteringGameplay || PlayLoadingUIForm.IsLoading) return;
        if (!LifeHelper.HasLife())
        {
            GF.UI.OpenUIForm(UIViews.AddLifeUIForm);
            return;
        }
        if (GetCurrentLevel() > 25)
        {
            GF.UI.OpenUIForm(UIViews.PrelevelUIForm);
            return;
        }
        EnterGameplayAsync().Forget();
    }

    async UniTaskVoid EnterGameplayAsync()
    {
        await TryEnterGameplayAsync(false);
    }

    public static async UniTask<bool> TryEnterGameplayAsync(bool useRocket)
    {
        if (isEnteringGameplay || PlayLoadingUIForm.IsLoading || GameFlowHelper.IsReturningToHall) return false;
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (useRocket && !playerData.TryUseRocket()) return false;
        isEnteringGameplay = true;
        playerData.PendingRocketUse = useRocket;
        bool loaded = false;
        try
        {
            loaded = await PlayLoadingUIForm.LoadGameplayAsync();
            return loaded;
        }
        catch (System.Exception exception)
        {
            UnityGameFramework.Runtime.Log.Error(exception.ToString());
            return false;
        }
        finally
        {
            if (!loaded)
            {
                playerData.PendingRocketUse = false;
                if (useRocket) playerData.AddRocket();
            }
            isEnteringGameplay = false;
        }
    }

    public static void CloseHallTabAfterGameplayLoaded()
    {
        string hallTabAsset = GF.UI.GetUIFormAssetName(UIViews.HallTabUIForm);
        if (string.IsNullOrEmpty(hallTabAsset))
        {
            return;
        }

        var hallTabForms = GF.UI.GetUIForms(hallTabAsset);
        if (hallTabForms == null)
        {
            return;
        }

        foreach (var uiForm in hallTabForms)
        {
            if (uiForm.Logic is HallTabUIForm hallTab)
            {
                hallTab.CloseBeforeEnterGameplay();
                break;
            }
        }
    }
}
