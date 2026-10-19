using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay;
using Scene;
using Service;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PlayLoadingUIForm")]
public partial class PlayLoadingUIForm : UIFormBase
{
    private const float LoadTimeoutSeconds = 15f;
    public static bool IsLoading { get; private set; }

    public static async UniTask<bool> LoadGameplayAsync()
    {
        if (IsLoading) return false;
        IsLoading = true;
        string sceneAsset = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
        int loadingId = -1;
        int playId = -1;
        bool success = false;
        UniTask pendingLoad = UniTask.CompletedTask;
        var cancellation = new CancellationTokenSource();
        var timeout = cancellation.CancelAfterSlim(TimeSpan.FromSeconds(LoadTimeoutSeconds), DelayType.Realtime);
        double deadline = Time.realtimeSinceStartupAsDouble + LoadTimeoutSeconds;
        CancellationToken token = cancellation.Token;
        try
        {
            loadingId = GF.UI.OpenUIForm(UIViews.PlayLoadingUIForm);
            await WaitForFormAsync(loadingId, token);
            // Give the loading screen a frame to render before preparing the level.
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            GF.UI.CloseUIForms(UIViews.PrelevelUIForm);
            GF.UI.CloseUIForms(UIViews.PlayUIForm);
            SceneHandler.BeginGameplayLoading();
            pendingLoad = PrepareSceneAsync(sceneAsset, token).Preserve();
            await pendingLoad.AttachExternalCancellation(token);
            if (Time.realtimeSinceStartupAsDouble >= deadline) cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            GameController controller = ServiceLocator.Get<GameController>();
            if (controller == null) throw new InvalidOperationException("Gameplay controller is missing.");
            playId = GF.UI.OpenUIForm(UIViews.PlayUIForm);
            await WaitForFormAsync(playId, token);
            if (Time.realtimeSinceStartupAsDouble >= deadline) cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            HomePageUIForm.CloseHallTabAfterGameplayLoaded();
            ArrowUITopbar.SetCoinVisible(false);
            GF.UI.CloseUIForm(loadingId);
            loadingId = -1;
            controller.ExposeGameplay();
            success = true;
            SceneHandler.FinishGameplayLoading(true);
            return true;
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Gameplay loading timed out after 15 seconds.");
            return false;
        }
        catch (Exception exception)
        {
            Log.Error(exception.ToString());
            return false;
        }
        finally
        {
            timeout.Dispose();
            cancellation.Cancel();
            cancellation.Dispose();
            if (loadingId >= 0) GF.UI.CloseUIForm(loadingId);
            if (success)
            {
                IsLoading = false;
            }
            else
            {
                if (playId >= 0) GF.UI.CloseUIForm(playId);
                GF.UI.CloseUIForms(UIViews.PrelevelUIForm);
                GF.UI.CloseUIForms(UIViews.PlayUIForm);
                HideGameplayScene();
                SceneHandler.FinishGameplayLoading(false);
                ArrowUITopbar.SetCoinVisible(true);
                if (!GF.UI.HasUIForm(UIViews.HallTabUIForm) && !GF.UI.IsLoadingUIForm(UIViews.HallTabUIForm))
                    GF.UI.OpenUIForm(UIViews.HallTabUIForm);
                // Unity scene IO cannot be interrupted. Reject its late result and unload it;
                // keep the entry guard until cleanup finishes to avoid same-scene load races.
                CleanupSceneAsync(pendingLoad, sceneAsset).Forget();
            }
        }
    }

    private static async UniTask WaitForFormAsync(int serialId, CancellationToken token)
    {
        if (serialId < 0) throw new InvalidOperationException("Could not open gameplay UI.");
        await UniTask.WaitUntil(() => !GF.UI.IsLoadingUIForm(serialId), cancellationToken: token);
        if (!GF.UI.HasUIForm(serialId)) throw new InvalidOperationException("Gameplay UI failed to load.");
    }

    private static async UniTask PrepareSceneAsync(string sceneAsset, CancellationToken token)
    {
        if (GF.Scene.SceneIsLoaded(sceneAsset))
        {
            if (!await GF.Scene.UnLoadSceneAwait(sceneAsset))
                throw new InvalidOperationException("Could not unload the previous level.");
        }
        token.ThrowIfCancellationRequested();
        if (!await GF.Scene.LoadSceneAwait(sceneAsset))
            throw new InvalidOperationException("Could not load the gameplay scene.");
        token.ThrowIfCancellationRequested();
        GameController controller = ServiceLocator.Get<GameController>();
        if (controller == null) throw new InvalidOperationException("Gameplay controller is missing.");
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, controller.GetCancellationTokenOnDestroy()))
            await controller.PrepareGameplayAsync(linked.Token);
    }

    private static void HideGameplayScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Gameplay");
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects()) root.SetActive(false);
    }

    private static async UniTaskVoid CleanupSceneAsync(UniTask pendingLoad, string sceneAsset)
    {
        try
        {
            try { await pendingLoad; }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Log.Warning(exception.ToString()); }
            HideGameplayScene();
            if (GF.Scene.SceneIsLoaded(sceneAsset)) await GF.Scene.UnLoadSceneAwait(sceneAsset);
        }
        catch (Exception exception) { Log.Error(exception.ToString()); }
        finally { IsLoading = false; }
    }
}
