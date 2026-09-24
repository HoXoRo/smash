using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class TaskItem : UIItemBase
{
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    [SerializeField] private TMPro.TextMeshProUGUI rewardText;
    [SerializeField] private UnityEngine.UI.Slider progressSlider;
    [SerializeField] private GameObject claimedIndicator;

    public int TaskId { get; private set; }

    protected override void OnInit()
    {
        base.OnInit();
        progressSlider.interactable = false;
    }

    public void SetData(TaskReward task, int completedLevels)
    {
        TaskId = task.Id;
        string descriptionFormat = GF.Localization.GetString("TaskItem.infoTxt");
        if (string.IsNullOrWhiteSpace(descriptionFormat) || descriptionFormat == "TaskItem.infoTxt")
        {
            descriptionFormat = "Win {0} levels!";
        }

        descriptionText.text = string.Format(descriptionFormat, task.Level);
        rewardText.text = task.Reward.ToString();
        progressSlider.minValue = 0f;
        progressSlider.maxValue = Mathf.Max(1, task.Level);
        progressSlider.wholeNumbers = true;
        progressSlider.SetValueWithoutNotify(Mathf.Clamp(completedLevels, 0, Mathf.Max(0, task.Level)));
        claimedIndicator.SetActive(false);
        bool isAdTask = task.TaskType == 1;
        varInfoTxt.gameObject.SetActive(!isAdTask);
        varBtn_claim.SetActive(!isAdTask);
        varAdTxt.SetActive(isAdTask);
        varBtn_ad.SetActive(isAdTask);
    }
}
