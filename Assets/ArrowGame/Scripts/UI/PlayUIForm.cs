using UnityEngine;
using DG.Tweening;

using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PlayUIForm")]

public partial class PlayUIForm : UIFormBase
{
    private static int s_RemainingBallCount;
    private static int s_RocketUseCount;
    private int m_GuideSerialId = -1;
    private RectTransform m_HintRect;
    private CanvasGroup m_HintCanvasGroup;
    private Vector2 m_HintRestPosition;
    private Sequence m_HintTween;

    public static void ShowBelowTableHint()
    {
        string assetName = GF.UI.GetUIFormAssetName(UIViews.PlayUIForm);
        if (string.IsNullOrEmpty(assetName)) return;
        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null) return;
        foreach (var form in forms)
        {
            if (form.Logic is PlayUIForm playUI && playUI.gameObject.activeInHierarchy)
                playUI.PlayBelowTableHint();
        }
    }

    private void PlayBelowTableHint()
    {
        if (m_HintRect == null || m_HintCanvasGroup == null) return;
        ResetHint();
        m_HintRect.anchoredPosition = m_HintRestPosition + Vector2.down * 50f;
        m_HintCanvasGroup.alpha = 1f;
        varTxtHint.SetActive(true);
        m_HintTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(m_HintRect.DOAnchorPos(m_HintRestPosition, 0.2f).SetEase(Ease.OutQuad))
            .AppendInterval(0.5f)
            .Append(m_HintCanvasGroup.DOFade(0f, 0.2f))
            .OnComplete(() =>
            {
                varTxtHint.SetActive(false);
                m_HintTween = null;
            });
    }

    private void ResetHint()
    {
        m_HintTween?.Kill();
        m_HintTween = null;
        if (m_HintRect != null) m_HintRect.anchoredPosition = m_HintRestPosition;
        if (m_HintCanvasGroup != null) m_HintCanvasGroup.alpha = 0f;
        if (varTxtHint != null) varTxtHint.SetActive(false);
    }

    public static void ShowFirstLevelGuides(Transform target)
    {
        string assetName = GF.UI.GetUIFormAssetName(UIViews.PlayUIForm);
        if (string.IsNullOrEmpty(assetName)) return;
        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null) return;
        foreach (var form in forms)
        {
            if (form.Logic is PlayUIForm playUI)
            {
                playUI.ShowFirstLevelGuide(target);
                return;
            }
        }
    }

    private void ShowFirstLevelGuide(Transform target)
    {
        if (GuideManager.Instance.IsGuideCompleted(1))
        {
            ShowBallCountGuide();
            return;
        }
        m_GuideSerialId = GuideManager.Instance.ShowGuide(1, target, onComplete: ShowBallCountGuide);
    }

    private void ShowBallCountGuide()
    {
        m_GuideSerialId = -1;
        if (varTxtCount != null && !GuideManager.Instance.IsGuideCompleted(2))
            m_GuideSerialId = GuideManager.Instance.ShowGuide(2, varTxtCount.transform.parent);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (!isShutdown && m_GuideSerialId >= 0 && (GF.UI.HasUIForm(m_GuideSerialId) || GF.UI.IsLoadingUIForm(m_GuideSerialId)))
            GF.UI.CloseUIForm(m_GuideSerialId);
        m_GuideSerialId = -1;
        ResetHint();
        base.OnClose(isShutdown, userData);
    }

    public static void SetRocketUseCount(int count)
    {
        s_RocketUseCount = Mathf.Max(0, count);
        SetRemainingBallCount(s_RemainingBallCount);
    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnSetting.onClick.AddListener(OnSettingClicked);
        if (varTxtHint != null)
        {
            m_HintRect = varTxtHint.GetComponent<RectTransform>();
            if (m_HintRect != null) m_HintRestPosition = m_HintRect.anchoredPosition;
            m_HintCanvasGroup = varTxtHint.GetComponent<CanvasGroup>();
            if (m_HintCanvasGroup == null) m_HintCanvasGroup = varTxtHint.AddComponent<CanvasGroup>();
            m_HintCanvasGroup.blocksRaycasts = false;
            m_HintCanvasGroup.interactable = false;
        }
        ResetHint();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ResetHint();
        RefreshRemainingBallCount();
    }

    public static void SetRemainingBallCount(int count)
    {
        s_RemainingBallCount = Mathf.Max(0, count);

        string assetName = GF.UI.GetUIFormAssetName(UIViews.PlayUIForm);
        if (string.IsNullOrEmpty(assetName))
        {
            return;
        }

        var forms = GF.UI.GetUIForms(assetName);
        if (forms == null)
        {
            return;
        }

        foreach (var uiForm in forms)
        {
            if (uiForm.Logic is PlayUIForm playUIForm)
            {
                playUIForm.RefreshRemainingBallCount();
            }
        }
    }

    private void RefreshRemainingBallCount()
    {
        if (varTxtCount != null)
        {
            varTxtCount.text = s_RemainingBallCount.ToString();
        }
    }

    void OnSettingClicked()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarBoolean>(SettingPageUIForm.P_FromGameplay, true);
        OpenSubUIForm(UIViews.SettingPageUIForm, @params: uiParams);
    }
}
