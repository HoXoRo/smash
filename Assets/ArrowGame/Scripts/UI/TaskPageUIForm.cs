using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using System;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/TaskPageUIForm")]

public partial class TaskPageUIForm : UIFormBase
{
    public const string P_FromHallTab = "FromHallTab";

    [SerializeField] private TaskItem taskItemPrefab;

    private readonly List<TaskItem> taskItems = new List<TaskItem>();
    private static bool s_AdRequestInProgress;
    private DateTime m_VisibleTaskDay;
    private float m_NextDayCheck;

    public IReadOnlyList<TaskItem> TaskItems => taskItems;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ArrowUITopbar.SetPageHidden(this, true);
        varBtnClose.gameObject.SetActive(!Params.Get<VarBoolean>(P_FromHallTab, false));
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        GF.Event.Subscribe(TaskUpdateEventArgs.EventId, OnTaskUpdated);
        RefreshTaskList();

        if (varTaskScrollView != null && varTaskScrollView.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(varTaskScrollView.content);
            varTaskScrollView.StopMovement();
            varTaskScrollView.verticalNormalizedPosition = 1f;
        }
    }

    public void RefreshTaskList()
    {
        var table = GF.DataTable.GetDataTable<TaskReward>();
        if (table == null)
        {
            Log.Error("TaskPageUIForm: TaskReward data table is not loaded.");
            RefreshTaskList(0);
            return;
        }

        TaskReward[] tasks = table.GetAllDataRows();
        System.Array.Sort(tasks, (left, right) =>
        {
            int levelComparison = left.Level.CompareTo(right.Level);
            return levelComparison != 0 ? levelComparison : left.Id.CompareTo(right.Id);
        });

        RefreshTaskList(tasks.Length);
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        int completedLevels = playerData.DailyCompletedLevels;
        m_VisibleTaskDay = DateTime.UtcNow.Date;
        for (int index = 0; index < taskItems.Count; index++)
        {
            TaskReward task = tasks[index];
            taskItems[index].SetData(task, completedLevels,
                task.TaskType == 2 && playerData.IsLevelTaskClaimed(task.Id),
                playerData.GetTaskAdCooldownEnd(task.Id), OnClaimRequested, OnClickClose);
        }
    }

    private void OnClaimRequested(TaskReward task)
    {
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (s_AdRequestInProgress || GF.UI.HasUIForm(UIViews.CoinRewardUIForm) ||
            GF.UI.IsLoadingUIForm(UIViews.CoinRewardUIForm)) return;
        if (task.Reward <= 0)
        {
            GF.UI.ShowToast("Not enough");
            return;
        }

        if (task.TaskType == 2)
        {
            if (playerData.DailyCompletedLevels < task.Level || playerData.IsLevelTaskClaimed(task.Id))
            {
                GF.UI.ShowToast("Not enough");
                return;
            }
            OpenCoinReward(task.Id, false);
            return;
        }

        if (task.TaskType != 1 ||
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() < playerData.GetTaskAdCooldownEnd(task.Id))
        {
            GF.UI.ShowToast("Not enough");
            return;
        }

        s_AdRequestInProgress = true;
        int taskId = task.Id;
        CommonHelper.ShowVideoAd("task_coin", success =>
        {
            s_AdRequestInProgress = false;
            if (!success)
            {
                GF.UI.ShowToast("Not enough");
                return;
            }
            OpenCoinReward(taskId, true);
        }, showFailureToast: false);
    }

    private static void OpenCoinReward(int taskId, bool adCompleted)
    {
        if (GF.UI.HasUIForm(UIViews.CoinRewardUIForm) ||
            GF.UI.IsLoadingUIForm(UIViews.CoinRewardUIForm)) return;
        UIParams uiParams = UIParams.Create();
        uiParams.Set<VarInt32>(CoinRewardUIForm.P_TaskId, taskId);
        uiParams.Set<VarBoolean>(CoinRewardUIForm.P_AdCompleted, adCompleted);
        GF.UI.OpenUIForm(UIViews.CoinRewardUIForm, uiParams);
    }

    private void OnPlayerDataChanged(object sender, GameEventArgs eventArgs)
    {
        if (eventArgs is PlayerDataChangedEventArgs changed &&
            (changed.DataType == PlayerDataType.LevelId || changed.DataType == PlayerDataType.Coins))
        {
            RefreshTaskList();
        }
    }

    private void OnTaskUpdated(object sender, GameEventArgs eventArgs)
    {
        RefreshTaskList();
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (Time.unscaledTime < m_NextDayCheck) return;
        m_NextDayCheck = Time.unscaledTime + 1f;
        if (DateTime.UtcNow.Date != m_VisibleTaskDay) RefreshTaskList();
    }

    private void RefreshTaskList(int taskCount)
    {
        if (taskItemPrefab == null || varTaskScrollView == null || varTaskScrollView.content == null)
        {
            Log.Error("TaskPageUIForm: Task item prefab, scroll rect and content must be assigned.");
            return;
        }

        taskCount = Mathf.Max(0, taskCount);
        for (int index = taskItems.Count - 1; index >= taskCount; index--)
        {
            taskItems[index].ClearClaimHandler();
            UnspawnItem<UIItemObject>(taskItemPrefab.gameObject, taskItems[index].gameObject);
            taskItems.RemoveAt(index);
        }

        while (taskItems.Count < taskCount)
        {
            UIItemObject itemObject = SpawnItem<UIItemObject>(taskItemPrefab.gameObject, varTaskScrollView.content);
            TaskItem taskItem = (TaskItem)itemObject.itemLogic;
            taskItem.transform.localScale = Vector3.one;
            taskItem.transform.localRotation = Quaternion.identity;
            taskItem.transform.localPosition = Vector3.zero;
            taskItem.transform.SetAsLastSibling();
            taskItem.gameObject.SetActive(true);
            taskItems.Add(taskItem);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ArrowUITopbar.SetPageHidden(this, false);
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        GF.Event.Unsubscribe(TaskUpdateEventArgs.EventId, OnTaskUpdated);
        foreach (TaskItem item in taskItems) item.ClearClaimHandler();
        taskItems.Clear();
        base.OnClose(isShutdown, userData);
    }
}
