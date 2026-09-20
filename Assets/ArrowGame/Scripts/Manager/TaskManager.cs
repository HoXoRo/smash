using GameFramework.DataTable;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

public class TaskManager
{
    public TaskManager(bool isAutoInit = true)
    {
        if (isAutoInit)
        {
            Init();
        }
    }
    
    PlayerDataModel m_PlayerDataModel;
    private const float CHECK_INTERVAL = 30.0f; // 检查间隔（秒）
    private float m_LastCheckTime = 0f;
    
    public void Init()
    {
        m_PlayerDataModel = GF.DataModel.GetDataModel<PlayerDataModel>();
        AddEvent();
        
        // 添加定时检查任务
        TimerManager.Instance.AddTimer("TaskOnline", CHECK_INTERVAL, OnTaskCheck);
    }

    private void AddEvent()
    {
        // GF.Event.Subscribe(TaskUpdateEventArgs.EventId, OnTaskUpdate);
    }
    private int lastMakeDataCount = 0;
    /// <summary>
    /// 定时检查任务状态和倒计时
    /// </summary>
    private void OnTaskCheck()
    {
        if (m_PlayerDataModel == null ) return;
        // m_PlayerDataModel.OnlineTime += (int)CHECK_INTERVAL;
    }


    /// <summary>
    /// 销毁时清理资源
    /// </summary>
    public void Dispose()
    {
        TimerManager.Instance.RemoveTimer("TaskOnline");
    }

}
