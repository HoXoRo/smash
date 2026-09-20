using System;
using System.Collections.Generic;
using AdjustSdk;
using UnityGameFramework.Runtime;
using UnityEngine;
public class AdjustManager
{
    private static AdjustManager m_Instance;
    private bool isAttributionCallBack = false;
    private bool m_HasRequestedAttribution = false;

    private const string appToken = "akcoyxhe9g5c";

    public static AdjustManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new AdjustManager();
            }
            return m_Instance;
        }
    }

    public AdjustManager()
    {
        InitSdk();
    }

    private void InitSdk()
    {
        AdjustConfig adjustConfig = new AdjustConfig(appToken, AdjustEnvironment.Production);
        adjustConfig.LogLevel = AdjustLogLevel.Suppress;

        Adjust.InitSdk(adjustConfig);
        isAttributionCallBack = false;
    }

    // 上报事件
    public void LogEvent(string eventName, Dictionary<string, string> parameters = null)
    {
        AdjustEvent adjustEvent = new AdjustEvent(eventName);
        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                adjustEvent.AddCallbackParameter(kvp.Key, kvp.Value);
            }
        }

        Adjust.TrackEvent(adjustEvent);
    }

    public void LogAdRevenue(double amount, string currency)
    {
        AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue("admob_sdk");
        adjustAdRevenue.SetRevenue(amount, currency);
        Log.Info("广告价值上报：value: " + amount + " currency: " + currency);
        Adjust.TrackAdRevenue(adjustAdRevenue);
    }

    public void GetAttribution()
    {
        if (m_HasRequestedAttribution || isAttributionCallBack)
        {
            return;
        }

        m_HasRequestedAttribution = true;
        Log.Info("AdjustManager GetAttribution SpecStatus={0}", CommonHelper.GetSpecStatus());

        // 强制 A/B 模式由配表直接决定用户状态，不需要向 Adjust 拉取归因。
        if (!CommonHelper.ShouldUseAttribution())
        {
            MarkAttributionComplete();
            return;
        }

        Log.Info($"归因数据是否已获取 CommonHelper.IsBorATest: {CommonHelper.IsSpec()} CommonHelper.IsRecognition(){CommonHelper.IsRecognition()}");
        if (CommonHelper.IsRecognition())
        {
            MarkAttributionComplete();
            return;
        }
        Adjust.GetGoogleAdId(gaid =>
        {
            Log.Info($"获取归因数据前获取GAID: {gaid}");
        });
        Log.Info($"开始获取归因数据>>>>");
        GF.Timer.AddTimer("GetAttributionTimeOut", 30, () =>
        {
            Log.Info($"获取归因数据超时>>>> 将用户设置为自然量用户");
            CommonHelper.UserType = 0;
            CommonHelper.SetUserType(0, false);
            CommonHelper.LogEvent(AdjustEventCodeEvent.a_enter);
            MarkAttributionComplete();
            CommonHelper.LogEvent(AdjustEventCodeEvent.attribution_timeout);
            GF.Timer.RemoveTimer("GetAttributionTimeOut");
        });
        Adjust.GetAttribution(attribution =>
        {
            // 超时后按自然量完成启动，忽略迟到的归因回调，避免重复启动和覆盖用户状态。
            if (isAttributionCallBack)
            {
                return;
            }

            // 判断是否为自然量用户
            bool isOrganic = string.IsNullOrEmpty(attribution.Network) || "organic".Equals(attribution.Network.ToLower());
            if (!isOrganic)
            {
                CommonHelper.UserType = 1;
                CommonHelper.SetUserType(1, true);
                CommonHelper.LogEvent(AdjustEventCodeEvent.b_enter);
                CommonHelper.LogEvent(AdjustEventCodeEvent.b_enter_attribution);
            }
            else
            {
                CommonHelper.UserType = 0;
                CommonHelper.SetUserType(0, true);
                CommonHelper.LogEvent(AdjustEventCodeEvent.a_enter);
                CommonHelper.LogEvent(AdjustEventCodeEvent.a_enter_attribution);
            }
            MarkAttributionComplete();
            GF.Timer.RemoveTimer("GetAttributionTimeOut");
            Log.Info($"归因数据返回>>>>{GF.AdjustManager.CheckAttributionCallBack()},CommonHelper.UserType: {CommonHelper.UserType},CommonHelper.IsBorATest: {CommonHelper.IsSpec()} CommonHelper.IsRecognition(){CommonHelper.IsRecognition()},attribution: {attribution.ToString()}");
        });
    }

    private void MarkAttributionComplete()
    {
        isAttributionCallBack = true;
#if !UNITY_EDITOR
        Log.Info($"判断是否开启h5");
        StartH5(CommonHelper.UserType == 1);
#endif
    }
    public bool CheckAttributionCallBack()
    {
#if UNITY_EDITOR
        return true;
#else
        return isAttributionCallBack;
#endif
    }
    public void StartH5(bool isBorATest)
    {
        Log.Info($"判断是否开启h5: {isBorATest}");

        if (isBorATest)
        {
            StartH5Immediately();
        }
    }
    private void StartH5Immediately()
    {
        Log.Info("H5启动");
        WebViewManager.Instance.CreateH5();
        Log.Info("H5启动完成");
    }
    private void StartH5Delayed(int delay)
    {
        Log.Info("A面H5延迟启动>>>> 延迟{0}秒", delay*60);
        GF.Timer.AddTimer("StartH5Delayed", delay * 60, () =>
        {
            StartH5Immediately();
            GF.Timer.RemoveTimer("StartH5Delayed");
        });
    }
}
