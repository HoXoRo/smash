using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/AddLifeUIForm")]
public partial class AddLifeUIForm : UIFormBase
{
    private PlayerDataModel playerData;
    private bool adPending;
    private int lifeCoin;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnAd.onClick.AddListener(AddLifeByAd);
        varBtnCoin.onClick.AddListener(AddLifeByCoin);
        varBtnClose.onClick.AddListener(CloseWithAnimation);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        lifeCoin = GF.Config.GetInt("lifeCoin");
        varTxtCoin.text = lifeCoin.ToString();
        playerData.LifeChanged += RefreshTime;
        playerData.RefreshLife();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        playerData.LifeChanged -= RefreshTime;
        base.OnClose(isShutdown, userData);
    }

    private void RefreshTime()
    {
        int seconds = playerData.GetSecondsUntilNextLife();
        varTxtTime.text = seconds < 0 ? "FULL" : $"{seconds / 60:00}:{seconds % 60:00}";
        bool canAdd = playerData.LifeCount < PlayerDataModel.MaxLifeCount && !adPending;
        varBtnAd.interactable = canAdd;
        varBtnCoin.interactable = canAdd && lifeCoin >= 0 && playerData.Coins >= lifeCoin;
    }

    private void AddLifeByCoin()
    {
        if (adPending) return;
        playerData.RefreshLife();
        if (playerData.LifeCount >= PlayerDataModel.MaxLifeCount || !playerData.TrySpendCoins(lifeCoin)) return;
        playerData.AddLife(1);
        OnClickClose();
    }

    private void AddLifeByAd()
    {
        if (adPending) return;
        playerData.RefreshLife();
        if (playerData.LifeCount >= PlayerDataModel.MaxLifeCount) return;
        adPending = true;
        PlayerDataModel rewardData = playerData;
        bool completed = false;
        CommonHelper.ShowVideoAd("life_refill",success =>
        {
            if (completed) return;
            completed = true;
            adPending = false;
            if (success)
            {
                rewardData.AddLife(1);
                OnClickClose();
            }
        });
    }
}
