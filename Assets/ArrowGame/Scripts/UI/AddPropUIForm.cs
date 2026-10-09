using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/AddPropUIForm")]
public partial class AddPropUIForm : UIFormBase
{
    private PlayerDataModel playerData;
    private int propCoin;
    private bool isBusy;
    private int openVersion;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnAd.onClick.AddListener(BuyByAd);
        varBtnCoin.onClick.AddListener(BuyByCoin);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        propCoin = GF.Config.GetInt("propCoin");
        varTxtCoin.text = propCoin.ToString();
        openVersion++;
        isBusy = false;
        RefreshButtons();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

    private void RefreshButtons()
    {
        varBtnAd.interactable = !isBusy;
        varBtnCoin.interactable = !isBusy && propCoin >= 0;
        varBtnClose.interactable = !isBusy;
    }

    private void BuyByCoin()
    {
        if (isBusy || propCoin < 0) return;
        CommonHelper.ConsumeCoins(propCoin, success =>
        {
            if (success) GrantRocket();
        });
    }

    private void BuyByAd()
    {
        if (isBusy) return;
        isBusy = true;
        RefreshButtons();
        int requestVersion = openVersion;
        bool completed = false;
        PlayerDataModel rewardData = playerData;
        CommonHelper.ShowVideoAd("rocket_refill", success =>
        {
            if (completed) return;
            completed = true;
            if (success) rewardData.AddRocket();
            if (this == null || requestVersion != openVersion) return;
            if (success)
            {
                GF.UI.CloseUIForm(Id);
                return;
            }
            isBusy = false;
            RefreshButtons();
        });
    }

    private void GrantRocket()
    {
        isBusy = true;
        RefreshButtons();
        playerData.AddRocket();
        GF.UI.CloseUIForm(Id);
    }
}
