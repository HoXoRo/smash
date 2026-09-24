using Cysharp.Threading.Tasks;
using UnityEngine;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/HomePageUIForm")]

public partial class HomePageUIForm : UIFormBase
{
    const string GameplaySceneName = "Gameplay";

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnPlay.onClick.AddListener(OnPlayClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        int currentLevel = GetCurrentLevel();
        varLevelTxt.text = $"Level {currentLevel}";
    }

    static int GetCurrentLevel()
    {
        PlayerDataModel playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        return Mathf.Max(1, playerData?.LevelId ?? 1);
    }

    void OnPlayClicked()
    {
        GF.Sound.PlayEffect("ui/ui_click.mp3");
        EnterGameplayAsync().Forget();
    }

    async UniTaskVoid EnterGameplayAsync()
    {
        string sceneAssetName = UtilityBuiltin.AssetsPath.GetScenePath(GameplaySceneName);
        if (!await GF.Scene.LoadSceneAwait(sceneAssetName))
        {
            return;
        }

        CloseHallTabAfterGameplayLoaded();
    }

    static void CloseHallTabAfterGameplayLoaded()
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
