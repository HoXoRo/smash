using System;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PhoneAddressStateUIForm")]
public partial class PhoneAddressStateUIForm : UIFormBase
{
    private DateTime m_OrderTime;
    private bool m_HasOrder;
    private bool m_HasOrderTime;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(PhoneAddressStateUIForm));
        var address = GF.DataModel.GetOrCreate<PlayerDataModel>().PhoneAddress;
        m_HasOrder = address != null;
        varTxtContactInfo.text = address == null ? string.Empty : $"{address.FullName} | {address.PhoneNumber}";
        varTxtDetailedAddress.text = address?.DetailedAddress ?? string.Empty;
        varTxtCountryState.text = address == null ? string.Empty : $"{address.Country}, {address.State}";
        varTxtOrderId.text = address == null ? string.Empty : GF.Localization.GetString("PhoneAddressStateUIForm.orderId", address.OrderID ?? string.Empty);
        varTxtTimeCompleted.text = address == null ? string.Empty : GF.Localization.GetString("PhoneAddressStateUIForm.completed", address.Time ?? string.Empty);
        m_HasOrderTime = DateTime.TryParseExact(address?.Time, "yyyy-MM-dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeLocal, out m_OrderTime);
        RefreshOrderState();
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        RefreshOrderState();
    }

    private void RefreshOrderState()
    {
        bool completed = m_HasOrderTime && DateTime.Now - m_OrderTime >= TimeSpan.FromDays(7);
        bool showState1 = m_HasOrder && !completed;
        bool showState2 = m_HasOrder && completed;
        if (varState1.activeSelf != showState1) varState1.SetActive(showState1);
        if (varState2.activeSelf != showState2) varState2.SetActive(showState2);
    }
}