using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/MakeupOrdemItem")]
public partial class ArrowMakeupOrdemItem : UIItemBase
{
    private MakeupData m_makeupdata;
    protected override void OnInit()
    {
        base.OnInit();
    }
    public void SetData(object data, ArrowMakeupOrdemListBank listBank)
    {
        if (data is MakeupData makeupData)
        {   
            this.m_makeupdata = makeupData;
        }
        else
        {
            Log.Error("SetData: data is not MakeupData");
            return;
        }

        varOutCount.text = CommonHelper.GetDollarString(m_makeupdata.supplyNeed);
        varTime.text = CommonHelper.GetNowTimeString(m_makeupdata.makeupTimestamp);
        varResult.text = m_makeupdata.makeupStep==MakeupStep.Success?"Success":"Fail";
        // 加载图标
        GF.Resource.LoadAsset($"Assets/MahjongGame/Sprites/UI/BankIcon/{m_makeupdata.paymentInfo.payment}.png", typeof(Sprite), new GameFramework.Resource.LoadAssetCallbacks(
            (assetName, asset, duration, userData) =>
            {
                varBankIcon.sprite = asset as Sprite;
            },
            (assetName, status, errorMessage, userData) =>
            {
                Log.Error("Can not load sprite '{0}' from '{1}' with error message '{2}'.", "Icon_Bank", assetName, errorMessage);
            }));

    }
}