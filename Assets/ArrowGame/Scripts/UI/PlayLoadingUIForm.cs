using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay;
using Gameplay.Level;
using System.Collections.Generic;
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
    public static bool IsPreparingStartup { get; private set; }
    private static GameplayAssetCache.Lease startupAssets;
    private static List<GameObject> startupActiveRoots;
    private static int startupLevel = -1;

    // Called once by startup, after configuration/localization is ready. Keep the
    // additive scene hidden and retain resource references until first entry.
    public static async UniTask PrepareStartupGameplayAsync()
    {
        if (IsPreparingStartup || startupLevel >= 0 || IsLoading) return;
        string sceneAsset = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
        if (GF.Scene.SceneIsLoaded(sceneAsset)) return;
        IsPreparingStartup = true;
        double started = Time.realtimeSinceStartupAsDouble;
        int level = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(GF.Base.GetCancellationTokenOnDestroy());
        var timeout = cancellation.CancelAfterSlim(TimeSpan.FromSeconds(LoadTimeoutSeconds), DelayType.Realtime);
        CancellationToken token = cancellation.Token;
        bool prepared = false;
        try
        {
            Log.Info($"[GameplayLoad] Startup prepare begin: level={level}");
            // Unity scene IO cannot be cancelled: await completion before cleanup
            // so a late load can never collide with the normal entry fallback.
            if (!await GF.Scene.LoadSceneAwait(sceneAsset))
                throw new InvalidOperationException("Could not preload gameplay scene.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Gameplay");
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Preloaded gameplay scene is missing.");
            startupActiveRoots = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!root.activeSelf) continue;
                startupActiveRoots.Add(root);
                root.SetActive(false);
            }
            token.ThrowIfCancellationRequested();
            Log.Info($"[GameplayLoad] Startup scene ready: {(Time.realtimeSinceStartupAsDouble - started) * 1000:F0}ms");
            var levelData = await LevelLoader.GetLevelAsync(level).AttachExternalCancellation(token);
            startupAssets = await GameplayAssetCache.PrepareAsync(levelData, token);
            token.ThrowIfCancellationRequested();
            startupLevel = level;
            prepared = true;
            Log.Info($"[GameplayLoad] Startup prepared: level={level}, total={(Time.realtimeSinceStartupAsDouble - started) * 1000:F0}ms");
        }
        catch (Exception exception)
        {
            Log.Warning($"[GameplayLoad] Startup prepare failed; normal entry will retry: {exception.Message}");
        }
        finally
        {
            timeout.Dispose();
            if (!prepared)
            {
                ReleaseStartupAssets();
                startupActiveRoots = null;
                try
                {
                    HideGameplayScene();
                    if (GF.Scene.SceneIsLoaded(sceneAsset)) await GF.Scene.UnLoadSceneAwait(sceneAsset);
                }
                catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
            }
            IsPreparingStartup = false;
        }
    }

    private static void ReleaseStartupAssets()
    {
        startupAssets?.Dispose();
        startupAssets = null;
        startupLevel = -1;
    }

    public static async UniTask<bool> LoadGameplayAsync()
    {
        if (IsLoading || IsPreparingStartup) return false;
        IsLoading = true;
        string sceneAsset = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
        int loadingId = -1;
        int playId = -1;
        bool success = false;
        double loadStarted = Time.realtimeSinceStartupAsDouble;
        double stageStarted = loadStarted;
        string stage = "LoadingUI";
        void BeginStage(string name)
        {
            stage = name;
            stageStarted = Time.realtimeSinceStartupAsDouble;
            Log.Info($"[GameplayLoad] Begin {stage}, elapsed={(stageStarted - loadStarted) * 1000:F0}ms");
        }
        void EndStage()
        {
            Log.Info($"[GameplayLoad] End {stage}: {(Time.realtimeSinceStartupAsDouble - stageStarted) * 1000:F0}ms");
        }
        UniTask pendingLoad = UniTask.CompletedTask;
        var cancellation = new CancellationTokenSource();
        var timeout = cancellation.CancelAfterSlim(TimeSpan.FromSeconds(LoadTimeoutSeconds), DelayType.Realtime);
        double deadline = Time.realtimeSinceStartupAsDouble + LoadTimeoutSeconds;
        CancellationToken token = cancellation.Token;
        try
        {
            BeginStage("LoadingUI");
            loadingId = GF.UI.OpenUIForm(UIViews.PlayLoadingUIForm);
            await WaitForFormAsync(loadingId, token);
            EndStage();
            BeginStage("LoadingUIFrameAndSetup");
            // Give the loading screen a frame to render before preparing the level.
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            GF.UI.CloseUIForms(UIViews.PrelevelUIForm);
            GF.UI.CloseUIForms(UIViews.PlayUIForm);
            SceneHandler.BeginGameplayLoading();
            EndStage();
            pendingLoad = PrepareSceneAsync(sceneAsset, token, BeginStage, EndStage).Preserve();
            await pendingLoad.AttachExternalCancellation(token);
            if (Time.realtimeSinceStartupAsDouble >= deadline) cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            GameController controller = ServiceLocator.Get<GameController>();
            if (controller == null) throw new InvalidOperationException("Gameplay controller is missing.");
            BeginStage("PlayUI");
            playId = GF.UI.OpenUIForm(UIViews.PlayUIForm);
            await WaitForFormAsync(playId, token);
            EndStage();
            if (Time.realtimeSinceStartupAsDouble >= deadline) cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            BeginStage("ExposeGameplay");
            HomePageUIForm.CloseHallTabAfterGameplayLoaded();
            ArrowUITopbar.SetCoinVisible(false);
            GF.UI.CloseUIForm(loadingId);
            loadingId = -1;
            controller.ExposeGameplay();
            success = true;
            SceneHandler.FinishGameplayLoading(true);
            EndStage();
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
            Log.Info($"[GameplayLoad] Result success={success}, total={(Time.realtimeSinceStartupAsDouble - loadStarted) * 1000:F0}ms, lastStage={stage}, stageElapsed={(Time.realtimeSinceStartupAsDouble - stageStarted) * 1000:F0}ms");
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

    private static async UniTask PrepareSceneAsync(string sceneAsset, CancellationToken token, Action<string> beginStage, Action endStage)
    {
        bool reuseStartup = startupLevel == GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId
            && startupAssets != null && startupActiveRoots != null && GF.Scene.SceneIsLoaded(sceneAsset);
        if (reuseStartup)
        {
            beginStage("ReuseStartupScene");
            foreach (var root in startupActiveRoots)
                if (root != null) root.SetActive(true);
            startupActiveRoots = null;
            Log.Info($"[GameplayLoad] Reusing startup scene/resources: level={startupLevel}");
            endStage();
        }
        else
        {
            ReleaseStartupAssets();
            startupActiveRoots = null;
        }
        if (!reuseStartup && GF.Scene.SceneIsLoaded(sceneAsset))
        {
            beginStage("UnloadPreviousScene");
            if (!await GF.Scene.UnLoadSceneAwait(sceneAsset))
                throw new InvalidOperationException("Could not unload the previous level.");
            endStage();
        }
        token.ThrowIfCancellationRequested();
        if (!reuseStartup)
        {
            beginStage("LoadScene");
            if (!await GF.Scene.LoadSceneAwait(sceneAsset))
                throw new InvalidOperationException("Could not load the gameplay scene.");
            endStage();
        }
        token.ThrowIfCancellationRequested();
        GameController controller = ServiceLocator.Get<GameController>();
        if (controller == null) throw new InvalidOperationException("Gameplay controller is missing.");
        beginStage("PrepareGameplay");
        try
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, controller.GetCancellationTokenOnDestroy()))
                await controller.PrepareGameplayAsync(linked.Token);
            endStage();
        }
        finally
        {
            // The controller's lease now owns the resources, or normal failure
            // cleanup will unload the scene. Do not pin startup references.
            ReleaseStartupAssets();
        }
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
            ReleaseStartupAssets();
            startupActiveRoots = null;
            HideGameplayScene();
            if (GF.Scene.SceneIsLoaded(sceneAsset)) await GF.Scene.UnLoadSceneAwait(sceneAsset);
        }
        catch (Exception exception) { Log.Error(exception.ToString()); }
        finally { IsLoading = false; }
    }
}
