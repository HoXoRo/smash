

using System;
using DG.Tweening;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityGameFramework.Runtime;

public class InterstitialAdController
{
    /// <summary>
    /// UI element activated when an ad is ready to show.
    /// </summary>
    // public GameObject AdLoadedStatus;

    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private const string _adUnitId = "ca-app-pub-4506479419321713/6824479869";
#elif UNITY_IPHONE
    private const string _adUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
    private const string _adUnitId = "unused";
#endif

    private InterstitialAd _interstitialAd;
    private InterstitialCompletion pendingCompletion;
    
    private int interstitialRetryAttempt;

    public InterstitialAdController()
    {
        LoadAd();
    }

    /// <summary>
    /// Loads the ad.
    /// </summary>
    public void LoadAd()
    {
        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            DestroyAd();
        }

        Log.Info("Loading interstitial ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        InterstitialAd.Load(_adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                interstitialRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));
                
                GF.Timer.AddTimer("LoadInterstitialAD", (float)retryDelay, () =>
                {
                    LoadAd();
                    GF.Timer.RemoveTimer("LoadInterstitialAD");
                });
                
                Log.Error("Interstitial ad failed to load an ad with error : " + error);
                return;
            }
            // If the operation failed for unknown reasons.
            // This is an unexpected error, please report this bug if it happens.
            if (ad == null)
            {
                interstitialRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));
                GF.Timer.AddTimer("LoadInterstitialAD", (float)retryDelay, () =>
                {
                    LoadAd();
                    GF.Timer.RemoveTimer("LoadInterstitialAD");
                });                
                Log.Error("Unexpected error: Interstitial load event fired with null ad and null error.");
                return;
            }

            interstitialRetryAttempt = 0;
            // The operation completed successfully.
            Log.Info("Interstitial ad loaded with response : " + ad.GetResponseInfo());
            _interstitialAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlers(ad);

            // Inform the UI that the ad is ready.
            // AdLoadedStatus?.SetActive(true);
        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public bool ShowAd(Action onCompleted = null)
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Log.Info("admob Showing interstitial ad.");
            CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display);
            pendingCompletion = new InterstitialCompletion(onCompleted);
            _interstitialAd.Show();
            return true;
        }
        else
        {
            Log.Error("Interstitial ad is not ready yet.");
            return false;
        }

        // Inform the UI that the ad is not ready.
        // AdLoadedStatus?.SetActive(false);
    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyAd()
    {
        if (_interstitialAd != null)
        {
            Log.Info("Destroying interstitial ad.");
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        // Inform the UI that the ad is not ready.
        // AdLoadedStatus?.SetActive(false);
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogResponseInfo()
    {
        if (_interstitialAd != null)
        {
            var responseInfo = _interstitialAd.GetResponseInfo();
            UnityEngine.Debug.Log(responseInfo);
        }
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        InterstitialCompletion completion = null;
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += async (AdValue adValue) =>
        {
            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
            if (completion == null && ad == _interstitialAd) completion = pendingCompletion;
            Log.Info(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
            var revenue = adValue.Value / 1000000f;
            PcardGiftRewardSession.Add(revenue);
            AdjustManager.Instance.LogAdRevenue(revenue, adValue.CurrencyCode, "admob_sdk");
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Log.Info("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Log.Info("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Log.Info("Interstitial ad full screen content opened.");
            CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_succeeded);
            CommonHelper.LogEvent(AdjustEventCodeEvent.interstitial_ad_times);
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += async () =>
        {
            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
            if (completion == null && ad == _interstitialAd) completion = pendingCompletion;
            if (completion != null) await completion.Finish();
            Log.Info("Interstitial ad full screen content closed.");
            LoadAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += async (AdError error) =>
        {
            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
            if (completion == null && ad == _interstitialAd) completion = pendingCompletion;
            if (completion != null) await completion.Finish();
            CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_failed);
            Log.Error("Interstitial ad failed to open full screen content with error : "
                + error);
        };
    }
    
}