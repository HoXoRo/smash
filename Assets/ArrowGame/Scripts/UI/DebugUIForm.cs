using UnityEngine;
using TMPro;
using GameFramework.Event;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/DebugUIForm")]

public partial class DebugUIForm : UIFormBase
{
    private ArrowLanguageListBank m_LanguageListBank;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        m_LanguageListBank = varLanguageScroll.GetComponent<ArrowLanguageListBank>();
        varLanguageScroll.dataSource = new LanguageDataSource(m_LanguageListBank);
        varLanguageScroll.prefabSource = varLanguageScroll.GetComponent<InitOnStart>();
        varLanguageName.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowLanguageList);

        varAddDollar.onClick.AddListener(AddDollar);
        varReduceDollor.onClick.AddListener(ReduceDollar);
        varAddPcard.onClick.AddListener(AddPcard);
        varReducePcard.onClick.AddListener(ReducePcard);
        varBtnVideo.onClick.AddListener(() => AdsManager.Instance.ShowAd(
            success => LogVideoResult("Auto", success), "debug_rewarded"));
        varBtnInter.onClick.AddListener(ShowInterstitial);
        varBtnOpenMax.onClick.AddListener(() => AdsManager.Instance.ShowMaxDebug());
        varBtnOpenAdmob.onClick.AddListener(() => AdsManager.Instance.ShowAdmobDebug());
        varBtnInter_max.onClick.AddListener(() => AdsManager.Instance.ShowMaxInter("debug_max_interstitial"));
        varBtnInter_admob.onClick.AddListener(() => AdsManager.Instance.ShowAdmobInter("debug_admob_interstitial"));
        varBtnBanner_max.onClick.AddListener(() => AdsManager.Instance.ShowMaxBanner());
        varBtnBanner_admob.onClick.AddListener(() => AdsManager.Instance.ShowAdmobBanner());
        varBtnBanner_hide.onClick.AddListener(CommonHelper.HideBannerAd);
        varBtnVideo_max.onClick.AddListener(() => AdsManager.Instance.ShowMaxAd(
            success => LogVideoResult("MAX", success), "debug_max_rewarded"));
        varBtnVideo_admob.onClick.AddListener(() => AdsManager.Instance.ShowAdmobAd(
            success => LogVideoResult("AdMob", success), "debug_admob_rewarded"));
        varBtnAddLv.onClick.AddListener(AddLevel);
        varBtnReduceLv.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ReduceLevel);
        varBtnResetTask.onClick.AddListener(ResetTasks);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(MahJongLanguageChangeEventArgs.EventId, OnLanguageChanged);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        varLanguageScroll.gameObject.SetActive(false);
        RefreshLanguage();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(MahJongLanguageChangeEventArgs.EventId, OnLanguageChanged);
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        base.OnClose(isShutdown, userData);
    }

    private void ShowLanguageList()
    {
        m_LanguageListBank.Refresh();
        varLanguageScroll.totalCount = m_LanguageListBank.GetListLength();
        varLanguageScroll.RefillCells();
        varLanguageScroll.gameObject.SetActive(true);
    }

    private void RefreshLanguage()
    {
        var language = GF.Setting.GetLanguage();
        var languageTable = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
        var languageRow = languageTable.GetDataRow(row => row.LanguageKey == language.ToString());
        varLanguageName.text = languageRow.LanguageDisplay;
    }

    private void OnLanguageChanged(object sender, GameEventArgs e)
    {
        RefreshLanguage();
        varLanguageScroll.gameObject.SetActive(false);
        GF.Localization.RemoveAllRawStrings();
        GF.Localization.LoadLanguage(GF.Localization.Language.ToString(), this);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        GF.UI.UpdateLocalizationTexts();
    }

    private void AddDollar()
    {
        float amount = GetInputDollar();
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerData.Dollars = playerData.Dollars + amount;
    }

    private void ReduceDollar()
    {
        float amount = GetInputDollar();
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerData.Dollars = Mathf.Max(0f, playerData.Dollars - amount);
    }

    private float GetInputDollar()
    {
        if (varInputDollar == null || !float.TryParse(varInputDollar.text, out float amount))
        {
            return 0f;
        }

        return Mathf.Max(0f, amount);
    }

    private void AddPcard()
    {
        ChangePcard(GetInputPcard());
    }

    private void ReducePcard()
    {
        ChangePcard(-GetInputPcard());
    }

    private void ChangePcard(float amount)
    {
        if (amount == 0f) return;

        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        ArrowUITopbar.CompletePcardDisplay(playerData);
        playerData.Pcard = Mathf.Max(0f, playerData.Pcard + amount);
        playerData.SetPcardDisplayValue(playerData.Pcard);
    }

    private float GetInputPcard()
    {
        if (varInputPcard == null || !float.TryParse(varInputPcard.text, out float amount)
            || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return 0f;
        }

        return Mathf.Max(0f, amount);
    }

    private static void LogVideoResult(string provider, bool success)
    {
        Debug.Log($"[DebugUIForm] {provider} rewarded ad completed: {success}");
    }

    private void ShowInterstitial()
    {
        AdsManager.Instance.ShowInter("debug_interstitial");
    }

    private void AddLevel()
    {
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (playerData.LevelId >= int.MaxValue) return;
        playerData.LevelId++;
        playerData.Save();
    }

    private void ReduceLevel()
    {
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (playerData.LevelId <= 1) return;
        playerData.LevelId--;
        playerData.Save();
    }

    private void ResetTasks()
    {
        GF.DataModel.GetOrCreate<PlayerDataModel>().ResetTaskProgress();
    }
}
