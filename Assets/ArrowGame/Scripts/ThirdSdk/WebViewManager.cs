using System;
using AdjustSdk;
using UnityEngine;
using UnityGameFramework.Runtime;

public class WebViewManager
{
    private const string ManagedWebViewClassName =
        "com.example.webviewmanagermylibrary.mobilv.ManagedWebView";

    private const string LogClassName =
        "com.example.webviewmanagermylibrary.mobilv.Log";

    private static WebViewManager m_Instance;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject m_ManagedWebView;
#endif

    public bool IsInitialized { get; private set; }

    public static WebViewManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new WebViewManager();
            }

            return m_Instance;
        }
    }

    private WebViewManager()
    {
    }

    public void CreateH5()
    {
        Initialize();

        /*Adjust.GetGoogleAdId(gaid =>
        {
            Log.Info($"获取GAID: {gaid}");
            if (IsValidAdvertisingId(gaid))
            {
                Start("https://x.gamedevs.pro", Application.identifier, gaid);
                CommonHelper.LogEvent(AdjustEventCodeEvent.H5_loading);
                return;
            }

            string androidId = CommonHelper.GetAndroidId();
            Log.Info($"GAID未返回，兜底获取AndroidId: {androidId}");
            if (!string.IsNullOrWhiteSpace(androidId))
            {
                CommonHelper.LogEvent(AdjustEventCodeEvent.H5_loading);
                Start("https://x.gamedevs.pro", Application.identifier, androidId);

            }
        });*/
        
        string androidId = CommonHelper.GetAndroidId();
        if (!string.IsNullOrWhiteSpace(androidId))
        {
            Log.Info($"获取AndroidId: {androidId}");

            CommonHelper.LogEvent(AdjustEventCodeEvent.H5_loading);
            Start("https://x.gamedevs.pro", Application.identifier, androidId);

        }

        SetCreationEnabled(true);
        // SetLogEnabled(true);
    }

    private static bool IsValidAdvertisingId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
               id != "00000000-0000-0000-0000-000000000000";
    }

    /// <summary>
    /// 使用 UnityPlayer.currentActivity 创建 mobilvweb 中的 ManagedWebView。
    /// 可重复调用，已初始化时不会重复创建。
    /// </summary>
    public void Initialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        RunOnUiThread(activity => EnsureInitialized(activity));
#else
        IsInitialized = true;
#endif
    }

    /// <summary>
    /// 初始化并使用三个配置参数启动 ManagedWebView。
    /// </summary>
    public void Start(string url, string packageName, string gaid)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            string.IsNullOrWhiteSpace(packageName) ||
            string.IsNullOrWhiteSpace(gaid))
        {
            Log.Error("WebViewManager Start failed: start parameters cannot be empty.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        RunOnUiThread(activity =>
        {
            if (EnsureInitialized(activity))
            {
                m_ManagedWebView.Call("start", url, packageName, gaid);
                Log.Info("WebViewManager ManagedWebView started.");
            }
        });
#else
        Log.Info("WebViewManager Start is only available on an Android device.");
#endif
    }

    public void SetCreationEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        RunOnUiThread(activity =>
        {
            if (EnsureInitialized(activity))
            {
                m_ManagedWebView.Call("setCreationEnabled", enabled);
            }
        });
#endif
    }

    public bool IsCreationEnabled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (m_ManagedWebView == null)
        {
            return false;
        }

        try
        {
            return m_ManagedWebView.Call<bool>("isCreationEnabled");
        }
        catch (Exception exception)
        {
            Log.Error("WebViewManager isCreationEnabled failed: {0}", exception);
            return false;
        }
#else
        return false;
#endif
    }

    /*public void SetLogEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var logClass = new AndroidJavaClass(LogClassName))
            {
                logClass.CallStatic("setEnabled", enabled);
            }
        }
        catch (Exception exception)
        {
            Log.Error("WebViewManager Log.setEnabled failed: {0}", exception);
        }
#endif
    }*/

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        RunOnUiThread(_ =>
        {
            if (m_ManagedWebView == null)
            {
                return;
            }

            m_ManagedWebView.Call("stop");
            m_ManagedWebView.Dispose();
            m_ManagedWebView = null;
            IsInitialized = false;
            Log.Info("WebViewManager ManagedWebView stopped.");
        });
#else
        IsInitialized = false;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool EnsureInitialized(AndroidJavaObject activity)
    {
        if (m_ManagedWebView != null)
        {
            return true;
        }

        try
        {
            m_ManagedWebView = new AndroidJavaObject(ManagedWebViewClassName, activity);
            IsInitialized = true;
            Log.Info("WebViewManager ManagedWebView initialized.");
            return true;
        }
        catch (Exception exception)
        {
            IsInitialized = false;
            Log.Error("WebViewManager ManagedWebView initialization failed: {0}", exception);
            return false;
        }
    }

    private static void RunOnUiThread(Action<AndroidJavaObject> action)
    {
        AndroidJavaObject activity = null;
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            if (activity == null)
            {
                Log.Error("WebViewManager failed: UnityPlayer.currentActivity is null.");
                return;
            }

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    action(activity);
                }
                catch (Exception exception)
                {
                    Log.Error("WebViewManager Android call failed: {0}", exception);
                }
                finally
                {
                    activity.Dispose();
                }
            }));
        }
        catch (Exception exception)
        {
            activity?.Dispose();
            Log.Error("WebViewManager could not run on Android UI thread: {0}", exception);
        }
    }
#endif
}
