using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

public enum RequestFailure { Timeout, Failed }

/// <summary>A request handle. Call Send and Cancel on Unity's main thread.</summary>
public sealed class RequestHandle
{
    public bool IsCompleted { get; private set; }
    public bool IsCancelled { get; private set; }
    internal Action ReleaseLoading;

    public void Cancel()
    {
        if (IsCompleted) return;
        IsCancelled = true;
        Complete();
    }

    internal void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        Action release = ReleaseLoading;
        ReleaseLoading = null;
        release?.Invoke();
    }
}

/// <summary>Simulates a response using device connectivity, not server reachability.</summary>
public static class SimulatedRequestHelper
{
    private static int loadingUsers;
    private static int loadingId = -1;

    public static RequestHandle Send(Action onSuccess, Action<RequestFailure> onFailure,
        bool showLoading = true, float timeoutSeconds = 10f, float simulatedDelaySeconds = 0.8f)
    {
        if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        if (float.IsNaN(simulatedDelaySeconds) || float.IsInfinity(simulatedDelaySeconds) || simulatedDelaySeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(simulatedDelaySeconds));
        var handle = new RequestHandle();
        RunAsync(handle, onSuccess, onFailure, showLoading, timeoutSeconds, simulatedDelaySeconds).Forget();
        return handle;
    }

    private static async UniTask RunAsync(RequestHandle handle, Action onSuccess,
        Action<RequestFailure> onFailure, bool showLoading, float timeoutSeconds, float delaySeconds)
    {
        RequestFailure? failure = null;
        double startedAt = Time.realtimeSinceStartupAsDouble;
        try
        {
            if (showLoading)
            {
                AcquireLoading();
                handle.ReleaseLoading = ReleaseLoading;
            }
            // Defer completion so the caller can store the handle first.
            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (handle.IsCancelled) return;
                double elapsed = Time.realtimeSinceStartupAsDouble - startedAt;
                if (elapsed >= timeoutSeconds)
                {
                    failure = RequestFailure.Timeout;
                    break;
                }
                if (showLoading && !GF.UI.IsLoadingUIForm(loadingId) && !GF.UI.HasUIForm(loadingId))
                    throw new InvalidOperationException("Simulated request loading UI is unavailable.");
                bool loadingReady = !showLoading || GF.UI.HasUIForm(loadingId);
                if (loadingReady && elapsed >= delaySeconds && UtilityEx.CheckNetwork()) break;
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception.ToString());
            failure = RequestFailure.Failed;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        finally
        {
            handle.Complete();
        }
        if (handle.IsCancelled) return;
        // Callback exceptions must not trigger a second result callback.
        if (failure.HasValue) onFailure?.Invoke(failure.Value);
        else onSuccess?.Invoke();
    }

    private static void AcquireLoading()
    {
        if (loadingUsers == 0)
        {
            var parameters = UIParams.Create(allowEscape: false);
            parameters.Set<VarBoolean>(LoadingUIForm.P_ManualClose, true);
            loadingId = GF.UI.OpenUIForm(UIViews.LoadingUIForm, parameters);
            if (loadingId < 0) throw new InvalidOperationException("Could not open simulated request loading UI.");
        }
        loadingUsers++;
    }

    private static void ReleaseLoading()
    {
        if (--loadingUsers > 0) return;
        int id = loadingId;
        loadingId = -1;
        // Closing a pending form by serial ID also cancels its eventual opening.
        if (id >= 0 && GF.UI != null && (GF.UI.HasUIForm(id) || GF.UI.IsLoadingUIForm(id)))
            GF.UI.CloseUIForm(id);
    }
}
