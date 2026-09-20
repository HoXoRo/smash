using ArrowMaze;
using GameFramework.DataTable;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

public class MakeupSys
{
    public MakeupSys(bool isAutoInit = true)
    {
        if (isAutoInit)
        {
            Init();
        }
    }

    PlayerDataModel m_PlayerDataModel;
    private const float CHECK_INTERVAL = 1.0f; // 检查间隔（秒）
    private float m_LastCheckTime = 0f;
    private int WithdrawalNumChange_Interval = 180;

    public void Init()
    {
        m_PlayerDataModel = GF.DataModel.GetDataModel<PlayerDataModel>();
        AddEvent();
        // 添加定时检查任务
        // TimerManager.Instance.AddTimer("MakeupTaskCheck", CHECK_INTERVAL, OnTaskCheck);
        // TimerManager.Instance.AddTimer("WithdrawalNumChange", WithdrawalNumChange_Interval, OnWithdrawalNumChange);
        OnTaskCheck();
        OnWithdrawalNumChange();
    }

    private void AddEvent()
    {
        GF.Event.Subscribe(WatchVideoFinishEventArgs.EventId, OnWatchVideoFinish);
        GF.Event.Subscribe(MakeupTaskChangeEventArgs.EventId, OnMakeupTaskChange);
    }
    private void OnWithdrawalNumChange()
    {
        int changeNum = UnityEngine.Random.Range(10, 50);
        m_PlayerDataModel.TodayWithdrawalNum += changeNum;
        m_PlayerDataModel.Save();
    }
    /// <summary>
    /// 定时检查任务状态和倒计时
    /// </summary>
    private void OnTaskCheck()
    {
        if (m_PlayerDataModel == null || m_PlayerDataModel.makeupDatas == null || m_PlayerDataModel.makeupDatas.Count == 0)
        {
            Log.Info($"提现任务数量为空,开始初始化提现任务");
            MakeupData makeupData = null;
            var banknoteTable = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>();
            var banknoteRows = banknoteTable.GetDataRow(1);
            makeupData = new MakeupData(banknoteRows);
            makeupData.step1TaskNum = ArrowLevelProgressUtility.GetCompletedMainLevelCount(m_PlayerDataModel.LevelId);
            // // 使用最新的支付信息
            // makeupData.paymentInfo.payment = m_PlayerDataModel.MakeupPaymentInfo.payment;
            // makeupData.paymentInfo.accountId = m_PlayerDataModel.MakeupPaymentInfo.accountId;
            // makeupData.paymentInfo.email = m_PlayerDataModel.MakeupPaymentInfo.email;
            // makeupData.paymentInfo.cpf = m_PlayerDataModel.MakeupPaymentInfo.cpf;
            m_PlayerDataModel.AddMakeupData(makeupData);
            Log.Info($"提现任务初始化完成");
            return;
        }

        SyncStep1LevelTaskProgress();
    }

    /// <summary>
    /// 同步 Step1 关卡任务进度（按已完成主关卡数，而非当前所在主关卡）。
    /// </summary>
    private void SyncStep1LevelTaskProgress()
    {
        int completedLevels = ArrowLevelProgressUtility.GetCompletedMainLevelCount(m_PlayerDataModel.LevelId);
        foreach (var makeupData in m_PlayerDataModel.makeupDatas)
        {
            if (makeupData.makeupType != MakeupType.Mon || makeupData.makeupStep != MakeupStep.Step1)
                continue;

            var config = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>().GetDataRow(makeupData.id);
            if (config == null || makeupData.step1TaskNum >= config.Step1_Lv)
                continue;

            if (makeupData.step1TaskNum != completedLevels)
            {
                makeupData.step1TaskNum = completedLevels;
                m_PlayerDataModel.UpdateMakeupData(makeupData);
            }
        }
    }

    /// <summary>
    /// 检查并更新任务步骤
    /// </summary>
    private bool CheckAndUpdateStep(MakeupData makeupData)
    {
        bool hasUpdate = false;

        switch (makeupData.makeupStep)
        {
            case MakeupStep.Step1:
                hasUpdate = CheckStep1Progress(makeupData);
                break;
            case MakeupStep.Step2:
                hasUpdate = CheckStep2Progress(makeupData);
                break;
        }

        return hasUpdate;
    }

    /// <summary>
    /// 检查Step1进度（合成数）
    /// </summary>
    private bool CheckStep1Progress(MakeupData makeupData)
    {
        // 检查是否满足任务要求
        bool canNext = false;
        if (makeupData.makeupType == MakeupType.Mon)
        {
            var config = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>().GetDataRow(makeupData.id);
            canNext = config != null && makeupData.step1TaskNum >= config.Step1_Lv;
        }
        if (canNext)
        {
            // makeupData.TurnNextStep();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检查Step2进度（广告数）
    /// </summary>
    private bool CheckStep2Progress(MakeupData makeupData)
    {
        // 检查是否满足任务要求
        bool canNext = false;
        if (makeupData.makeupType == MakeupType.Mon)
        {
            var config = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>().GetDataRow(makeupData.id);
            canNext = config != null && makeupData.step2TaskNum >= config.Step2_Mon;
        }
        if (canNext)
        {
            // makeupData.TurnNextStep();
            return true;
        }
        return false;
    }
    

    private void OnMakeupTaskChange(object sender, GameEventArgs e)
    {
        IDataTable<ArrowMakeupBanknotes> dtMakeupBanknote = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>();
        MakeupTaskChangeEventArgs makeupTaskChangeEventArgs = e as MakeupTaskChangeEventArgs;
        float value = makeupTaskChangeEventArgs.Value;
        ArrowMakeupBanknotes drMakeupBanknote = null;
        bool isUpdate = false;
        foreach (var makeupData in m_PlayerDataModel.makeupDatas)
        {
            if (makeupData.makeupType == MakeupType.Mon)
            {
                if (makeupData.makeupStep == MakeupStep.Step1)
                {
                    drMakeupBanknote = dtMakeupBanknote.GetDataRow(makeupData.id);
                    if (makeupData.step1TaskNum < drMakeupBanknote.Step1_Lv)
                    {
                        makeupData.step1TaskNum = value > 0
                            ? (int)value
                            : ArrowLevelProgressUtility.GetCompletedMainLevelCount(m_PlayerDataModel.LevelId);
                        // 使用PlayerDataModel的新方法更新提现数据
                        m_PlayerDataModel.UpdateMakeupData(makeupData);

                        if (makeupData.step1TaskNum == drMakeupBanknote.Step1_Lv)
                        {
                            CommonHelper.LogEvent(AdjustEventCodeEvent.withdraw_step1);
                        }
                    }
                }
            }

        }

    }
    private void OnWatchVideoFinish(object sender, GameEventArgs e)
    {
        IDataTable<ArrowMakeupBanknotes> MakeupBanknotesDT = GF.DataTable.GetDataTable<ArrowMakeupBanknotes>();
        ArrowMakeupBanknotes drMakeupBanknote = null;

        bool isUpdate = false;
        foreach (var makeupData in m_PlayerDataModel.makeupDatas)
        {
            if (makeupData.makeupStep == MakeupStep.Step2)
            {
                if (makeupData.makeupType == MakeupType.Mon)
                {
                    drMakeupBanknote = MakeupBanknotesDT.GetDataRow(makeupData.id);
                    if (makeupData.step2TaskNum < drMakeupBanknote.Step2_Mon)
                    {
                        makeupData.step2TaskNum++;
                        isUpdate = true;

                        // 使用PlayerDataModel的新方法更新提现数据
                        m_PlayerDataModel.UpdateMakeupData(makeupData);
                        GF.Event.Fire(null, MakeupTaskChangeEventArgs.Create());
                    }
                }
            }
        }
    }

    /// <summary>
    /// 销毁时清理资源
    /// </summary>
    public void Dispose()
    {
        TimerManager.Instance.RemoveTimer("MakeupTaskCheck");
        GF.Event.Unsubscribe(WatchVideoFinishEventArgs.EventId, OnWatchVideoFinish);
        GF.Event.Unsubscribe(MakeupTaskChangeEventArgs.EventId, OnMakeupTaskChange);
    }
    public int GetRandomQueue()
    {
        int[] arr = GF.Config.GetArray<int>("MakeupQueueReduce");
        int value = 0;
        if (arr != null)
        {
            value = CommonHelper.RandomInt(arr);
        }
        return value;
    }
}
