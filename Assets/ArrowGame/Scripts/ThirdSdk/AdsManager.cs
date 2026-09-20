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
    
    private ArrowMaxAdsController  _maxAdsController;
    
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
        _maxAdsController.ShowRewardedAd(reward, adId);
    }

    public void ShowInter(string adId = "")
    {
        _maxAdsController.ShowInterstitial(adId);
    }
    
    public void ShowH5()
    {

    }

    private void Init()
    {
        _maxAdsController = new ArrowMaxAdsController();
    }

    public void ShowMaxDebugger()
    {
        _maxAdsController.ShowMaxDebugger();
    }
}
