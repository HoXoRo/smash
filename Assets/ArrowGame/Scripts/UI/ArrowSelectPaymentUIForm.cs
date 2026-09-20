using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using TMPro;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/SelectPaymentUIForm")]
public partial class ArrowSelectPaymentUIForm : UIFormBase
{
    private PlayerDataModel m_PlayerData;
    private ArrowPaymentListBank m_ListBank;
    private CustomPaymentDataSource m_DataSource;
    private InitOnStart m_InitOnStart;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 初始化输入框事件
        // varFull.onValueChanged.AddListener(OnFullValueChanged);
        varFull.onSubmit.AddListener(OnFullMethodSubmit);
        // varAccount.onValueChanged.AddListener(OnAccountValueChanged);
        varCPF.onSubmit.AddListener(OnCPFMethodSubmit);

        // 初始化支付列表
        InitPaymentList();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // CommonHelper.LogEvent(AdjustEventCodeEvent.withdrawinfo_enter);
        CommonHelper.LogEvent(AdjustEventCodeEvent.popu_enter, new Dictionary<string, string>() {{"popu", "makeupInfoUI"}});

        m_PlayerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        // 注册选中事件
        m_ListBank.OnPaymentSelected += OnPaymentSelected;
        Log.Info("OnOpen - UI初始化完成");

        // 更新UI显示
        UpdateUI();

        // 加载已保存的支付信息
        LoadSavedPaymentInfo();
    }

    private void InitPaymentList()
    {
        // 初始化列表组件
        m_InitOnStart = varList.GetComponent<InitOnStart>();
        // 创建支付列表银行
        m_ListBank = varList.GetComponent<ArrowPaymentListBank>();
        Log.Info($"m_InitOnStart: {m_InitOnStart} m_ListBank: {m_ListBank}");

        // 创建自定义数据源
        m_DataSource = new CustomPaymentDataSource(m_ListBank);

        // 设置varList的数据源
        // 注意：这里需要根据实际的UI组件名称进行调整
        // 如果varvarList不存在，可以注释掉这部分代码
        if (varList != null)
        {
            varList.dataSource = m_DataSource;
            varList.totalCount = m_ListBank.GetListLength();
            varList.prefabSource = m_InitOnStart;
            varList.RefillCells();
        }

    }

    private void LoadSavedPaymentInfo()
    {
        // 加载已保存的支付信息到输入框
        // if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.accountId))
        // {
        //     varFull.text = m_PlayerData.MakeupPaymentInfo.accountId;
        // }
        // if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.cpf))
        // {
        //     varCPF.text = m_PlayerData.MakeupPaymentInfo.cpf;
        // }
        // if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.email))
        // {
        //     varAccount.text = m_PlayerData.MakeupPaymentInfo.email;
        // }

        // 如果有已选择的支付方式，更新列表选中状态
        if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.payment))
        {
            m_ListBank.SetSelectedPayment(m_PlayerData.MakeupPaymentInfo.payment);
        }
        else
        {
            m_ListBank.SetSelectedPayment("Default");
        }
    }

    public void UpdateUI()
    {
        // 根据选中的支付方式更新输入框提示
        UpdateInputFieldPlaceholders();
    }

    private void UpdateInputFieldPlaceholders()
    {
        PaymentItemData selectedPayment = m_ListBank.GetSelectedPayment();
        varBg.sizeDelta = new Vector2(varBg.sizeDelta.x, 1080f);
        varCPF.gameObject.SetActive(false);
        if (selectedPayment != null)
        {
            if (string.Equals(selectedPayment.payment, "PayPal", System.StringComparison.OrdinalIgnoreCase))
            {
                varFull.placeholder.GetComponent<TextMeshProUGUI>().text = GF.Localization.GetString("EnterPayPalaccount");
            }
            else if (string.Equals(selectedPayment.payment, "PagSeguro", System.StringComparison.OrdinalIgnoreCase))
            {
                varFull.placeholder.GetComponent<TextMeshProUGUI>().text = GF.Localization.GetString("EnterFullName");
                varCPF.placeholder.GetComponent<TextMeshProUGUI>().text = GF.Localization.GetString("EnterCPF");
                varBg.sizeDelta = new Vector2(varBg.sizeDelta.x, 1214f);
                varCPF.gameObject.SetActive(true);
            }
            else
            {
                varFull.placeholder.GetComponent<TextMeshProUGUI>().text = GF.Localization.GetString("Enteraccountnumber");
            }
        }
    }

    private void OnPaymentSelected(PaymentItemData paymentData)
    {
        Log.Info($"支付方式已选择: {paymentData.payment}，界面更新委托回调");

        // 更新输入框提示
        UpdateInputFieldPlaceholders();

        // 刷新列表显示选中状态
        // 注意：这里需要根据实际的UI组件名称进行调整
        if (varList != null)
        {
            varList.RefreshCells();
        }

        // 清空输入框内容（可选，根据需求决定）
        if (paymentData.payment != m_PlayerData.MakeupPaymentInfo.payment)
        {
            varFull.text = "";
            varCPF.text = "";
        }
        else
        {
            // 加载已保存的支付信息到输入框
            if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.accountId))
            {
                varFull.text = m_PlayerData.MakeupPaymentInfo.accountId;
            }
            if (!string.IsNullOrEmpty(m_PlayerData.MakeupPaymentInfo.cpf))
            {
                varCPF.text = m_PlayerData.MakeupPaymentInfo.cpf;
            }
        }
        varDesc.text = string.Format(GF.Localization.GetString("ArrowSelectPaymentUIForm.desc"), paymentData.payment);
    }


    private void OnFullMethodSubmit(string value)
    {
        Log.Info($"OnFullMethodSubmit: {value}");
        // 提交时校验
        ValidateInput();
    }

    private void OnCPFMethodSubmit(string value)
    {
        Log.Info($"OnCPFMethodSubmit: {value}");
        // 提交时校验
        if (!CheckCPFValidly(value))
        {
            GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips2"));
        }
    }
    private void ValidateInput()
    {
        PaymentItemData selectedPayment = m_ListBank.GetSelectedPayment();
        if (selectedPayment == null) return;

        bool isValid = true;
        string errorMessage = "";

        // 校验账号输入
        if (string.Equals(selectedPayment.payment, "PayPal", System.StringComparison.OrdinalIgnoreCase))
        {
            if (!CheckEMailValidly(varFull.text))
            {
                isValid = false;
                errorMessage = GF.Localization.GetString("InfoErrTips3");
            }
        }
        else if (string.Equals(selectedPayment.payment, "PagSeguro", System.StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(varFull.text))
            {
                isValid = false;
                errorMessage = GF.Localization.GetString("InfoErrTips4");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(varFull.text))
            {
                isValid = false;
                errorMessage = GF.Localization.GetString("InfoErrTips5");
            }
        }

        // 显示错误信息（可选）
        if (!isValid && !string.IsNullOrEmpty(errorMessage))
        {
            // 可以在这里显示错误提示
            GF.UI.ShowToast(errorMessage);
        }
    }

    // 检查邮箱
    public static bool CheckEMailValidly(string eMail)
    {
        //是否为空
        if (string.IsNullOrWhiteSpace(eMail))
        {
            return false;
        }

        //是否为邮箱格式
        const string expression =
            @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
        return Regex.IsMatch(eMail, expression);
    }
    // 检查CPF
    public static bool CheckCPFValidly(string cpf)
    {
        //是否为空
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return false;
        }
        //是否为cpf格式：123.456.789-09 或 12345678909
        const string expression = @"^(\d{3}\.){2}\d{3}-\d{2}$|^\d{11}$";
        return Regex.IsMatch(cpf, expression);
    }

    private bool CheckValidly()
    {
        PaymentItemData selectedPayment = m_ListBank.GetSelectedPayment();
        if (selectedPayment == null)
        {
            GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips6"));
            return false;
        }

        // 校验账号输入
        if (string.Equals(selectedPayment.payment, "PayPal", System.StringComparison.OrdinalIgnoreCase))
        {
            if (!CheckEMailValidly(varFull.text))
            {
                GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips3"));
                return false;
            }
        }
        else if (string.Equals(selectedPayment.payment, "PagSeguro", System.StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(varFull.text))
            {
                GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips4"));
                return false;
            }
            if (!CheckCPFValidly(varCPF.text))
            {
                GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips2"));
                return false;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(varFull.text))
            {
                GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips5"));
                return false;
            }
        }
        
        CommonHelper.LogEvent(AdjustEventCodeEvent.withdrawinfo_comfirm);
        // 使用PlayerDataModel的新方法更新支付信息
        m_PlayerData.UpdatePaymentInfo(selectedPayment.payment, varFull.text.Trim(), "", varCPF.text.Trim());

        Log.Info($"支付信息已保存: Payment={selectedPayment.payment}, Account={varFull.text}, CPF={varCPF.text}");

        GF.UI.ShowToast(GF.Localization.GetString("InfoErrTips7"));
        return true;
    }

    protected override void OnButtonClick(object sender, string btnId)
    {
        base.OnButtonClick(sender, btnId);
        switch (btnId)
        {
            case "Button_Close":
                GF.UI.Close(this.UIForm);
                break;
            case "Button_Confirm":
                if (CheckValidly())
                {
                    GF.UI.Close(this.UIForm);
                }
                break;
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        
        // 清理事件监听
        if (m_ListBank != null)
        {
            m_ListBank.OnPaymentSelected -= OnPaymentSelected;
        }

        // 清理输入框事件
        if (varFull != null)
        {
            varFull.onSubmit.RemoveListener(OnFullMethodSubmit);
        }

        if (varCPF != null)
        {
            varCPF.onSubmit.RemoveListener(OnCPFMethodSubmit);
        }
    }
}

// 自定义数据源类，用于连接PaymentListBank和LoopVerticalScrollRect
public class CustomPaymentDataSource : LoopScrollDataSource
{
    private ArrowPaymentListBank m_ListBank;

    public CustomPaymentDataSource(ArrowPaymentListBank listBank)
    {
        m_ListBank = listBank;
    }

    public void ProvideData(Transform transform, int idx)
    {
        if (m_ListBank != null)
        {
            var bankData = m_ListBank.GetLoopListBankData(idx);
            Log.Info($"ProvideData: idx={idx}, bankData={bankData}, listLength={m_ListBank.GetListLength()}");

            if (bankData != null)
            {
                // 调用PaymentItem的SetData方法
                var paymentItem = transform.GetComponent<ArrowPaymentItem>();
                Log.Info($"设置PaymentItem数据: {transform.gameObject.name} {idx}");
                if (paymentItem != null)
                {
                    paymentItem.SetData(bankData.Content, m_ListBank);
                }
                else
                {
                    Log.Error($"MahjongPaymentItem component not found on transform {transform.name}");
                }
            }
            else
            {
                Log.Warning($"BankData is null for index {idx}");
            }
        }
        else
        {
            Log.Error("m_ListBank为空");
        }

        // 同时调用ScrollCellIndex方法保持兼容性
        transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
    }
}


