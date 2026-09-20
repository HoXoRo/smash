using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
public class ArrowLanguageListBank : LoopListBankBase
{
    private List<LoopListBankData> m_DataList = new List<LoopListBankData>();
    // 选中事件
    private void Awake()
    {
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

        var languageCfg = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
        var languageRows = languageCfg.GetAllDataRows();
        foreach (var row in languageRows)
        {
            var loopData = new LoopListBankData
            {
                Content = row.LanguageKey,
                UniqueID = row.LanguageKey
            };
            m_DataList.Add(loopData);
        }
    }



}

public class LanguageData
{
    public int Index { get; set; }
    public int Id { get; set; }
    public bool IsSelected { get; set; }
}
