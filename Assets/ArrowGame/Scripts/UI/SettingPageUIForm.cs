using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/SettingPageUIForm")]

public partial class SettingPageUIForm : UIFormBase
{

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
         
        // varBtnTask.onClick.AddListener(() => OnTabClicked(UIViews.TaskPageUIForm, varBtnTask));
        // varBtnHome.onClick.AddListener(() => OnTabClicked(UIViews.HomePageUIForm, varBtnHome));
        // varBtnSettings.onClick.AddListener(() => OnTabClicked(UIViews.SettingPageUIForm, varBtnSettings));

        InitSettings();
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
        }
    }
    
    private void InitSettings()
    {
        varMusicController.SetOnOff(!GF.Setting.GetMediaMute(Const.SoundGroup.Music));
        varSFXController.SetOnOff(!GF.Setting.GetMediaMute(Const.SoundGroup.Sound));
        varVibrateController.SetOnOff(!GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate));
    }
    
    
    private void OnMusicSliderChanged()
    {
        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Music);
        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, !isOn ? 1 : 0);
        GF.Setting.SetMediaMute(Const.SoundGroup.Music, isOn);
        Log.Info($" setting OnMusicSliderChanged: {GF.Setting.GetMediaMute(Const.SoundGroup.Music)}");
        varMusicController.SetOnOff(!isOn);
        
    }

    private void OnSoundFxSliderChanged()
    {
        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Sound);
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, !isOn ? 1 : 0);
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, isOn);
        Log.Info($" setting OnSoundFxSliderChanged: {GF.Setting.GetMediaMute(Const.SoundGroup.Sound)}");
        varSFXController.SetOnOff(!isOn);
        
    }

    private void VibrateChanged()
    {
        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate);
        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, isOn);
        Log.Info($" setting VibrateChanged: {GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate)}");
        varVibrateController.SetOnOff(!isOn);
    }
    
}