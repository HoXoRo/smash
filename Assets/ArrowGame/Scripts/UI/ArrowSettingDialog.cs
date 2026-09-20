using System;
using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/SettingDialog")]
public partial class ArrowSettingDialog : UIFormBase
{
    int m_ClickCount;
    float m_LastClickTime;
    readonly float clickInterval = 0.4f;
    private Action onHomeAction;
    private Action onResumeAction;

    private InitOnStart m_InitOnStart;
    private ArrowLanguageListBank m_ListBank;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // 初始化列表组件
        m_InitOnStart = varLanguageScroll.GetComponent<InitOnStart>();
        m_ListBank = varLanguageScroll.GetComponent<ArrowLanguageListBank>();

        var customDataSource = new LanguageDataSource(m_ListBank);
        varLanguageScroll.dataSource = customDataSource;
        varLanguageScroll.prefabSource = m_InitOnStart;

    }
    public override void InitLocalization()
    {
        base.InitLocalization();
        varVersionTxt.text = Utility.Text.Format("{0}v{1}", AppSettings.Instance.DebugMode ? "Debug " : string.Empty, GF.Base.EditorResourceMode ? Application.version : Utility.Text.Format("{0}({1})", Application.version, GF.Resource.InternalResourceVersion));
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        GF.Event.Subscribe(UserTypeChangeEventArgs.EventId, OnUserTypeChange);
        GF.Event.Subscribe(MahJongLanguageChangeEventArgs.EventId, OnLanguageChanged);
        varLanguageScroll.gameObject.SetActive(false);
        varAorB.text = CommonHelper.IsSpec() ? "B" : "A";

        m_ClickCount = 0;
        m_LastClickTime = Time.time;
        onHomeAction = Params.Get<VarAction>("OnHomeAction")?.Value;
        if (Params.Get<VarAction>("OnResumeAction") != null)
        {
            onResumeAction = Params.Get<VarAction>("OnResumeAction")?.Value;
        }
        InitSettings();
        
        // CommonHelper.LogEvent(AdjustEventCodeEvent.setting_enter);

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        GF.Event.Unsubscribe(UserTypeChangeEventArgs.EventId, OnUserTypeChange);
        GF.Event.Unsubscribe(MahJongLanguageChangeEventArgs.EventId, OnLanguageChanged);
        base.OnClose(isShutdown, userData);
        onHomeAction = null;
        onResumeAction = null;
        
        // CommonHelper.LogEvent(AdjustEventCodeEvent.setting_exit);

    }
    private void OnUserTypeChange(object sender, GameEventArgs e)
    {
        var args = e as UserTypeChangeEventArgs;
        if (args == null) return;

        var isB = CommonHelper.IsSpec();
        if (varAorB != null)
        {
            varAorB.text = isB ? "B" : "A";
            // varAorB.color = isB ? Color.red : Color.green;
        }
    }
    private void InitSettings()
    {
        varMusicController.SetOnOff(!GF.Setting.GetMediaMute(Const.SoundGroup.Music));
        varSFXController.SetOnOff(!GF.Setting.GetMediaMute(Const.SoundGroup.Sound));
        varVibrateController.SetOnOff(!GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate));
        varToHome.gameObject.SetActive(onHomeAction != null);
        // varBtnPrivacy.gameObject.SetActive(onHomeAction == null);
        bool isDebug = CommonHelper.IsDebug();
        varDebug.SetActive(isDebug && onHomeAction == null);

        varBg.sizeDelta = new Vector2(1058f, onHomeAction != null ? 1100f : isDebug ? 1100f : 900f);
        RefreshLanguage();
    }
    private void OnMusicSliderChanged()
    {
        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Music);
        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, !isOn ? 1 : 0);
        GF.Setting.SetMediaMute(Const.SoundGroup.Music, isOn);
        Log.Info($" setting OnMusicSliderChanged: {GF.Setting.GetMediaMute(Const.SoundGroup.Music)}");
        varMusicController.SetOnOff(!isOn);
        
        // if (isOn)
        // {
        //     CommonHelper.LogEvent(AdjustEventCodeEvent.setting_music_open);
        // }
        // else
        // {
        //     CommonHelper.LogEvent(AdjustEventCodeEvent.setting_music_close);
        // }
    }

    private void OnSoundFxSliderChanged()
    {
        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Sound);
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, !isOn ? 1 : 0);
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, isOn);
        Log.Info($" setting OnSoundFxSliderChanged: {GF.Setting.GetMediaMute(Const.SoundGroup.Sound)}");
        varSFXController.SetOnOff(!isOn);
        
        // if (isOn)
        // {
        //     CommonHelper.LogEvent(AdjustEventCodeEvent.setting_effect_open);
        // }
        // else
        // {
        //     CommonHelper.LogEvent(AdjustEventCodeEvent.setting_effect_close);
        // }
    }

    private void VibrateChanged()
    {
        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate);
        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, isOn);
        Log.Info($" setting VibrateChanged: {GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate)}");
        varVibrateController.SetOnOff(!isOn);
    }

    private void RefreshLanguage()
    {
        var curLang = GF.Setting.GetLanguage();
        var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
        var langRow = langTb.GetDataRow(row => row.LanguageKey == curLang.ToString());
        varLanguageName.text = langRow.LanguageDisplay;
    }
    protected override void OnButtonClick(object sender, string btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        switch (btSelf)
        {
            case "Button_Music":
                OnMusicSliderChanged();
                break;
            case "Button_SFX":
                OnSoundFxSliderChanged();
                break;
            case "Button_Vibrate":
                VibrateChanged();
                break;
            case "Button_Privacy":
                GF.UI.OpenUIForm(UIViews.ArrowPrivacyPolicyUIForm);
                break;
            case "Button_Home":
                onHomeAction?.Invoke();
                GF.UI.Close(this.UIForm);
                // GF.UI.CloseUIForms(UIViews.MenuUIForm);
                break;
            case "Button_Close":
                GF.UI.Close(this.UIForm);
                break;
            case "Button_Resume":
                CommonHelper.ShowInterstitial("interstitial_resume");
                onResumeAction?.Invoke();
                GF.UI.Close(this.UIForm);
                break;
            case "ABChange":
                CommonHelper.IsConstraintBorA();
                break;
            case "OpenAdDebug":
                AdsManager.Instance.ShowMaxDebugger();
                break;
            case "ShowVideo":
                CommonHelper.ShowVideoAd("test_video", b => { });
                break;
            case "ShowInter":
                CommonHelper.ShowInterstitial("test_inter");
                break;
            case "LanguageName":
                RefreshList();
                varLanguageScroll.gameObject.SetActive(true);
                break;
            case "AddDiamond":
                var diamond = float.Parse(varInputDiamond.text);
                var playerData = GF.DataModel?.GetDataModel<PlayerDataModel>();
                playerData.Dollars += diamond;
                playerData.Save();
                break;
            case "ReduceDiamond":
                var diamond1 = float.Parse(varInputDiamond.text);
                var playerData1 = GF.DataModel?.GetDataModel<PlayerDataModel>();
                playerData1.Dollars -= diamond1;
                playerData1.Save();
                break;
        }



    }
    public void RefreshList()
    {
        m_ListBank.Refresh();
        varLanguageScroll.totalCount = m_ListBank.GetListLength();
        varLanguageScroll.RefillCells();
    }
    void OnLanguageChanged(object sender, GameEventArgs e)
    {
        RefreshLanguage();
        varLanguageScroll.gameObject.SetActive(false);
        // GF.UI.CloseUIForms(UIViews.LanguagesDialog);
        ReloadLanguage();
    }
    private void ReloadLanguage()
    {
        GF.Localization.RemoveAllRawStrings();
        GF.Localization.LoadLanguage(GF.Localization.Language.ToString(), this);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        GF.UI.UpdateLocalizationTexts();
    }
    public void OnClickVersionText()
    {
        if (Time.time - m_LastClickTime <= clickInterval)
        {
            m_ClickCount++;
            if (m_ClickCount > 5)
            {
                GF.Debugger.ActiveWindow = !GF.Debugger.ActiveWindow;
                m_ClickCount = 0;
            }
        }
        else
        {
            m_ClickCount = 0;
        }
        m_LastClickTime = Time.time;
    }


}

// 自定义数据源类，用于连接TaskListBank和LoopVerticalScrollRect_Ordem
public class LanguageDataSource : LoopScrollDataSource
{
    private ArrowLanguageListBank m_ListBank;

    public LanguageDataSource(ArrowLanguageListBank listBank)
    {
        m_ListBank = listBank;
    }

    public void ProvideData(Transform transform, int idx)
    {
        if (m_ListBank != null)
        {
            var bankData = m_ListBank.GetLoopListBankData(idx);
            if (bankData != null)
            {
                // 调用TaskItem的SetData方法
                var TaskItem = transform.GetComponent<ArrowLanguageItem>();
                if (TaskItem != null)
                {
                    Log.Info($"设置提现记录数据: idx={idx}, content={bankData.Content}");
                    TaskItem.SetData((string)bankData.Content);
                }
            }
        }
        // 同时调用ScrollCellIndex方法保持兼容性
        transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
    }
}

