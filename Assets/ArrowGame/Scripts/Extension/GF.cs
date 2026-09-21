using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class GF : GFBuiltin
{
    public static DataModelComponent DataModel { get; private set; }
    public static VariablePoolComponent VariablePool { get; private set; }
    public static StaticUIComponent StaticUI { get; private set; }
    public static TimerManager Timer { get; private set; }
    // public static DataCollectManager DataCollectManager { get; private set; }
    public static GuideManager GuideManager { get; private set; }
    
    public static AdjustManager AdjustManager { get; private set; }
    public static AdsManager AdsManager { get; private set; }
    private bool m_IsFrameworkInitialized = false;
    // public static ITenjinBridge TenjinBridge { get; private set; }
    public static string apiKey = "abcd";
    bool isStartURSdk = false; // 是否已经启动URSdk


    private void Awake()
    {
        Timer = TimerManager.Instance;
    }

    private void Start()
    {
        DataModel = GameEntry.GetComponent<DataModelComponent>();
        StaticUI = GameEntry.GetComponent<StaticUIComponent>();
        VariablePool = GameEntry.GetComponent<VariablePoolComponent>();
        DOTween.SetTweensCapacity(1000, 100);
        // 订阅热更新完成事件
        Event.Subscribe(GFEventArgs.EventId, OnGFEventCallback);
        isStartURSdk = false;
        // 第三方sdk
        // DataCollectManager = DataCollectManager.Instance;
        // TenjinConnect();
        AdjustManager = AdjustManager.Instance;
        AdsManager = AdsManager.Instance;
        
        // CommonHelper.LogEvent(AdjustEventCodeEvent.background_return);
    }

    private void OnGFEventCallback(object sender, GameEventArgs e)
    {
        if (m_IsFrameworkInitialized) return;
        var args = e as GFEventArgs;
        if (args.EventType == GFEventType.AppOpenMenu)
        {
            Log.Info("AppOpenMenu");
            // 确保所有必要的组件都已初始化
            if (DataModel == null || StaticUI == null || VariablePool == null)
            {
                Log.Error("Framework components not fully initialized!");
                return;
            }
            // if (!DataCollectManager.IsInit())
            // {
            //     DataCollectManager.Init();
            // }
            Log.Info("初始化自定义管理器Custom managers initialized after framework ready.");
            // 初始化自定义管理器
            // FirebaseMseeageManage.Instance.LogEvent("login");
            GuideManager = GuideManager.Instance;
            // taskManager = new TaskManager();
            // LevelDataManager = LevelDataManager.Instance;
            m_IsFrameworkInitialized = true;
        }

    }

    private void Update()
    {
        // if (m_IsFrameworkInitialized)
        // {
            // 更新计时器
            TimerManager.Instance.Update();
        // }
    }

    private void OnApplicationQuit()
    {
        Log.Info("OnApplicationQuit");
        OnExitGame();
    }

    private void OnApplicationPause(bool pause)
    {
        Log.Info($"OnApplicationPause Application.isMobilePlatform{Application.isMobilePlatform},pauseXXX:{pause}");
        if (Application.isMobilePlatform && pause)
        {
            OnExitGame(pause);
        }
    }
    public Vector2 GetCanvasSize()
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        return rect.sizeDelta;
    }

    public Vector2 World2ScreenPoint(Camera cam, Vector3 worldPoint)
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        Vector2 sPoint = cam.WorldToViewportPoint(worldPoint) * rect.sizeDelta;
        return sPoint - rect.sizeDelta * 0.5f;
    }

    private void OnExitGame(bool pause = false)
    {
        var playerDataModel = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerDataModel != null)
        {
            playerDataModel.Save(true);
        }
        if (pause)
        {
            Timer.PauseTimer("MaxAutoInterstitial");
        }
        else
        {
            Timer.ResumeTimer("MaxAutoInterstitial");
        }
        GF.Event.FireNow(this, GFEventArgs.Create(GFEventType.ApplicationQuit));
        var exit_time = DateTime.UtcNow.ToString();
        GF.Setting.SetString(ConstBuiltin.Setting.QuitAppTime, exit_time);
        GF.Setting.Save();
        Log.Info("Application Quit:{0}", exit_time);
        
        // CommonHelper.LogEvent(AdjustEventCodeEvent.background_enter);

    }

    // private const int MAX_TENJIN_RETRY_COUNT = 3;
    // private const float TENJIN_RETRY_INTERVAL = 2.5f; // 2.5秒重试间隔
    // private const string TENJIN_RETRY_TIMER_ID = "TenjinRetryTimer";
    // private int m_TenjinRetryCount = 0;
    // private bool m_TenjinAttributionReceived = false;

    // // 归因数据上报相关
    // private const int MAX_ATTRIBUTION_REPORT_COUNT = 5; // 最多上报5次
    // private const float ATTRIBUTION_REPORT_INTERVAL = 5f; // 每5秒上报一次
    // private const string ATTRIBUTION_REPORT_TIMER_ID = "AttributionReportTimer";
    // private int m_AttributionReportCount = 0;
    // private Dictionary<string, string> m_AttributionData = null;

//     public void TenjinConnect()
//     {
//         // 重置重试标记和上报计数
//         m_TenjinAttributionReceived = false;
//         m_AttributionReportCount = 0;
//         m_AttributionData = null;

//         // 清理旧的上报定时器
//         if (Timer != null)
//         {
//             Timer.RemoveTimer(ATTRIBUTION_REPORT_TIMER_ID);
//         }

//         // 获取TenjinBridge实例，如果不存在会自动创建
//         var tenjinBridge = TenjinBridgeProvider.GetOrCreateBridge();
//         TenjinBridge = tenjinBridge;
//         if (tenjinBridge == null)
//         {
//             Log.Error("===> TenjinBridge初始化失败");
//             return;
//         }

//         tenjinBridge.SetCacheEventSetting(apiKey, true);
// #if UNITY_IOS && !UNITY_EDITOR
//         if (new System.Version(UnityEngine.iOS.Device.systemVersion).CompareTo(new System.Version("14.0")) >= 0)
//         {
//             // Tenjin wrapper for requestTrackingAuthorization
//             tenjinBridge.RequestTrackingAuthorization(apiKey, (status) =>
//             {
//                 Log.Info("===> App Tracking Transparency Authorization Status: " + status);
//                 // Sends install/open event to Tenjin
//                 tenjinBridge.Connect(apiKey);
//                 // 延迟获取归因信息，确保Connect已执行
//                 TryGetAttributionInfo();
//             });
//         }
//         else
//         {
//             tenjinBridge.Connect(apiKey);
//             TryGetAttributionInfo();
//         }
// #else

//         // Sends install/open event to Tenjin
//         tenjinBridge.Connect(apiKey, null, true);
//         TryGetAttributionInfo();

// #endif
//         // tenjinBridge.DebugLogs(apiKey);
//     }

//     private void TryGetAttributionInfo()
//     {
//         Log.Info($"===> Tenjin尝试获取归因信息，重试次数: {m_TenjinRetryCount + 1}/{MAX_TENJIN_RETRY_COUNT}");

//         var tenjinBridge = TenjinBridgeProvider.Bridge;
//         if (tenjinBridge == null)
//         {
//             Log.Error("===> TenjinBridge未初始化，无法获取归因信息");
//             return;
//         }
//         tenjinBridge.SetAppStoreType(apiKey);
//         tenjinBridge.GetAttributionInfo(apiKey, (attributionInfo) =>
//         {

//             // 检查归因信息是否有效
//             bool isValid = attributionInfo != null && attributionInfo.ContainsKey("ad_network") && !string.IsNullOrEmpty(attributionInfo["ad_network"]);
//             if (isValid)
//             {
//                 // 成功获取到有效的归因信息
//                 m_TenjinAttributionReceived = true;
//                 // 移除重试定时器
//                 if (Timer != null)
//                 {
//                     Timer.RemoveTimer(TENJIN_RETRY_TIMER_ID);
//                 }

//                 // 保存归因数据
//                 m_AttributionData = attributionInfo;
//                 // 判断是否为自然量用户
//                 bool isOrganic = "organic".Equals(attributionInfo["ad_network"]);
//                 int userType = isOrganic ? 0 : 1; // 0=自然量, 1=非自然量
//                 Log.Info("===> attributionInfo.ad_network: " + attributionInfo["ad_network"] + " userType: " + userType);
//                 if (!isOrganic)
//                 {
//                     CommonHelper.UserType = 1;
//                     CommonHelper.SetUserType(1);
//                 }
//                 // 启动归因数据上报定时器
//                 StartAttributionReporting();
//             }
//             else
//             {
//                 // 归因信息无效或为空
//                 Log.Warning("===> Tenjin归因信息无效或为空");
//                 HandleTenjinRetry();
//             }
//         });
//     }

//     private void HandleTenjinRetry()
//     {
//         // 如果已经收到有效信息，不再重试
//         if (m_TenjinAttributionReceived)
//         {
//             return;
//         }

//         // 增加重试次数
//         m_TenjinRetryCount++;

//         if (m_TenjinRetryCount >= MAX_TENJIN_RETRY_COUNT)
//         {
//             Log.Warning($"===> Tenjin已达到最大重试次数({MAX_TENJIN_RETRY_COUNT})，停止重试");
//             // // 重试失败时，如果用户类型未识别，则不设置（保持默认值）
//             // var playerData = GF.DataModel?.GetDataModel<PlayerDataModel>();
//             // if (playerData != null && !playerData.IsRecognition)
//             // {
//             //     Log.Warning("===> 归因信息获取失败，用户类型保持默认值");
//             // }
//             return;
//         }

//         // 使用TimerManager设置重试定时器
//         if (Timer != null)
//         {
//             // 移除旧的定时器（如果存在）
//             Timer.RemoveTimer(TENJIN_RETRY_TIMER_ID);

//             // 添加新的定时器，在指定时间后重新Connect和获取归因信息
//             Timer.AddTimer(TENJIN_RETRY_TIMER_ID, TENJIN_RETRY_INTERVAL, () =>
//             {
//                 Log.Info($"===> Tenjin重试定时器触发，准备重新连接...");

//                 // 移除定时器，避免重复触发
//                 Timer.RemoveTimer(TENJIN_RETRY_TIMER_ID);

//                 // 重新调用Connect
//                 var tenjinBridge = TenjinBridgeProvider.Bridge;
//                 if (tenjinBridge != null)
//                 {
//                     tenjinBridge.Connect(apiKey);

//                     // 再次尝试获取归因信息
//                     TryGetAttributionInfo();
//                 }
//                 else
//                 {
//                     Log.Error("===> TenjinBridge未初始化，无法重试");
//                 }
//             });
//         }
//         else
//         {
//             Log.Error("===> TimerManager未初始化，无法进行Tenjin重试");
//         }
//     }

//     /// <summary>
//     /// 启动归因数据上报定时器
//     /// </summary>
//     private void StartAttributionReporting()
//     {
//         // 检查是否有归因数据
//         if (m_AttributionData == null || !m_AttributionData.ContainsKey("ad_network"))
//         {
//             Log.Warning("===> 没有归因数据，跳过上报");
//             return;
//         }

//         // 立即上报第一次
//         ReportAttributionData();

//         // 如果还没达到最大上报次数，设置定时器
//         if (m_AttributionReportCount < MAX_ATTRIBUTION_REPORT_COUNT && Timer != null)
//         {
//             Timer.RemoveTimer(ATTRIBUTION_REPORT_TIMER_ID);
//             Timer.AddTimer(ATTRIBUTION_REPORT_TIMER_ID, ATTRIBUTION_REPORT_INTERVAL, () =>
//             {
//                 ReportAttributionData();

//                 // 如果已经达到最大上报次数，移除定时器
//                 if (m_AttributionReportCount >= MAX_ATTRIBUTION_REPORT_COUNT)
//                 {
//                     Timer.RemoveTimer(ATTRIBUTION_REPORT_TIMER_ID);
//                     Log.Info($"===> 归因数据上报完成，共上报{m_AttributionReportCount}次");
//                 }
//             });
//         }
//     }
//     /// <summary>
//     /// 上报归因数据到TDAnalytics
//     /// </summary>
//     private void ReportAttributionData()
//     {
//         // 检查是否已达到最大上报次数
//         if (m_AttributionReportCount >= MAX_ATTRIBUTION_REPORT_COUNT)
//         {
//             return;
//         }

//         // 检查归因数据是否有效
//         if (m_AttributionData == null || !m_AttributionData.ContainsKey("ad_network"))
//         {
//             Log.Warning("===> 归因数据无效，跳过上报");
//             return;
//         }

//         // 增加上报计数
//         m_AttributionReportCount++;

//         // 提取归因数据字段，如果字段不存在则使用空字符串
//         string advertising_id = m_AttributionData.ContainsKey("advertising_id") ? m_AttributionData["advertising_id"] : "";
//         string ad_network = m_AttributionData.ContainsKey("ad_network") ? m_AttributionData["ad_network"] : "";
//         string campaign_id = m_AttributionData.ContainsKey("campaign_id") ? m_AttributionData["campaign_id"] : "";
//         string campaign_name = m_AttributionData.ContainsKey("campaign_name") ? m_AttributionData["campaign_name"] : "";
//         string site_id = m_AttributionData.ContainsKey("site_id") ? m_AttributionData["site_id"] : "";
//         string creative_name = m_AttributionData.ContainsKey("creative_name") ? m_AttributionData["creative_name"] : "";
//         string remote_campaign_id = m_AttributionData.ContainsKey("remote_campaign_id") ? m_AttributionData["remote_campaign_id"] : "";

//         Log.Info($"===> 开始上报归因数据到TDAnalytics (第{m_AttributionReportCount}次/{MAX_ATTRIBUTION_REPORT_COUNT}次)");
//         Log.Info($"===> ad_network: {ad_network}, campaign_name: {campaign_name}");

//         // 调用FirebaseMseeageManage的SendNetworkInfo方法上报
//         if (DataCollectManager != null)
//         {
//             DataCollectManager.SendNetworkInfo(
//                 advertising_id,
//                 ad_network,
//                 campaign_id,
//                 campaign_name,
//                 site_id,
//                 creative_name,
//                 remote_campaign_id
//             );
//             Log.Info($"===> 归因数据上报成功 (第{m_AttributionReportCount}次)");
//             bool isEmpty = string.IsNullOrEmpty(ad_network);
//             bool isOrganic = "organic".Equals(ad_network);
//             int userType = isOrganic ? 0 : 1; // 0=自然量, 1=非自然量
//             // 首次获取到归因数据时，启动URSdk
//             if (!isEmpty && !isStartURSdk && !string.IsNullOrEmpty(ad_network))
//             {
//                 isStartURSdk = true;
//                 if (userType == 1)
//                 {
//                     StartURSdkWithChannel(ad_network);
//                 }
//                 else
//                 {
//                     DataCollectManager.LogEvent("hf_shield");
//                 }
//             }
//         }
//         else
//         {
//             Log.Error("===> FirebaseMseeageManage未初始化，无法上报归因数据");
//         }
//     }
//     public static void SendAppLovinImpression(string impressionData)
//     {
//         if (TenjinBridge != null)
//         {
//             TenjinBridge.AppLovinImpressionFromJSON(apiKey, impressionData);
//             //Debug.Log($"===> TrackAdRevenue Tenjin AppLovinImpressionFromJSON: {impressionData}");
//         }
//     }

//     /// <summary>
//     /// 启动URSdk（在归因完成后调用）
//     /// </summary>
//     /// <param name="channel">归因回传参数的ad_network属性值</param>
//     private void StartURSdkWithChannel(string channel)
//     {
//         try
//         {
// #if UNITY_ANDROID && !UNITY_EDITOR
//             // 获取urSdkBridge实例并启动URSdk
//             var urSdkBridge = URSdkBridge.Instance;
//             if (urSdkBridge != null)
//             {
//                 urSdkBridge.StartURSdk(channel);
//                 Log.Info($"===> URSdk启动成功, channel: {channel}");
//             }
//             else
//             {
//                 Log.Error("===> URSdkBridge未初始化，无法启动URSdk");
//             }
// #else
//             Log.Info($"===> 非Android平台，跳过URSdk启动, channel: {channel}");
// #endif
//         }
//         catch (Exception e)
//         {
//             Log.Error($"===> 启动URSdk失败: {e.Message}");
//         }
//     }

}
