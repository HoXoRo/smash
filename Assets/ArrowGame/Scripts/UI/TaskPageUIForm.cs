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
    [SerializeField] private ScrollRect taskScrollRect;

    private readonly List<TaskItem> taskItems = new List<TaskItem>();
    private static bool s_AdRequestInProgress;

    public IReadOnlyList<TaskItem> TaskItems => taskItems;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        varBtnClose.gameObject.SetActive(!Params.Get<VarBoolean>(P_FromHallTab, false));
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        RefreshTaskList();

        if (taskScrollRect != null && taskScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(taskScrollRect.content);
            taskScrollRect.StopMovement();
            taskScrollRect.verticalNormalizedPosition = 1f;
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
        int completedLevels = Mathf.Max(0, playerData.LevelId - 1);
        for (int index = 0; index < taskItems.Count; index++)
        {
            TaskReward task = tasks[index];
            taskItems[index].SetData(task, completedLevels,
                task.TaskType == 2 && playerData.IsLevelTaskClaimed(task.Id),
                playerData.GetTaskAdCooldownEnd(task.Id), OnClaimRequested);
        }
    }

    private void OnClaimRequested(TaskReward task)
    {
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        if (task.TaskType == 2)
        {
            if (!playerData.TryClaimLevelTask(task.Id, task.Level, task.Reward))
            {
                GF.UI.ShowToast("Not enough");
                return;
            }
            RefreshTaskList();
            return;
        }

        if (task.TaskType != 1 || s_AdRequestInProgress ||
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() < playerData.GetTaskAdCooldownEnd(task.Id))
        {
            GF.UI.ShowToast("Not enough");
            return;
        }

        s_AdRequestInProgress = true;
        int taskId = task.Id;
        int reward = task.Reward;
        if (!playerData.TryClaimTaskAdReward(taskId, reward, DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            GF.UI.ShowToast("Not enough");
        }
        else
        {
            CommonHelper.ShowVideoAd("task_coin", success =>
            {
                s_AdRequestInProgress = false;
                if (!success)
                {
                    return;
                }
                if (this != null && gameObject.activeInHierarchy) RefreshTaskList();
            });
        }

        
    }

    private void OnPlayerDataChanged(object sender, GameEventArgs eventArgs)
    {
        if (eventArgs is PlayerDataChangedEventArgs changed && changed.DataType == PlayerDataType.LevelId)
        {
            RefreshTaskList();
        }
    }

    private void RefreshTaskList(int taskCount)
    {
        if (taskItemPrefab == null || taskScrollRect == null || taskScrollRect.content == null)
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
            UIItemObject itemObject = SpawnItem<UIItemObject>(taskItemPrefab.gameObject, taskScrollRect.content);
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
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        foreach (TaskItem item in taskItems) item.ClearClaimHandler();
        taskItems.Clear();
        base.OnClose(isShutdown, userData);
    }
}
