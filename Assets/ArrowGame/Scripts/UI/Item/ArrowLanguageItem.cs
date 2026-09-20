using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class ArrowLanguageItem : UIItemBase
{
    protected override void OnInit()
    {
        base.OnInit();
        varLanguageBtn.onClick.RemoveAllListeners();
        varLanguageBtn.onClick.AddListener(OnClick);
    }
    private string m_LanguageKey;
    public void SetData(string languageKey)
    {
        m_LanguageKey = languageKey;
        varLanguageName.text = languageKey;
    }
    public void OnClick()
    {
        GF.Setting.SetLanguage(System.Enum.Parse<GameFramework.Localization.Language>(m_LanguageKey));
        GF.Event.Fire(this,MahJongLanguageChangeEventArgs.Create());
    }
}