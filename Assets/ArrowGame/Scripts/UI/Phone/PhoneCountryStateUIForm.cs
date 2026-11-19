using System;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PhoneCountryStateUIForm")]
public partial class PhoneCountryStateUIForm : UIFormBase, UnityEngine.UI.LoopScrollDataSource
{
    public const string SelectionKey = "CountryStateSelection";
    [SerializeField] private GameObject countryStatesItemPrefab;
    private PhoneCountrySelectionRequest m_Request;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        var source = varLoopVerticalScrollRect.GetComponent<InitOnStart>();
        if (source == null) source = varLoopVerticalScrollRect.gameObject.AddComponent<InitOnStart>();
        source.item = countryStatesItemPrefab;
        varLoopVerticalScrollRect.prefabSource = source;
        varLoopVerticalScrollRect.dataSource = this;
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(PhoneCountryStateUIForm));
        m_Request = Params.Get<VarObject>(SelectionKey)?.Value as PhoneCountrySelectionRequest;
        varLoopVerticalScrollRect.totalCount = m_Request == null ? 0 :
            m_Request.Type == PhoneCountrySelectionType.Country ? m_Request.Countries.Length :
            m_Request.Country.states?.Length ?? 0;
        varLoopVerticalScrollRect.RefillCells();
    }

    public void ProvideData(Transform itemTransform, int index)
    {
        string label = m_Request.Type == PhoneCountrySelectionType.Country
            ? m_Request.Countries[index].name : m_Request.Country.states[index];
        itemTransform.GetComponent<CountryStatesItem>().SetData(label, index, SelectItem);
    }

    private void SelectItem(int index)
    {
        if (m_Request?.OnSelected == null) return;
        var callback = m_Request.OnSelected;
        m_Request.OnSelected = null;
        callback(index);
        OnClickClose();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varLoopVerticalScrollRect.ClearCells();
        m_Request = null;
        base.OnClose(isShutdown, userData);
    }
}

public enum PhoneCountrySelectionType { Country, State }
public sealed class PhoneCountrySelectionRequest
{
    public PhoneCountrySelectionType Type;
    public PhoneCountryData[] Countries;
    public PhoneCountryData Country;
    public Action<int> OnSelected;
}
