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

    public void ShowBanner()
    {
        _bannerViewController.ShowAd();
    }

    public void ShowH5()
    {

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