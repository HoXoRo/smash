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
        if (!_rewardedAdController.ShowAd(reward))
        {
            _maxAdsController.ShowRewardedAd(reward, adId);
        }

    }

    public void ShowInter(string adId = "")
    {
        if (!_interstitialAdController.ShowAd())
        {
            _maxAdsController.ShowInterstitial(adId);
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
        _rewardedAdController.ShowAd(reward);
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