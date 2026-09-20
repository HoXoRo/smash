using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
public class ArrowPaymentListBank : LoopListBankBase
{
    public List<PaymentItemData> m_PaymentList = new List<PaymentItemData>();
    private PlayerDataModel m_PlayerData;
    private List<LoopListBankData> m_DataList = new List<LoopListBankData>();
    private PaymentItemData m_SelectedPayment;
    private string language = "";
    private ArrowLocalizationMoney m_LocalizationMoney = null;
    private bool m_UseHorizontalLayout;
    // 选中事件
    public System.Action<PaymentItemData> OnPaymentSelected;
    private void Awake()
    {
        m_PlayerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        InitPaymentData();
    }

    public void ReloadPaymentData()
    {
        language = "";
        m_LocalizationMoney = null;
        InitPaymentData();
    }

    private void InitPaymentData()
    {
        m_PaymentList.Clear();
        if (string.IsNullOrEmpty(language))
        {
            if (m_PlayerData != null && !string.IsNullOrEmpty(m_PlayerData.Language))
            {
                language = m_PlayerData.Language;
                var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
                var langRow = langTb.GetDataRow(row => row.LanguageKey == language);
                if (langRow == null)
                {
                    langRow = langTb.MinIdDataRow;
                    language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey).ToString();//不支持的语言默认用英文
                }
            }
            else
            {
                language = GFBuiltin.Localization.SystemLanguage.ToString();
                var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
                var langRow = langTb.GetDataRow(row => row.LanguageKey == language);
                if (langRow == null)
                {
                    langRow = langTb.MinIdDataRow;
                    language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey).ToString();//不支持的语言默认用英文
                }
            }

            if (m_LocalizationMoney == null)
            {
                var moneyTb = GF.DataTable.GetDataTable<ArrowLocalizationMoney>();
                m_LocalizationMoney = moneyTb.GetDataRow(row => row.LanguageName == language);
                if (m_LocalizationMoney == null)
                {
                    m_LocalizationMoney = moneyTb.MinIdDataRow;
                }
            }
        }
#if UNITY_EDITOR
        language = GF.Localization.Language.ToString(); //编辑器下使用设置语言方便测试;

        var moneyTb2 = GF.DataTable.GetDataTable<ArrowLocalizationMoney>();
        m_LocalizationMoney = moneyTb2.GetDataRow(row => row.LanguageName == language);
        if (m_LocalizationMoney == null)
        {
            m_LocalizationMoney = moneyTb2.MinIdDataRow;
        }

#endif
        if (m_LocalizationMoney == null || m_LocalizationMoney.Currency == null)
            return;

        for (int i = 0; i < m_LocalizationMoney.Currency.Length; i++)
        {
            var currency = m_LocalizationMoney.Currency[i];
            m_PaymentList.Add(new PaymentItemData { payment = currency });
        }
        // m_PaymentList.Add(new PaymentItemData { payment = "PayPal" });
        // m_PaymentList.Add(new PaymentItemData { payment = "OVO" });
        // m_PaymentList.Add(new PaymentItemData { payment = "Pay" });
        // m_PaymentList.Add(new PaymentItemData { payment = "PayPay" });
        // m_PaymentList.Add(new PaymentItemData { payment = "YandexMoney" });

        RefreshData();
    }

    public override List<LoopListBankData> InitLoopListBankDataList()
    {
        RefreshData();
        return m_DataList;
    }

    public override int GetListLength()
    {
        return m_DataList.Count;
    }

    public override LoopListBankData GetLoopListBankData(int index)
    {
        if (index < 0 || index >= m_DataList.Count)
        {
            return new LoopListBankData();
        }
        return m_DataList[index];
    }

    public override List<LoopListBankData> GetLoopListBankDatas()
    {
        return m_DataList;
    }

    public override void SetLoopListBankDatas(List<LoopListBankData> newDatas)
    {
        m_DataList = newDatas;
    }

    public override int GetCellPreferredTypeIndex(int index)
    {
        return 0; // 使用单一预制体
    }

    public override Vector2 GetCellPreferredSize(int index)
    {
        if (m_UseHorizontalLayout)
            return new Vector2(333f * 0.7f, 0);

        return new Vector2(0, 120); // 固定高度，宽度自适应
    }

    public void SetHorizontalLayout(bool horizontal)
    {
        m_UseHorizontalLayout = horizontal;
    }

    public void Refresh()
    {
        RefreshData();
    }

    private void RefreshData()
    {
        m_DataList.Clear();

        // 将支付数据转换为LoopListBankData
        for (int i = 0; i < m_PaymentList.Count; i++)
        {
            var loopData = new LoopListBankData
            {
                Content = m_PaymentList[i],
                UniqueID = $"Payment_{m_PaymentList[i].payment}"
            };
            m_DataList.Add(loopData);
        }
    }

    public PaymentItemData GetSelectedPayment()
    {
        return m_SelectedPayment;
    }

    public void SetSelectedPayment(string payment)
    {
        // 查找对应的支付数据
        var paymentData = m_PaymentList[0];
        if (payment != "Default")
        {
            paymentData = m_PaymentList.Find(p => string.Equals(p.payment, payment, System.StringComparison.OrdinalIgnoreCase));
        }
        
        if (paymentData != null)
        {
            m_SelectedPayment = paymentData;
            Log.Info($"支付方式已选择: {payment}");

            // 触发选中事件
            OnPaymentSelected?.Invoke(paymentData);
        }
        else
        {
            Log.Warning($"未找到支付方式: {payment}");
        }
    }

    public bool IsPaymentSelected(string payment)
    {
        return m_SelectedPayment != null && string.Equals(m_SelectedPayment.payment, payment, System.StringComparison.OrdinalIgnoreCase);
    }
}

public class PaymentItemData
{
    public string payment;
}