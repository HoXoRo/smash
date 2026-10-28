using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/CoinRewardUIForm")]
public partial class CoinRewardUIForm : UIFormBase
{
    public const string P_TaskId = "TaskId";
    public const string P_AdCompleted = "AdCompleted";
    private const float RewardRollDuration = 0.8f;

    private TaskReward m_Task;
    private bool m_AdCompleted;
    private bool m_IsClaiming;
    private int m_RequestVersion;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        varBtnClaim.onClick.AddListener(OnClaimClicked);
        varBtnAd.onClick.AddListener(OnAdClicked);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.LogPopup(nameof(CoinRewardUIForm));
        m_RequestVersion++;
        m_IsClaiming = false;
        int taskId = Params.Get<VarInt32>(P_TaskId)?.Value ?? 0;
        m_AdCompleted = Params.Get<VarBoolean>(P_AdCompleted)?.Value ?? false;
        m_Task = GF.DataTable.GetDataTable<TaskReward>()?.GetDataRow(taskId);

        varRewardCount.text = (m_Task?.Reward ?? 0).ToString();
        varTxtMulit.text = $"X{m_Task?.RewardMulti ?? 1}";
        varBtnClaim.interactable = m_Task != null;
        varBtnAd.gameObject.SetActive(m_Task != null && m_Task.TaskType == 2);
        varBtnAd.interactable = true;
        
        GF.Sound.PlayEffect("Coin_reward.mp3");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopAllCoroutines();
        m_RequestVersion++;
        m_Task = null;
        base.OnClose(isShutdown, userData);
    }

    private void OnClaimClicked()
    {
        if (m_IsClaiming) return;
        ClaimReward(1);
    }

    private void OnAdClicked()
    {
        if (m_IsClaiming || m_Task == null || m_Task.TaskType != 2) return;
        m_IsClaiming = true;
        varBtnClaim.interactable = false;
        varBtnAd.interactable = false;
        int requestVersion = m_RequestVersion;
        CommonHelper.ShowVideoAd("task2", success =>
        {
            if (this == null || requestVersion != m_RequestVersion) return;
            if (success)
            {
                int targetReward = (int)Math.Min(int.MaxValue, (long)m_Task.Reward * Mathf.Max(1, m_Task.RewardMulti));
                StartCoroutine(RollRewardAndClaim(targetReward));
                return;
            }
            m_IsClaiming = false;
            varBtnClaim.interactable = true;
            varBtnAd.interactable = true;
            GF.UI.ShowToast("Not enough");
        }, showFailureToast: false);
    }

    private IEnumerator RollRewardAndClaim(int targetReward)
    {
        int startReward = m_Task.Reward;
        yield return CommonHelper.RollNumber(startReward, targetReward, RewardRollDuration,
            value => varRewardCount.text = Mathf.RoundToInt(value).ToString());
        varRewardCount.text = targetReward.ToString();
        ClaimReward(Mathf.Max(1, m_Task.RewardMulti));
    }

    private void ClaimReward(int multiplier)
    {
        if (m_Task == null) return;
        m_IsClaiming = true;
        varBtnClaim.interactable = false;
        varBtnAd.interactable = false;

        int reward = (int)Math.Min(int.MaxValue, (long)m_Task.Reward * multiplier);
        PlayerDataModel playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        bool animateCoins = ArrowUITopbarSub.CanAnimateCoins();
        bool claimed = m_Task.TaskType == 1
            ? m_AdCompleted && playerData.TryClaimTaskAdReward(m_Task.Id, reward, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), !animateCoins)
            : m_Task.TaskType == 2 && playerData.TryClaimLevelTask(m_Task.Id, m_Task.Level, reward, !animateCoins);
        if (!claimed)
        {
            m_IsClaiming = false;
            varRewardCount.text = m_Task.Reward.ToString();
            varBtnClaim.interactable = true;
            varBtnAd.interactable = true;
            GF.UI.ShowToast("Not enough");
            return;
        }
        if (animateCoins)
        {
            GF.Event.Fire(this, RewardCollectedEventArgs.Create(
                reward, varRewardCount.transform.position, PlayerDataType.Coins));
        }
        GF.UI.Close(UIForm);
    }
}
