using System;
using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class TaskItem : UIItemBase
{

    private TaskReward task;
    private Action<TaskReward> onClaimRequested;
    private Action onGoRequested;
    private long adCooldownEnd;
    private string readyAdText;
    private UnityEngine.UI.Button adButton;
    private float nextAdRefresh;

    public int TaskId { get; private set; }

    protected override void OnInit()
    {
        base.OnInit();
        varTaskProgress.interactable = false;
        readyAdText = varTxtAd.text;
        adButton = varBtn_ad.GetComponent<UnityEngine.UI.Button>();
        adButton.onClick.AddListener(OnClaimClicked);
        varBtn_claim.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClaimClicked);
        varBtn_go.onClick.AddListener(OnGoClicked);
    }

    public void SetData(TaskReward task, int completedLevels, bool claimed, long cooldownEnd, Action<TaskReward> claimHandler, Action goHandler)
    {
        this.task = task;
        onClaimRequested = claimHandler;
        onGoRequested = goHandler;
        adCooldownEnd = cooldownEnd;
        TaskId = task.Id;
        bool isAdTask = task.TaskType == 1;
        string descriptionFormat = GF.Localization.GetString("TaskItem.infoTxt");
        if (string.IsNullOrWhiteSpace(descriptionFormat) || descriptionFormat == "TaskItem.infoTxt")
        {
            descriptionFormat = "Win {0} levels!";
        }

        if (isAdTask)
        {
            varInfoTxt.text  = string.Format(descriptionFormat, completedLevels);
        }

        varRewardTxt.text = task.Reward.ToString();
        varTaskProgress.minValue = 0f;
        varTaskProgress.maxValue = Mathf.Max(1, task.Level);
        varTaskProgress.wholeNumbers = true;
        int targetProgress = Mathf.Max(0, task.Level);
        int currentProgress = Mathf.Clamp(completedLevels, 0, targetProgress);
        varTaskProgress.SetValueWithoutNotify(currentProgress);
        varTxtTaskProgress.text = $"{currentProgress}/{targetProgress}";
        varClaimed.SetActive(claimed);
        varInfoTxt.gameObject.SetActive(!isAdTask);
        bool isLevelTask = task.TaskType == 2;
        bool isLevelTaskComplete = completedLevels >= task.Level;
        varBtn_go.gameObject.SetActive(isLevelTask && !isLevelTaskComplete && !claimed);
        varBtn_claim.SetActive(isLevelTask && isLevelTaskComplete && !claimed);
        varAdTxt.SetActive(isAdTask);
        varBtn_ad.SetActive(isAdTask);
        RefreshAdCountdown();
    }

    public void ClearClaimHandler()
    {
        onClaimRequested = null;
        onGoRequested = null;
        task = null;
    }

    private void OnGoClicked()
    {
        if (task != null) onGoRequested?.Invoke();
    }

    private void OnClaimClicked()
    {
        if (task != null) onClaimRequested?.Invoke(task);
    }

    private void Update()
    {
        if (task == null || task.TaskType != 1 || Time.unscaledTime < nextAdRefresh) return;
        nextAdRefresh = Time.unscaledTime + 0.25f;
        RefreshAdCountdown();
    }

    private void RefreshAdCountdown()
    {
        if (task == null || task.TaskType != 1) return;
        long remaining = Math.Max(0, adCooldownEnd - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        adButton.interactable = remaining == 0;
        varTxtAd.text = remaining > 0 ? $"{remaining / 60:00}:{remaining % 60:00}" : readyAdText;
    }
}
