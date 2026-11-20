using System.Collections;
using UnityEngine;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/PhoneBtnItem")]
[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class PhoneBtnItem : MonoBehaviour
{
    [SerializeField] private RectTransform lightTransform;
    [SerializeField] private TMPro.TextMeshProUGUI txtProgress;
    private int m_LastFragments = -1;
    private int m_LastFragmentTarget = -1;
    [SerializeField] private float rotationSpeed = -36f;
    private UnityEngine.UI.Button m_Button;
    private Coroutine m_GuideRoutine;
    private int m_GuideSerialId = -1;

    private void Awake()
    {
        m_Button = GetComponent<UnityEngine.UI.Button>();
    }

    private void OnEnable()
    {
        RefreshProgress();
        m_Button.onClick.AddListener(OpenPhoneUI);
        m_GuideRoutine = StartCoroutine(ShowPhoneGuideWhenReady());
    }

    private void OnDisable()
    {
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
        if (txtProgress == null) return;
        var task = PhoneManager.Instance.Task;
        if (task == null)
        {
            txtProgress.gameObject.SetActive(false);
            return;
        }

        int fragments = GF.DataModel.GetOrCreate<PlayerDataModel>().PhoneFragments;
        bool visible = fragments < task.Fragment;
        if (txtProgress.gameObject.activeSelf != visible)
            txtProgress.gameObject.SetActive(visible);
        if (m_LastFragments == fragments && m_LastFragmentTarget == task.Fragment) return;
        m_LastFragments = fragments;
        m_LastFragmentTarget = task.Fragment;
        txtProgress.text = $"{fragments}/{task.Fragment}";
    }

    private void OpenPhoneUI()
    {
        if (!PhoneManager.Instance.IsUnlocked) return;
        if (GF.UI.HasUIForm(UIViews.PhoneUIForm) || GF.UI.IsLoadingUIForm(UIViews.PhoneUIForm)) return;
        GF.UI.OpenUIForm(UIViews.PhoneUIForm);
    }
}
