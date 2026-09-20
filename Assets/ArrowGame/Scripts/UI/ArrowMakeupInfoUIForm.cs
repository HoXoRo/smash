using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Resource;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupInfoUIForm")]
public partial class ArrowMakeupInfoUIForm : UIFormBase
{
    private PlayerDataModel m_PlayerData;
    // private int MakeupCount;
    private MakeupType m_MakeupType;
    private int m_MakeupId;
    private Action m_StartMakeupAction;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        var uiParams = userData as UIParams;
        if (uiParams != null)
        {
            // MakeupCount = uiParams.Get<VarInt32>("MakeupCount").Value;
            m_MakeupType = (MakeupType)uiParams.Get<VarInt32>("MakeupType").Value;
            m_MakeupId = uiParams.Get<VarInt32>("MakeupId").Value;
            m_StartMakeupAction = uiParams.Get<VarAction>("StartMakeup").Value;
            Log.Info($"makeupType: {m_MakeupType}, makeupId: {m_MakeupId}");
        }
        Log.Info("OnOpen - UI初始化完成");
        UpdateUI();
    }
    private void UpdateUI()
    {
        varPaymentIcon.SetSprite($"UI/BankIcon/{m_PlayerData.MakeupPaymentInfo.payment}.png");
        if (m_PlayerData.MakeupPaymentInfo.payment == "PagSeguro")
        {
            varFullName.text = "Full Name";
            varFull.text = m_PlayerData.MakeupPaymentInfo.accountId;
            varCpf.text = m_PlayerData.MakeupPaymentInfo.cpf;
            varCPFName.gameObject.SetActive(true);
        }
        else
        {
            varFullName.text = "Account";
            varFull.text = m_PlayerData.MakeupPaymentInfo.accountId;
            varCPFName.gameObject.SetActive(false);
        }

    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Button_Continue":
                m_StartMakeupAction?.Invoke();
                GF.UI.Close(this.UIForm);
                break;
            case "Button_Rebind":
                GF.UI.OpenUIForm(UIViews.ArrowSelectPaymentUIForm);
                GF.UI.Close(this.UIForm);
                break;
        }
    }
}
