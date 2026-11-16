using System;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PhoneAddressUIForm")]
public partial class PhoneAddressUIForm : UIFormBase
{
    [SerializeField] private TextAsset countriesStatesJson;
    private PhoneCountryData[] m_Countries;
    private PhoneCountryData m_Country;
    private string m_State;
    private bool m_SelectorOpen;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnCountry.onClick.AddListener(OpenCountries);
        varBtnState.onClick.AddListener(OpenStates);
        if (countriesStatesJson != null)
            m_Countries = JsonUtility.FromJson<PhoneCountryCatalog>(countriesStatesJson.text)?.countries;
        m_Countries ??= Array.Empty<PhoneCountryData>();
        
        varBtnClose.onClick.AddListener(OnClickClose);

    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        if (m_Country == null)
        {
#if UNITY_EDITOR
            string language = GF.Base.EditorLanguage.ToString();
#else
            string language = GFBuiltin.Localization.SystemLanguage.ToString();
#endif
            SelectCountry(PhoneCountryCatalog.FindDefault(m_Countries, language));
        }
        RefreshSelection();
    }

    private void OpenCountries() => OpenSelector(PhoneCountrySelectionType.Country);
    private void OpenStates() => OpenSelector(PhoneCountrySelectionType.State);

    private void OpenSelector(PhoneCountrySelectionType type)
    {
        if (m_SelectorOpen || m_Country == null) return;
        var request = new PhoneCountrySelectionRequest
        {
            Type = type,
            Countries = m_Countries,
            Country = m_Country,
            OnSelected = index =>
            {
                if (type == PhoneCountrySelectionType.Country)
                    SelectCountry(m_Countries[index]);
                else
                    m_State = m_Country.states[index];
                RefreshSelection();
            }
        };
        var parameters = UIParams.Create();
        parameters.Set(PhoneCountryStateUIForm.SelectionKey, request);
        parameters.CloseCallback = _ => m_SelectorOpen = false;
        m_SelectorOpen = true;
        if (OpenSubUIForm(UIViews.PhoneCountryStateUIForm, 0, parameters) < 0)
            m_SelectorOpen = false;
    }

    private void SelectCountry(PhoneCountryData country)
    {
        if (country == m_Country) return;
        m_Country = country;
        m_State = country?.states != null && country.states.Length > 0 ? country.states[0] : string.Empty;
    }

    private void RefreshSelection()
    {
        varTxtContry.text = m_Country?.name ?? string.Empty;
        varTxtState.text = m_State ?? string.Empty;
        varBtnCountry.interactable = m_Countries.Length > 0;
        varBtnState.interactable = m_Country?.states != null && m_Country.states.Length > 0;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        m_SelectorOpen = false;
    }
}

[Serializable, Obfuz.ObfuzIgnore]
public sealed class PhoneCountryData
{
    public string code;
    public string name;
    public string[] defaultForLanguages;
    public string[] states;
}

[Serializable, Obfuz.ObfuzIgnore]
public sealed class PhoneCountryCatalog
{
    public PhoneCountryData[] countries;

    public static PhoneCountryData FindDefault(PhoneCountryData[] countries, string language)
    {
        if (countries == null || countries.Length == 0) return null;
        var unitedStates = Array.Find(countries, country => country.code == "US");
        if (language != "English")
        {
            foreach (var country in countries)
                if (country.defaultForLanguages != null && Array.IndexOf(country.defaultForLanguages, language) >= 0)
                    return country;
        }
        // Keep the UI usable if US is accidentally removed from the catalog.
        return unitedStates ?? countries[0];
    }
}
