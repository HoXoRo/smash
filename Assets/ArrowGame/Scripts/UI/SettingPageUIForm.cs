using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/SettingPageUIForm")]

public partial class SettingPageUIForm : UIFormBase
{
    public const string P_FromGameplay = "FromGameplay";

    bool fromGameplay;
    bool isQuitting;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
         
        varBtnQuit.onClick.AddListener(OnQuitClicked);
        varBtnContinue.onClick.AddListener(OnClickClose);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        fromGameplay = Params.Get<VarBoolean>(P_FromGameplay)?.Value ?? false;
        isQuitting = false;
        varBtnQuit.gameObject.SetActive(fromGameplay);
        varBtnContinue.gameObject.SetActive(fromGameplay);
        varBtnClose.gameObject.SetActive(fromGameplay);
        InitSettings();
    }

    void OnQuitClicked()
    {
        if (!fromGameplay || isQuitting)
        {
            return;
        }

        GF.Sound.PlayEffect("ui/ui_click.mp3");
        QuitGameplayAsync().Forget();
    }

    async UniTaskVoid QuitGameplayAsync()
    {
        isQuitting = true;
        Interactable = false;
        try
        {
            string sceneAssetName = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
            if (!await GF.Scene.UnLoadSceneAwait(sceneAssetName))
            {
                isQuitting = false;
                Interactable = true;
                return;
            }
        }
        catch (System.Exception exception)
        {
            isQuitting = false;
            Interactable = true;
            Log.Error(exception.ToString());
            return;
        }

        GF.UI.CloseUIForm(Id);
        GF.UI.CloseUIForms(UIViews.PlayUIForm);
        GF.UI.OpenUIForm(UIViews.HallTabUIForm);
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
