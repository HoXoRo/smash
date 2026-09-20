using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
public class ArrowMakeupOrdemListBank : LoopListBankBase
{
    public List<MakeupData> m_PaymentList = new List<MakeupData>();
    private PlayerDataModel m_PlayerData;
    private List<LoopListBankData> m_DataList = new List<LoopListBankData>();
    // 选中事件
    private void Awake()
    {
        m_PlayerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        InitPaymentData();
    }
    public void InitPaymentData()
    {
        m_PaymentList.Clear();
        var makeupDatas = m_PlayerData.makeupDatas.FindAll(x =>( x.makeupStep == MakeupStep.Fail||x.makeupStep == MakeupStep.Success));
        if(makeupDatas!=null)
        {
            foreach(var makeupData in makeupDatas)
            {
                m_PaymentList.Add(makeupData);
            }
        }
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
        return new Vector2(0, 120); // 固定高度，宽度自适应
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



}

