using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/PhoneBtnItem")]
[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class PhoneBtnItem : MonoBehaviour
{
    [SerializeField] private RectTransform lightTransform;
    [SerializeField] private TMPro.TextMeshProUGUI txtProgress;
    [SerializeField] private GameObject finger;
    private bool m_FragmentGuideClicked;
    private Sequence m_FingerSequence;
    private Vector3 m_FingerInitialScale;
    private Vector3 m_FingerInitialPosition;
    private int m_LastFragments = -1;
    private int m_LastFragmentTarget = -1;
    [SerializeField] private float rotationSpeed = -36f;
    private UnityEngine.UI.Button m_Button;
    private Coroutine m_GuideRoutine;
    private int m_GuideSerialId = -1;

    private void Awake()
    {
        m_Button = GetComponent<UnityEngine.UI.Button>();
        if (finger != null)
        {
            m_FingerInitialScale = finger.transform.localScale;
            m_FingerInitialPosition = finger.transform.localPosition;
        }
    }

    private void OnEnable()
    {
        RefreshProgress();
        m_Button.onClick.AddListener(OpenPhoneUI);
        m_GuideRoutine = StartCoroutine(ShowPhoneGuideWhenReady());
    }

    private void OnDisable()
    {
        SetFingerVisible(false);
        m_Button.onClick.RemoveListener(OpenPhoneUI);
        if (m_GuideRoutine != null)
        {
            StopCoroutine(m_GuideRoutine);
            m_GuideRoutine = null;
        }
        if (GF.UI != null && m_GuideSerialId >= 0 &&
            (GF.UI.HasUIForm(m_GuideSerialId) || GF.UI.IsLoadingUIForm(m_GuideSerialId)))
            GF.UI.CloseUIForm(m_GuideSerialId);
        m_GuideSerialId = -1;
    }

    private IEnumerator ShowPhoneGuideWhenReady()
    {
        // OnEnable can run before the owning form applies its unlock state.
        yield return null;
        var owner = GetComponentInParent<UIFormBase>();
        while (PhoneManager.Instance.IsUnlocked && !GuideManager.Instance.IsGuideCompleted(5))
        {
            if (!PlayLoadingUIForm.IsLoading && (owner == null || owner.Interactable) &&
                m_Button.IsInteractable() &&
                !GF.UI.HasUIForm(UIViews.ArrowGuideUIForm) &&
                !GF.UI.IsLoadingUIForm(UIViews.ArrowGuideUIForm) &&
                !GF.UI.HasUIForm(UIViews.PhoneUIForm) &&
                !GF.UI.IsLoadingUIForm(UIViews.PhoneUIForm))
            {
                m_GuideSerialId = GuideManager.Instance.ShowGuide(5, transform,
                    onComplete: () => m_GuideSerialId = -1);
                break;
            }
            yield return null;
        }
        m_GuideRoutine = null;
    }

    private void Update()
    {
        RefreshProgress();
        if (lightTransform != null)
            lightTransform.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    private void RefreshProgress()
    {

        var task = PhoneManager.Instance.Task;
        if (task == null)
        {
            if (txtProgress != null) txtProgress.gameObject.SetActive(false);
            SetFingerVisible(false);
            return;
        }

        var player = GF.DataModel.GetOrCreate<PlayerDataModel>();
        int fragments = player.PhoneFragments;
        bool fragmentsFull = fragments >= task.Fragment;
        bool visible = player.PhoneStage == 1 && !fragmentsFull;
        if (!fragmentsFull) m_FragmentGuideClicked = false;
        bool showFinger = fragmentsFull && player.PhoneStage == 1 && !m_FragmentGuideClicked;
        SetFingerVisible(showFinger);
        if (txtProgress == null) return;
        if (txtProgress.gameObject.activeSelf != visible)
            txtProgress.gameObject.SetActive(visible);
        if (m_LastFragments == fragments && m_LastFragmentTarget == task.Fragment) return;
        m_LastFragments = fragments;
        m_LastFragmentTarget = task.Fragment;
        txtProgress.text = $"{fragments}/{task.Fragment}";
    }

    private void SetFingerVisible(bool visible)
    {
        if (finger == null) return;
        if (finger.activeSelf != visible) finger.SetActive(visible);
        if (!visible)
        {
            m_FingerSequence?.Kill();
            m_FingerSequence = null;
            finger.transform.localScale = m_FingerInitialScale;
            finger.transform.localPosition = m_FingerInitialPosition;
            return;
        }
        if (m_FingerSequence != null) return;

        var fingerTransform = finger.transform;
        m_FingerSequence = DOTween.Sequence()
            .Append(fingerTransform.DOLocalMove(m_FingerInitialPosition + new Vector3(-8f, 8f, 0f), 0.25f).SetEase(Ease.InOutSine))
            .Join(fingerTransform.DOScale(m_FingerInitialScale * 0.85f, 0.25f).SetEase(Ease.InOutSine))
            .Append(fingerTransform.DOLocalMove(m_FingerInitialPosition, 0.3f).SetEase(Ease.OutSine))
            .Join(fingerTransform.DOScale(m_FingerInitialScale, 0.3f).SetEase(Ease.OutSine))
            .AppendInterval(0.45f)
            .SetLoops(-1)
            .SetUpdate(true);
    }

    private void OpenPhoneUI()
    {
        if (!PhoneManager.Instance.IsUnlocked) return;
        if (finger != null && finger.activeSelf)
        {
            m_FragmentGuideClicked = true;
            SetFingerVisible(false);
        }
        if (GF.UI.HasUIForm(UIViews.PhoneUIForm) || GF.UI.IsLoadingUIForm(UIViews.PhoneUIForm)) return;
        GF.UI.OpenUIForm(UIViews.PhoneUIForm);
    }
}
