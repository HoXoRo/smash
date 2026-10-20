using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;


#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PcardRewardUIForm")]
public partial class PcardRewardUIForm : UIFormBase
{
    [SerializeField] private PcardRewardItem rewardItemPrefab = null;

    private readonly List<PcardRewardItem> m_Items = new List<PcardRewardItem>();
    private List<float> m_RewardValues;
    private bool m_IsClaiming;
    private bool m_ClaimCompleted;
    private bool m_AnimateClaim;
    private float m_ClaimedValue;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_IsClaiming = false;
        m_ClaimCompleted = false;
        m_AnimateClaim = false;
        m_ClaimedValue = 0f;
        m_RewardValues = PcardGiftRewardSession.GetSnapshot();
        CreateRewardItems();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(this);
        // Closing during the gather animation must not lose the saved reward or leave stale display.
        if (!isShutdown && m_IsClaiming && !m_ClaimCompleted && m_AnimateClaim)
        {
            m_ClaimCompleted = true;
            var player = GF.DataModel.GetOrCreate<PlayerDataModel>();
            ArrowUITopbar.CompletePcardDisplay(player);
        }
        m_Items.Clear();
        base.OnClose(isShutdown, userData);
    }

    private void CreateRewardItems()
    {
        m_Items.Clear();
        if (rewardItemPrefab == null || varContent == null || m_RewardValues == null)
        {
            return;
        }

        RectTransform content = varContent.transform as RectTransform;
        int rowCount = Mathf.CeilToInt(m_RewardValues.Count / 3f);
        if (content != null)
        {
            content.sizeDelta = new Vector2(880f, Mathf.Max(200f, rowCount * 220f));
        }

        for (int i = 0; i < m_RewardValues.Count; i++)
        {
            UIItemObject itemObject = SpawnItem<UIItemObject>(rewardItemPrefab.gameObject, varContent.transform);
            PcardRewardItem item = itemObject.itemLogic as PcardRewardItem;
            if (item == null)
            {
                continue;
            }

            item.SetValue(m_RewardValues[i]);
            item.gameObject.GetOrAddComponent<CanvasGroup>().alpha = 1f;
            PositionItem(item.RectTransform, i, m_RewardValues.Count, rowCount);
            m_Items.Add(item);
        }
    }

    private static void PositionItem(RectTransform item, int index, int totalCount, int rowCount)
    {
        if (item == null)
        {
            return;
        }

        const float columnSpacing = 300f;
        const float rowSpacing = 220f;
        int row = index / 3;
        int column = index % 3;
        int firstIndexInRow = row * 3;
        int itemsInRow = Mathf.Min(3, totalCount - firstIndexInRow);

        item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.localScale = Vector3.one;
        item.anchoredPosition = new Vector2(
            (column - (itemsInRow - 1) * 0.5f) * columnSpacing,
            ((rowCount - 1) * 0.5f - row) * rowSpacing);
    }

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf != null && btSelf.name == "btnClaim")
        {
            PlayClaimAnimation(btSelf);
        }
    }

    private void PlayClaimAnimation(Button claimButton)
    {
        if (m_IsClaiming || m_Items.Count == 0)
        {
            return;
        }

        m_IsClaiming = true;
        claimButton.interactable = false;
        var player = GF.DataModel.GetOrCreate<PlayerDataModel>();
        m_AnimateClaim = player.PcardDisplayValue > 0f;
        m_ClaimedValue = PcardGiftRewardSession.Consume(m_RewardValues.Count);
        player.CollectPcard(m_ClaimedValue);
        if (!m_AnimateClaim)
        {
            m_ClaimCompleted = true;
            GF.UI.Close(UIForm);
            return;
        }

        Vector3 gatherPosition = varContent.transform.position;

        int remaining = m_Items.Count;
        for (int i = 0; i < m_Items.Count; i++)
        {
            PcardRewardItem item = m_Items[i];
            if (item == null || item.RectTransform == null)
            {
                remaining--;
                continue;
            }

            CanvasGroup canvasGroup = item.gameObject.GetOrAddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetId(this);
            sequence.Append(item.RectTransform.DOMove(gatherPosition, 0.35f).SetEase(Ease.InOutQuad));
            sequence.Join(item.RectTransform.DOScale(0.75f, 0.35f).SetEase(Ease.InOutQuad));
            sequence.OnComplete(() =>
            {
                item.gameObject.SetActive(false);
                remaining--;
                if (remaining <= 0)
                {
                    CompleteClaim(gatherPosition);
                }
            });
        }

        if (remaining <= 0)
        {
            CompleteClaim(gatherPosition);
        }
    }

    private void CompleteClaim(Vector3 collectPosition)
    {
        if (m_ClaimCompleted) return;
        m_ClaimCompleted = true;
        var player = GF.DataModel.GetOrCreate<PlayerDataModel>();
        // The reward is already saved. The topbar only owns its presentation.
        if (!ArrowUITopbar.TryPlayPcardReward(m_ClaimedValue, collectPosition))
            ArrowUITopbar.CompletePcardDisplay(player);
        GF.UI.Close(UIForm);
    }
}
