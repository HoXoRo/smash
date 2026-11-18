using UnityEngine;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/PhoneBtnItem")]
[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class PhoneBtnItem : MonoBehaviour
{
    [SerializeField] private RectTransform lightTransform;
    [SerializeField] private float rotationSpeed = -36f;
    private UnityEngine.UI.Button m_Button;

    private void Awake()
    {
        m_Button = GetComponent<UnityEngine.UI.Button>();
    }

    private void OnEnable()
    {
        m_Button.onClick.AddListener(OpenPhoneUI);
    }

    private void OnDisable()
    {
        m_Button.onClick.RemoveListener(OpenPhoneUI);
    }

    private void Update()
    {
        if (lightTransform != null)
            lightTransform.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    private void OpenPhoneUI()
    {
        if (GF.UI.HasUIForm(UIViews.PhoneUIForm) || GF.UI.IsLoadingUIForm(UIViews.PhoneUIForm)) return;
        GF.UI.OpenUIForm(UIViews.PhoneUIForm);
    }
}
