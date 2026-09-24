using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameFramework.Event;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/TaskPageUIForm")]

public partial class TaskPageUIForm : UIFormBase
{
    [SerializeField] private TaskItem taskItemPrefab;
    [SerializeField] private ScrollRect taskScrollRect;

    private readonly List<TaskItem> taskItems = new List<TaskItem>();

    public IReadOnlyList<TaskItem> TaskItems => taskItems;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
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
        PlayerDataModel playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        int completedLevels = playerData != null ? Mathf.Max(0, playerData.LevelId - 1) : 0;
        for (int index = 0; index < taskItems.Count; index++)
        {
            taskItems[index].SetData(tasks[index], completedLevels);
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
        taskItems.Clear();
        base.OnClose(isShutdown, userData);
    }
}
