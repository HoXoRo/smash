using System;
using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class TaskItem : UIItemBase
{
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject claimedIndicator;

    private TaskReward task;
    private Action<TaskReward> onClaimRequested;
    private long adCooldownEnd;
    private string readyAdText;
    private float nextAdRefresh;

    public int TaskId { get; private set; }

    protected override void OnInit()
    {
        base.OnInit();
        varTaskProgress.interactable = false;
        readyAdText = varTxtAd.text;
        varBtn_ad.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClaimClicked);
        varBtn_claim.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClaimClicked);
    }

    public void SetData(TaskReward task, int completedLevels, bool claimed, long cooldownEnd, Action<TaskReward> claimHandler)
    {
        this.task = task;
        onClaimRequested = claimHandler;
        adCooldownEnd = cooldownEnd;
        TaskId = task.Id;
        bool isAdTask = task.TaskType == 1;
        string descriptionFormat = GF.Localization.GetString("TaskItem.infoTxt");
        if (string.IsNullOrWhiteSpace(descriptionFormat) || descriptionFormat == "TaskItem.infoTxt")
        {
            descriptionFormat = "Win {0} levels!";
        }

        descriptionText.text = isAdTask ? "Watch an ad" : string.Format(descriptionFormat, task.Level);
        varRewardTxt.text = task.Reward.ToString();
        varTaskProgress.minValue = 0f;
        varTaskProgress.maxValue = Mathf.Max(1, task.Level);
        varTaskProgress.wholeNumbers = true;
        varTaskProgress.SetValueWithoutNotify(Mathf.Clamp(completedLevels, 0, Mathf.Max(0, task.Level)));
        varTaskProgress.gameObject.SetActive(!isAdTask);
        claimedIndicator.SetActive(claimed);
        varInfoTxt.gameObject.SetActive(!isAdTask);
        varBtn_claim.SetActive(!isAdTask && !claimed);
        varAdTxt.SetActive(isAdTask);
        varBtn_ad.SetActive(isAdTask);
        RefreshAdCountdown();
    }

    public void ClearClaimHandler()
    {
        onClaimRequested = null;
        task = null;
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
        varTxtAd.text = remaining > 0 ? $"{remaining / 60:00}:{remaining % 60:00}" : readyAdText;
    }
}
