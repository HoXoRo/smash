using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using AHTIYCt9QWESDK;
using UnityGameFramework.Runtime;
using System;

public class AdsManager
{
    private bool isRegistered = false;
    private static AdsManager m_Instance;
    
    private GoogleMobileAdsController _googleMobileAdsController;
    private ArrowMaxAdsController  _maxAdsController;
    private RewardedAdController _rewardedAdController;
    private InterstitialAdController _interstitialAdController;
    private BannerViewController _bannerViewController;
    
    public static AdsManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new AdsManager();
            }
            return m_Instance;
        }
    }
    public AdsManager()
    {
        Init();
    }
    public void ShowAd(Action<bool> reward, string adId = "")
    {
        if (!_rewardedAdController.ShowAd(reward, adId, allowFallback: true))
        {
            _maxAdsController.ShowRewardedAd(reward, adId);
        }

    }

    public void ShowInter(string adId = "", Action onCompleted = null)
    {
        if (!_interstitialAdController.ShowAd(onCompleted))
        {
            _maxAdsController.ShowInterstitial(adId, onCompleted);
        }

    }


    private bool isMaxBanner = false;
    public void ShowBanner()
    {
        if (!_bannerViewController.ShowAd())
        {
            if (isMaxBanner) return;
            isMaxBanner = true;
            _maxAdsController.ToggleBannerVisibility();
        }
    }
    
    public void HideBanner()
    {
        _bannerViewController.HideAd();
        if (isMaxBanner)
        {
            isMaxBanner = false;
            _maxAdsController.ToggleBannerVisibility();
        }
    }


    public void ShowMaxAd(Action<bool> reward, string adId = "")
    {
        _maxAdsController.ShowRewardedAd(reward, adId);
    }
    
    public void ShowMaxInter(string adId = "")
    {
        _maxAdsController.ShowInterstitial(adId);
    }

    public void ShowAdmobAd(Action<bool> reward, string adId = "")
    {
        _rewardedAdController.ShowAd(reward, adId);
    }
    
    public void ShowAdmobInter(string adId = "")
    {
        _interstitialAdController.ShowAd();
    }


    public void ShowAdmobBanner()
    {
        _bannerViewController.ShowAd();
    }
    
    public void ShowMaxBanner()
    {
        _maxAdsController.ToggleBannerVisibility();
    }

    public void ShowH5()
    {

    }

    public void ShowAdmobDebug()
    {
        _googleMobileAdsController.OpenAdInspector();
    }

    public void ShowMaxDebug()
    {
        _maxAdsController.ShowMaxDebugger();
    }
    
    private void Init()
    {
        _maxAdsController = new ArrowMaxAdsController();
        _googleMobileAdsController = new GoogleMobileAdsController();
        _rewardedAdController = new RewardedAdController();
        _interstitialAdController = new InterstitialAdController();
        _bannerViewController = new BannerViewController();
    }

    public void ShowMaxDebugger()
    {
        _maxAdsController.ShowMaxDebugger();
    }
}
internal sealed class InterstitialCompletion
{
    private Action callback;
    private bool finishing;
    public bool IsFinishing => finishing;
    // A unique placement associates MAX close/failure events with this presentation.
    public readonly string Placement = "gift_" + Guid.NewGuid().ToString("N");
    public InterstitialCompletion(Action callback) { this.callback = callback; }
    public async Cysharp.Threading.Tasks.UniTask Finish()
    {
        await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
        if (finishing) return;
        finishing = true;
        var completed = callback;
        callback = null;
        completed?.Invoke();
    }
}
