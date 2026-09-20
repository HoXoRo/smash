using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using GameFramework.Event;
using System.Linq;
using UnityGameFramework.Runtime;

public class ArrowMakeupListBank : LoopListBankBase
{
    private List<LoopListBankData> m_DataList = new List<LoopListBankData>();
    private MakeupType m_CurrentType;
    private PlayerDataModel m_PlayerData;
    private Dictionary<MakeupType, int> m_CurrentMaxId = new Dictionary<MakeupType, int>();

    // 支付信息变更事件
    public static System.Action OnPaymentInfoChanged;

    private void Awake()
    {
        m_PlayerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        InitializeMaxIds();
        
        // 监听支付信息变更事件
        // GF.Event.Subscribe(PaymentInfoChangedEventArgs.EventId, OnPaymentInfoChangedEvent);
        // GF.Event.Subscribe(MakeupDataChangedEventArgs.EventId, OnMakeupDataChangedEvent);
    }

    // 通知支付信息变更
    public static void NotifyPaymentInfoChanged()
    {
        Log.Info("支付信息已变更，通知相关界面更新");
        OnPaymentInfoChanged?.Invoke();
    }


    private void InitializeMaxIds()
    {
        // 初始化每种类型的最大ID
        m_CurrentMaxId[MakeupType.Mon] = 1;
        m_CurrentMaxId[MakeupType.Coin] = 1;

        // 根据已有提现数据更新最大ID
        foreach (var makeupData in m_PlayerData.makeupDatas)
        {
            // 只有当提现数据进入Step1状态时，才解锁下一档
            if ((int)makeupData.makeupStep >= (int)MakeupStep.Step1 && makeupData.id >= m_CurrentMaxId[makeupData.makeupType])
            {
                m_CurrentMaxId[makeupData.makeupType] = makeupData.id + 1;
            }
        }
        Log.Info($"m_CurrentMaxId: {m_CurrentMaxId[MakeupType.Mon]}, {m_CurrentMaxId[MakeupType.Coin]}");
    }

    public void SetMakeupType(MakeupType type)
    {
        // if (m_CurrentType == type) return;
        m_CurrentType = type;
        RefreshData();
    }

    private void RefreshData()
    {
        m_DataList.Clear();

        switch (m_CurrentType)
        {
            case MakeupType.Mon:
                RefreshMoneyData();
                break;
        }

        Log.Info($"RefreshData - 当前类型: {m_CurrentType}, 数据数量: {m_DataList.Count}");
    }

    private void RefreshMoneyData()
    {
        var banknotesTable = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>();
        var banknotesRows = banknotesTable.GetAllDataRows();
        var currentMaxId = m_CurrentMaxId[MakeupType.Mon];
        var collectTask = m_PlayerData.CollectCount;

        Log.Info($"RefreshMoneyData - 总配置数: {banknotesRows.Count()}, 当前最大ID: {currentMaxId}, 玩家美元: {collectTask}");

        // 获取当前可显示的配置数据
        var availableConfigs = banknotesRows.Where(row =>
        {   //(修改为只有第一档)
            if (row.Id == 1) return true;
            return false;
        }).ToList();

        Log.Info($"RefreshMoneyData - 可用配置数: {availableConfigs.Count}");

        foreach (var row in availableConfigs)
        {
            var makeupData = m_PlayerData.makeupDatas.Find(x => x.makeupType == MakeupType.Mon && x.id == row.Id);
            var itemData = new MakeupItemData
            {
                Index = row.Id - 1,
                Type = MakeupType.Mon,
                Config = row,
                MakeupData = makeupData,
                IsUnlocked = row.Id <= currentMaxId,
                // 完成任意一个任务
                CanMakeup = makeupData != null && 
                            ((makeupData.makeupStep == MakeupStep.Step1 && makeupData.step1TaskNum >= row.Step1_Lv) 
                            || (makeupData.makeupStep == MakeupStep.Step2 && m_PlayerData.Dollars >= CommonHelper.GetStep2TargetStored(makeupData, row)))
            };
            
            Log.Info($"当前任务：{makeupData.makeupStep} 任务进度1：{makeupData.step1TaskNum}/{row.Step1_Lv}  任务进度2：{m_PlayerData.Dollars}/{CommonHelper.GetStep2TargetStored(makeupData, row)}");

            var data = new LoopListBankData
            {
                Content = itemData,
                UniqueID = $"Money_{row.Id}"
            };

            m_DataList.Add(data);
            Log.Info($"添加Money数据: ID={row.Id}, 需求={row.Step1_Lv}, 可提现={itemData.CanMakeup}");
        }

        Log.Info($"RefreshMoneyData完成 - 数据列表数量: {m_DataList.Count}");
    }

    // 开始提现操作（点击Makeup按钮后调用）
    public void StartMakeup(MakeupType type, int id)
    {
        // 检查是否已经存在该档位的提现数据
        var existingData = m_PlayerData.makeupDatas.Find(x => x.makeupType == type && x.id == id);
        if (existingData != null)
        {
            if (type == MakeupType.Mon)
            {
                if (existingData.makeupStep == MakeupStep.Step1)
                {
                    var banknoteTable = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>();
                    var banknoteRows = banknoteTable.GetDataRow(id);
                    // 使用最新的支付信息
                    existingData.paymentInfo.payment = m_PlayerData.MakeupPaymentInfo.payment;
                    existingData.paymentInfo.accountId = m_PlayerData.MakeupPaymentInfo.accountId;
                    existingData.paymentInfo.email = m_PlayerData.MakeupPaymentInfo.email;
                    existingData.paymentInfo.cpf = m_PlayerData.MakeupPaymentInfo.cpf;
                    
                    existingData.TurnNextStep();
                }
            }
            
            // if (makeupData != null)
            // {
            //     // 使用PlayerDataModel的新方法添加提现数据
            //     // m_PlayerData.AddMakeupData(makeupData);
            //
            //     // 如果当前ID等于最大ID，解锁下一档
            //     if (id == m_CurrentMaxId[type])
            //     {
            //         m_CurrentMaxId[type] = id + 1;
            //     }
            // }
        }
  
        
        // 刷新列表显示
        // RefreshData();
        
        // 通知支付信息变更，确保所有相关界面都能更新
        NotifyPaymentInfoChanged();
    }

    /// <summary>
    /// 通知UI更新
    /// </summary>
    private void NotifyUIUpdate()
    {
        // 触发支付信息变更事件，通知UI更新
        NotifyPaymentInfoChanged();
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
}

public class MakeupItemData
{
    public int Index { get; set; }
    public MakeupType Type { get; set; }
    public ArrowMakeupBanknotes Config { get; set; }
    // public MahjongMakeupCoin CoinConfig { get; set; }
    public MakeupData MakeupData { get; set; }
    public bool IsUnlocked { get; set; }  // 是否已解锁
    public bool CanMakeup { get; set; }  // 是否可以开始提现
}
 