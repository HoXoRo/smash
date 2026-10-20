using System;
using Cysharp.Threading.Tasks;
using Scene;
using UnityGameFramework.Runtime;

public static class GameFlowHelper
{
    public static bool IsReturningToHall { get; private set; }

    public static async UniTask<bool> ReturnToHallAsync(int sourceUIId)
    {
        if (IsReturningToHall || PlayLoadingUIForm.IsLoading) return false;
        IsReturningToHall = true;
        try
        {
            string sceneAssetName = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
            if (GF.Scene.SceneIsLoaded(sceneAssetName) &&
                !await GF.Scene.UnLoadSceneAwait(sceneAssetName)) return false;

            SceneHandler.FinishReturnToHall();
            GF.UI.CloseUIForms(UIViews.SettingPageUIForm);
            GF.UI.CloseUIForms(UIViews.PlayUIForm);

            // Reuse a pending hall load instead of opening a second instance.
            if (GF.UI.IsLoadingUIForm(UIViews.HallTabUIForm))
                await UniTask.WaitUntil(() => !GF.UI.IsLoadingUIForm(UIViews.HallTabUIForm));

            if (!GF.UI.HasUIForm(UIViews.HallTabUIForm) &&
                await GF.UI.OpenUIFormAwait(UIViews.HallTabUIForm) == null) return false;

            GF.UI.CloseUIForm(sourceUIId);
            if (PcardGiftRewardSession.Count > 0 &&
                !GF.UI.HasUIForm(UIViews.PcardGiftUIForm) &&
                !GF.UI.IsLoadingUIForm(UIViews.PcardGiftUIForm))
            {
                GF.UI.OpenUIForm(UIViews.PcardGiftUIForm);
            }
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception.ToString());
            return false;
        }
        finally
        {
            IsReturningToHall = false;
        }
    }
}
