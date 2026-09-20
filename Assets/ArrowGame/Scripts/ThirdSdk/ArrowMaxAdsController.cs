
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class ArrowMaxAdsController
{
    // private static FruitsMaxAdsController s_Instance;
#if UNITY_IOS
    private const string InterstitialAdUnitId = "43b5de7861adb733";
    private const string RewardedAdUnitId = "244c925e3b7a89f1";
    private const string BannerAdUnitId = "8b7add738779cc2d";
    private const string AppOpenAdUnitId = "d7b399fc94af3a0e";
    private const string MRecAdUnitId = "ENTER_IOS_MREC_AD_UNIT_ID_HERE";
#else // UNITY_ANDROID
    private const string InterstitialAdUnitId = "f2c0a5c7643edc94";
    private const string RewardedAdUnitId = "a7f1d4a4183b8688";
    private const string BannerAdUnitId = "21dc286c1479038d";
    private const string AppOpenAdUnitId = "f069c8cd35607d2b";
    private const string MRecAdUnitId = "ENTER_ANDROID_MREC_AD_UNIT_ID_HERE";
#endif
    private bool isBannerShowing;
    private bool isMRecShowing;

    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;
    /// <summary>
    /// 插屏广告收益
    /// </summary>
    private double interstitialAdInfo;
    /// <summary>
    /// 激励视频广告收益
    /// </summary>
    private double rewardedAdInfo;
    /// <summary>
    /// 广告来源
    /// </summary>
    private string adsource;

    public ArrowMaxAdsController()
    {
        #if UNITY_EDITOR
        return;
        #endif
        InitializeSDK();
    }

    public void InitializeSDK()
    {
        interstitialAdInfo = -1;
        rewardedAdInfo = -1;
        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            // AppLovin SDK is initialized, configure and start loading ads.
            Log.Info("MAX SDK Initialized");
            // MaxSdk.SetVerboseLogging(true);
            InitializeInterstitialAds();
            InitializeRewardedAds();
            // InitializeBannerAds();
            // InitializeMRecAds();
        };

        MaxSdk.InitializeSdk();
        
        // var playerDataModel = GF.DataModel.GetDataModel<PlayerDataModel>();
        // if (playerDataModel != null && playerDataModel.LevelId > 1)
        // {
        //     GF.Timer.AddTimer("MaxAutoInterstitial", 180f, AutoInterstitial);
        // }

    }
    private void AutoInterstitial()
    {
        string interstitialId = RewardSourceConst.AutoInterstitial;
        ShowInterstitial(interstitialId);
    }
    public void ShowMaxDebugger()
    {
        MaxSdk.ShowMediationDebugger();
    }

    #region Interstitial Ad Methods
    private void InitializeInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;

        // Load the first interstitial
        LoadInterstitial();
    }

    public void LoadInterstitial()
    {
        Log.Info("max Interstitial Loading...");
        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
    }

    public void ShowInterstitial(string adsource)
    {
        this.adsource = adsource;
        if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
        {
            Log.Info("max Interstitial Showing...");
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
        }
        else
        {
            LoadInterstitial();
            Log.Info("Interstitial Ad not ready...");
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
        Log.Info("Interstitial loaded");
        interstitialAdInfo = adInfo.Revenue;
        // Reset retry attempt
        interstitialRetryAttempt = 0;
        // GF.Event.Fire(this,  InterstitialAdLoadedEventArgs.Create(AdType.Max, adInfo.Revenue * 1000f));

    }

    private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
        {
            return;
        }
        interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));
        
        // DOVirtual.DelayedCall((float)retryDelay, () => LoadInterstitial());
        GF.Timer.AddTimer("LoadMaxInterAD", (float)retryDelay, () =>
        {
            LoadInterstitial();
            GF.Timer.RemoveTimer("LoadMaxInterAD");
        });
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Log.Info("Interstitial displayed");
        string adPositionType = adsource.Contains("_") ? adsource.Split('_')[0] : adsource;
        // 数据上报
        // DataCollectManager.Instance.LogEvent("Ad_Show", new Dictionary<string, object> {
        //     {"ad_type", "interstitial"},
        //     {"ad_position", adsource},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", adInfo.Placement}
        // });
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display);

    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Log.Info("Interstitial clicked");
        string adPositionType = adsource.Contains("_") ? adsource.Split('_')[0] : adsource;
        // 数据上报
        // DataCollectManager.Instance.LogEvent("Ad_Click", new Dictionary<string, object> {
        //     {"ad_type", "interstitial"},
        //     {"ad_position", adsource},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", adInfo.Placement}
        // });
    }

    private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad failed to display. We recommend loading the next ad
        Log.Info("Interstitial failed to display with error code: " + errorInfo.Code);
        rewardedCallBack?.Invoke(false);
        rewardedCallBack = null;
        LoadInterstitial();
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_failed);

    }

    private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is hidden. Pre-load the next ad
        interstitialAdInfo = -1;
        rewardedCallBack?.Invoke(true);
        rewardedCallBack = null;
        Log.Info("Interstitial dismissed");
        LoadInterstitial();
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_succeeded);

    }

    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad revenue paid. Use this callback to track user revenue.
        Log.Info("Interstitial revenue paid");
        CommonHelper.LogEvent(AdjustEventCodeEvent.interstitial_ad_times);

        
        // Ad revenue
        // double revenue = adInfo.Revenue;

        // Miscellaneous data
        // string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        // string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        // string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        // string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
        //
        // Dictionary<string, object> parameters = new Dictionary<string, object>();
        // parameters["countrycode"] = MaxSdk.GetSdkConfiguration().CountryCode;
        // parameters["revenue"] = adInfo.Revenue;
        // parameters["networkname"] = adInfo.NetworkName;
        // parameters["adunitid"] = adInfo.AdUnitIdentifier;
        // parameters["adformat"] = adInfo.AdFormat;
        // parameters["adsource"] = adsource;
        // // 数据上报
        // DataCollectManager.Instance.LogAdRevenue(parameters);
        
        // AdjustManager.Instance.LogAdRevenue(adInfo.Revenue, MaxSdk.GetSdkConfiguration().CountryCode);
        
        TrackAdRevenue(adInfo);
        
        adsource = string.Empty;
    }

    #endregion

    #region Rewarded Ad Methods

    private void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

        // Load the first RewardedAd
        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        Log.Info("Rewarded Loading...");
        MaxSdk.LoadRewardedAd(RewardedAdUnitId);
    }

    // 视频广告回调
    private Action<bool> rewardedCallBack;
    public void ShowRewardedAd(Action<bool> callback, string adsource)
    {
        this.adsource = adsource;
        rewardedCallBack = callback;
        Log.Info("请求播放max激励视频广告");
        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
        {
            if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId) && rewardedAdInfo != -1 && interstitialAdInfo != -1 && interstitialAdInfo > rewardedAdInfo)
            {
                Log.Info($"插屏比激励视频收益高，优先展示插屏，插屏收益：{interstitialAdInfo}，激励视频收益：{rewardedAdInfo}");
                MaxSdk.ShowInterstitial(InterstitialAdUnitId);
                return;
            }
            Log.Info("Max Rewarded Showing...");
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
        }
        else
        {
            Log.Info("Rewarded Ad not ready");
            rewardedCallBack?.Invoke(false);
            rewardedCallBack = null;
        }
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
        Log.Info("Rewarded ad loaded");
        rewardedAdInfo = adInfo.Revenue;
        // Reset retry attempt
        rewardedRetryAttempt = 0;

        // 广告加载完成，开始竞价...
        // GF.Event.Fire(this,  AdLoadedEventArgs.Create(AdType.Max, adInfo.Revenue * 1000f));
    }

    private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));

        Log.Info("Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");
        Log.Info("Rewarded ad failed to load with error code: " + errorInfo.Code);
        rewardedCallBack?.Invoke(false);
        rewardedCallBack = null;
        // GF.Event.Fire(this,  AdLoadedEventArgs.Create(AdType.Max, -1));

        // DOVirtual.DelayedCall((float)retryDelay, () => LoadRewardedAd());
        GF.Timer.AddTimer("LoadMaxRewardAD", (float)retryDelay, () =>
        {
            LoadRewardedAd();
            GF.Timer.RemoveTimer("LoadMaxRewardAD");
        });
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. We recommend loading the next ad
        Log.Info("Rewarded ad failed to display with error code: " + errorInfo.Code);
        rewardedCallBack?.Invoke(false);
        rewardedCallBack = null;
        LoadRewardedAd();
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_failed);

    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Log.Info("Rewarded ad displayed");
        string adPositionType = adsource.Contains("_") ? adsource.Split('_')[0] : adsource;
        // 数据上报
        // DataCollectManager.Instance.LogEvent("Ad_Show", new Dictionary<string, object> {
        //     {"ad_type", "reward"},
        //     {"ad_position", adsource},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", adInfo.Placement}
        // });
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display);

    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Log.Info("Rewarded ad clicked");
        string adPositionType = adsource.Contains("_") ? adsource.Split('_')[0] : adsource;
        // 数据上报
        // DataCollectManager.Instance.LogEvent("Ad_Click", new Dictionary<string, object> {
        //     {"ad_type", "interstitial"},
        //     {"ad_position", adsource},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", adInfo.Placement}
        // });
    }

    private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        Log.Info("Rewarded ad dismissed");
        rewardedAdInfo = -1;
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad was displayed and user should receive the reward
        Log.Info("Rewarded ad received reward " + reward);

        rewardedCallBack?.Invoke(true);
        rewardedCallBack = null;
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_succeeded);

    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad revenue paid. Use this callback to track user revenue.
        Log.Info("Rewarded ad revenue paid");

        // Dictionary<string, object> parameters = new Dictionary<string, object>();
        // parameters["countrycode"] = MaxSdk.GetSdkConfiguration().CountryCode;
        // parameters["revenue"] = adInfo.Revenue;
        // parameters["networkname"] = adInfo.NetworkName;
        // parameters["adunitid"] = adInfo.AdUnitIdentifier;
        // parameters["adformat"] = adInfo.AdFormat;
        // parameters["adsource"] = adsource;
        // // 数据上报
        // DataCollectManager.Instance.LogAdRevenue(parameters);
        
        // AdjustManager.Instance.LogAdRevenue(adInfo.Revenue, MaxSdk.GetSdkConfiguration().CountryCode);
        CommonHelper.LogEvent(AdjustEventCodeEvent.video_ad_times);

        TrackAdRevenue(adInfo);
        adsource = string.Empty;
    }

    #endregion

    #region Banner Ad Methods

    private void InitializeBannerAds()
    {
        // Attach Callbacks
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
        MaxSdk.CreateBanner(BannerAdUnitId, new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.TopCenter));

        // Set background or background color for banners to be fully functional.
        MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
    }

    private void ToggleBannerVisibility()
    {
        if (!isBannerShowing)
        {
            MaxSdk.ShowBanner(BannerAdUnitId);
            Log.Info("Hide Banner");
        }
        else
        {
            MaxSdk.HideBanner(BannerAdUnitId);
            Log.Info("Show Banner");
        }

        isBannerShowing = !isBannerShowing;
    }

    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Banner ad is ready to be shown.
        // If you have already called MaxSdk.ShowBanner(BannerAdUnitId) it will automatically be shown on the next ad refresh.
        Log.Info("Banner ad loaded");
    }

    private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Banner ad failed to load. MAX will automatically try loading a new ad internally.
        Log.Info("Banner ad failed to load with error code: " + errorInfo.Code);
    }

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Log.Info("Banner ad clicked");
    }

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Banner ad revenue paid. Use this callback to track user revenue.
        Log.Info("Banner ad revenue paid");

        // Ad revenue
        // double revenue = adInfo.Revenue;

        // Miscellaneous data
        // string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        // string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        // string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        // string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

        TrackAdRevenue(adInfo);
    }

    #endregion

    #region MREC Ad Methods

    private void InitializeMRecAds()
    {
        // Attach Callbacks
        MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
        MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdFailedEvent;
        MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;

        // MRECs are automatically sized to 300x250.
        MaxSdk.CreateMRec(MRecAdUnitId, new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter));
    }

    private void ToggleMRecVisibility()
    {
        if (!isMRecShowing)
        {
            MaxSdk.ShowMRec(MRecAdUnitId);
            Log.Info("Hide MREC");
        }
        else
        {
            MaxSdk.HideMRec(MRecAdUnitId);
            Log.Info("Show MREC");
        }

        isMRecShowing = !isMRecShowing;
    }

    private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // MRec ad is ready to be shown.
        // If you have already called MaxSdk.ShowMRec(MRecAdUnitId) it will automatically be shown on the next MRec refresh.
        Log.Info("MRec ad loaded");
    }

    private void OnMRecAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // MRec ad failed to load. MAX will automatically try loading a new ad internally.
        Log.Info("MRec ad failed to load with error code: " + errorInfo.Code);
    }

    private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Log.Info("MRec ad clicked");
    }

    private void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // MRec ad revenue paid. Use this callback to track user revenue.
        Log.Info("MRec ad revenue paid");

        // Ad revenue
        double revenue = adInfo.Revenue;

        // Miscellaneous data
        string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

        // TrackAdRevenue(adInfo);
    }

    #endregion

    #region App Open Ad Methods
    public void ShowAdIfReady()
    {
        if (MaxSdk.IsAppOpenAdReady(AppOpenAdUnitId))
        {
            MaxSdk.ShowAppOpenAd(AppOpenAdUnitId);
        }
        else
        {
            MaxSdk.LoadAppOpenAd(AppOpenAdUnitId);
        }
    }
    #endregion

    private void TrackAdRevenue(MaxSdkBase.AdInfo adInfo)
    {
        // AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAppLovinMAX);
        //
        // adjustAdRevenue.setRevenue(adInfo.Revenue, "USD");
        // adjustAdRevenue.setAdRevenueNetwork(adInfo.NetworkName);
        // adjustAdRevenue.setAdRevenueUnit(adInfo.AdUnitIdentifier);
        // adjustAdRevenue.setAdRevenuePlacement(adInfo.Placement);
        //
        // Adjust.trackAdRevenue(adjustAdRevenue);
        
        
        AdjustManager.Instance.LogAdRevenue(adInfo.Revenue, "USD");
    }
}
/// <summary>
/// 广告展示数据结构（用于序列化）
/// </summary>
[System.Serializable]
public class ImpressionData
{
    public string creative_id;
    public string placement;
    public string format;
    public string country;
    public string ad_revenue_currency;
    public string network_placement;
    public string revenue_precision;
    public string ad_unit_id;
    public double revenue;
    public string network_name;
}
