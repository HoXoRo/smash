using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/PaymentItem")]
public partial class ArrowPaymentItem : UIItemBase
{
    public string payment;
    private ArrowPaymentListBank m_ListBank;
    private PaymentItemData m_ItemData;
    private bool m_Interactable = true;
    
    protected override void OnInit()
    {
        base.OnInit();
        varButton.onClick.AddListener(OnClick);
    }
    
    public void SetData(object data, ArrowPaymentListBank listBank)
    {
        SetData(data, listBank, true);
    }

    public void SetData(object data, ArrowPaymentListBank listBank, bool interactable)
    {
        Log.Info($"SetData: {data}");
        m_Interactable = interactable;
        if (data is PaymentItemData itemData)
        {
            m_ListBank = listBank;
            m_ItemData = itemData;
            UpdateUI();
        }
    }
    
    public void UpdateUI()
    {
        if (m_ItemData == null) return;
        
        payment = m_ItemData.payment;
        Log.Info($"加载提现方式图标，Assets/MahjongGame/Sprites/UI/BankIcon/{payment}.png");
        varIcon.SetSprite($"UI/BankIcon/{payment}.png");
        ApplyInteractableState();
        // 更新选中状态
        UpdateSelectedState();
    }

    private void ApplyInteractableState()
    {
        if (varButton != null)
        {
            varButton.interactable = m_Interactable;
        }
    }
    
    private void UpdateSelectedState()
    {
        if (!m_Interactable)
        {
            if (varSelectIcon != null)
                varSelectIcon.gameObject.SetActive(false);
            return;
        }

        if (m_ListBank == null || m_ItemData == null) return;
        
        // 检查当前项是否被选中
        bool isSelected = m_ListBank.IsPaymentSelected(m_ItemData.payment);
        
        // 显示或隐藏选中图标
        if (varSelectIcon != null)
        {
            varSelectIcon.gameObject.SetActive(isSelected);
        }
        
        // 更新按钮状态（可选）
        if (varButton != null)
        {
            // 可以根据选中状态改变按钮外观
            // var buttonImage = varButton.GetComponent<Image>();
            // if (buttonImage != null)
            // {
            //     // 选中时改变颜色
            //     buttonImage.color = isSelected ? Color.green : Color.white;
            // }
        }
        
        // 更新文本颜色（可选）
        // 注意：如果varText不存在，可以注释掉这部分代码
        // if (varText != null)
        // {
        //     varText.color = isSelected ? Color.green : Color.black;
        // }
        
        Log.Info($"PaymentItem {m_ItemData.payment} 选中状态: {isSelected}");
    }
    
    public void ScrollCellIndex(int idx)
    {
        gameObject.name = $"PaymentItem_{idx}";
        
        // 当滚动时更新选中状态
        UpdateSelectedState();
    }
    
    private void OnClick()
    {
        if (!m_Interactable)
            return;

        if (m_ListBank != null && m_ItemData != null)
        {
            Log.Info($"点击支付方式: {m_ItemData.payment}");
            m_ListBank.SetSelectedPayment(m_ItemData.payment);
        }
    }
}