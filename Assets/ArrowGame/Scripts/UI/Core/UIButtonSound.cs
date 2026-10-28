using Audio;
using AudioType = Audio.AudioType;
using UnityEngine;

/// <summary>Routes uGUI button clicks through the shared sound system.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UnityEngine.UI.Button))]
[AddComponentMenu("UI/UIButtonSound")]
public sealed class UIButtonSound : MonoBehaviour
{
    [Tooltip("Click sound. Select None to silence this button.")]
    [SerializeField] private AudioType soundType = AudioType.UIClick;

    [Tooltip("Optional sound asset name relative to the Audio folder, including extension.")]
    [SerializeField] private string customSoundName;

    private UnityEngine.UI.Button button;

    private void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        // A preceding click callback may already have closed this panel or
        // disabled its Button. Do not recheck their state after a valid click.
        if (!enabled || soundType == AudioType.None || GF.Sound == null) return;

        string soundName = string.IsNullOrWhiteSpace(customSoundName)
            ? soundType.ToSoundName()
            : customSoundName;
        if (!string.IsNullOrEmpty(soundName)) GF.Sound.PlayEffect(soundName);
    }

    /// <summary>Preserves manually configured components, including disabled ones.</summary>
    public static void AttachToButtons(GameObject root)
    {
        foreach (var target in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            // FlowButton already owns its click feedback.
            if (target.GetComponent<FlowButton>() != null ||
                target.GetComponent<UIButtonSound>() != null) continue;

            target.gameObject.AddComponent<UIButtonSound>();
        }
    }
}
