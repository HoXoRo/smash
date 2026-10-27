using System;
using DG.Tweening;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityGameFramework.Runtime;

public class RewardedAdController
{
    /// <summary>
    /// UI element activated when an ad is ready to show.
    /// </summary>
    // public GameObject AdLoadedStatus;

    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private const string _adUnitId = "ca-app-pub-4506479419321713/8575003241";
#elif UNITY_IPHONE
    private const string _adUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string _adUnitId = "unused";
#endif

    private RewardedAd _rewardedAd;

    private Action<bool> callBcak;
    
    private int rewardedRetryAttempt;


    public RewardedAdController()
    {
        LoadAd();
    }

    /// <summary>
    /// Loads the ad.
    /// </summary>
    public void LoadAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            DestroyAd();
        }

        Log.Info("Loading rewarded ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                rewardedRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));
                GF.Timer.AddTimer("LoadRewardAD", (float)retryDelay, () =>
                {
                    LoadAd();
                    GF.Timer.RemoveTimer("LoadRewardAD");
                });
                
                Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                return;
            }
            // If the operation failed for unknown reasons.
            // This is an unexpected error, please report this bug if it happens.
            if (ad == null)
            {
                rewardedRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));
                GF.Timer.AddTimer("LoadRewardAD", (float)retryDelay, () =>
                {
                    LoadAd();
                    GF.Timer.RemoveTimer("LoadRewardAD");
                });
                
                Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                return;
            }

            // The operation completed successfully.
            Log.Info("Rewarded ad loaded with response : " + ad.GetResponseInfo());
            _rewardedAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlers(ad);

            // Inform the UI that the ad is ready.
            // AdLoadedStatus?.SetActive(true);
            
            rewardedRetryAttempt = 0;
        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public bool ShowAd(Action<bool> _callBcak)
    {
        callBcak = _callBcak;
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            Log.Info("admob Showing rewarded ad.");
            _rewardedAd.Show((Reward reward) =>
            {
                Log.Info(String.Format("Rewarded ad granted a reward: {0} {1}",
                                        reward.Amount,
                                        reward.Type));
                _callBcak?.Invoke(true);
                _callBcak = null;
                
                CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_succeeded);
            });
            
            return true;
        }
        else
        {
            Log.Error("Rewarded ad is not ready yet.");
            _callBcak?.Invoke(false);
            _callBcak = null;
            
            CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_failed);
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
        if (_rewardedAd != null)
        {
            Log.Info("Destroying rewarded ad.");
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        // Inform the UI that the ad is not ready.
        // AdLoadedStatus?.SetActive(false);
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogResponseInfo()
    {
        if (_rewardedAd != null)
        {
            var responseInfo = _rewardedAd.GetResponseInfo();
            UnityEngine.Debug.Log(responseInfo);
        }
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Log.Info(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
            
            var revenue = adValue.Value / 1000000f;
            PcardGiftRewardSession.Add(revenue);
            AdjustManager.Instance.LogAdRevenue(revenue, adValue.CurrencyCode);
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Log.Info("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Log.Info("Rewarded ad was clicked.");
        };
        // Raised when the ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Log.Info("Rewarded ad full screen content opened.");
            CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display);
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Log.Info("Rewarded ad full screen content closed.");
            LoadAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Log.Error("Rewarded ad failed to open full screen content with error : "
                + error);
            CommonHelper.LogEvent(AdjustEventCodeEvent.ad_display_failed);
        };
    }
}
