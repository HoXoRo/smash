using System;
using System.Collections;
using DG.Tweening;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/LevelCompleteUIForm")]
public partial class LevelCompleteUIForm : UIFormBase
{
    public const string P_LevelId = "LevelId";
    public const string P_OnNext = "OnNext";

    private const float RewardRollDuration = 0.8f;
    private const float CoinCollectDuration = 2.1f;

    private int m_BaseReward;
    private int m_AdMulti;
    private bool m_IsClaiming;
    private Action m_OnNext;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnNext.onClick.AddListener(OnNextClicked);
        varBtnAd.onClick.AddListener(OnAdClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        int fallbackLevel = Mathf.Max(1, GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId - 1);
        int completedLevel = Params.Get<VarInt32>(P_LevelId, fallbackLevel);
        m_OnNext = Params.Get<VarAction>(P_OnNext)?.Value;
        m_IsClaiming = false;

        LevelCompleteReward rewardRow = GetRewardRow(completedLevel);
        m_BaseReward = rewardRow?.Reward ?? 0;
        m_AdMulti = rewardRow?.AdMulti ?? 0;

        varRewardCount.text = m_BaseReward.ToString();
        varTxtMulit.text = $"X{m_AdMulti}";
        varBtnAd.gameObject.SetActive(m_AdMulti > 0);
        SetButtonsInteractable(true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopAllCoroutines();
        m_OnNext = null;
        base.OnClose(isShutdown, userData);
    }

    private static LevelCompleteReward GetRewardRow(int completedLevel)
    {
        var table = GF.DataTable.GetDataTable<LevelCompleteReward>();
        if (table == null)
        {
            Log.Error("LevelCompleteUIForm: LevelCompleteReward data table is not loaded.");
            return null;
        }

        LevelCompleteReward directRow = table.GetDataRow(completedLevel);
        if (directRow != null)
        {
            return directRow;
        }

        LevelCompleteReward[] rows = table.GetAllDataRows();
        if (rows == null || rows.Length == 0)
        {
            Log.Error("LevelCompleteUIForm: LevelCompleteReward data table is empty.");
            return null;
        }

        Array.Sort(rows, (left, right) => left.Id.CompareTo(right.Id));
        int lastIndex = rows.Length - 1;
        int maxId = rows[lastIndex].Id;
        if (completedLevel > maxId)
        {
            int cycleCount = Mathf.Min(10, rows.Length);
            int cycleStart = rows.Length - cycleCount;
            int offset = (completedLevel - maxId - 1) % cycleCount;
            return rows[cycleStart + offset];
        }

        return rows[0];
    }

    private void OnNextClicked()
    {
        if (m_IsClaiming)
        {
            return;
        }

        ClaimAndContinue(m_BaseReward);
    }

    private void OnAdClicked()
    {
        if (m_IsClaiming || m_AdMulti <= 0)
        {
            return;
        }

        m_IsClaiming = true;
        SetButtonsInteractable(false);
        CommonHelper.ShowVideoAd("level_complete_reward", success =>
        {
            if (success)
            {
                StartCoroutine(RollRewardAndContinue(m_BaseReward * m_AdMulti));
                return;
            }

            m_IsClaiming = false;
            SetButtonsInteractable(true);
        });
    }

    private IEnumerator RollRewardAndContinue(int targetReward)
    {
        int startReward = m_BaseReward;
        float elapsed = 0f;
        while (elapsed < RewardRollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / RewardRollDuration);
            varRewardCount.text = Mathf.RoundToInt(Mathf.Lerp(startReward, targetReward, progress)).ToString();
            yield return null;
        }

        varRewardCount.text = targetReward.ToString();
        ClaimAndContinue(targetReward);
    }

    private void ClaimAndContinue(int reward)
    {
        if (!m_IsClaiming)
        {
            m_IsClaiming = true;
            SetButtonsInteractable(false);
        }

        Action onNext = m_OnNext;
        m_OnNext = null;

        GF.Event.Fire(this, RewardCollectedEventArgs.Create(
            Mathf.Max(0, reward),
            varRewardCount.transform.position,
            PlayerDataType.Coins));

        GF.UI.Close(UIForm);

        DOVirtual.DelayedCall(CoinCollectDuration, () =>
            {
                GF.DataModel.GetOrCreate<PlayerDataModel>().Save();
                onNext?.Invoke();
            })
            .SetUpdate(true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        varBtnNext.interactable = interactable;
        varBtnAd.interactable = interactable;
    }
}
