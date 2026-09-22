using Audio;
using Haptics;
using Service;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class FlowButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerClickHandler
{
	[SerializeField]
	private bool interactable = true;

	[Header("Animation")]
	[SerializeField]
	private AnimationClip idleClip;

	[SerializeField]
	private AnimationClip selectedClip;

	[SerializeField]
	private AnimationClip disabledClip;

	private const string IdleClipName = "Idle";

	private const string SelectedClipName = "Selected";

	private const string DisabledClipName = "Disabled";

	[Header("Click Feedback")]
	[SerializeField]
	private HapticType hapticTypeOnClick = HapticType.Selection;

	[SerializeField]
	private Audio.AudioType audioTypeOnClick = Audio.AudioType.UIClick;

	[SerializeField]
	private UnityEvent onClick = new UnityEvent();

	private UnityEvent _onClickDown = new UnityEvent();

	private UnityEvent _onClickUp = new UnityEvent();

	private Animation _animationComponent;

	private int _enabledFrame = -1;

	public bool Interactable
	{
		get => interactable;
		set
		{
			interactable = value;
			UpdateVisualState();
		}
	}

	public UnityEvent OnClick => onClick ?? (onClick = new UnityEvent());

	public UnityEvent OnClickDown => _onClickDown ?? (_onClickDown = new UnityEvent());

	public UnityEvent OnClickUp => _onClickUp ?? (_onClickUp = new UnityEvent());

	private void OnEnable()
	{
		_enabledFrame = Time.frameCount;
		EnsureAnimationComponent();
		EnsureClips();
		UpdateVisualState();
	}

	private void EnsureAnimationComponent()
	{
		_animationComponent = GetComponent<Animation>();
		if (_animationComponent == null)
		{
			_animationComponent = gameObject.AddComponent<Animation>();
		}
	}

	private void EnsureClips()
	{
		TryAddClip(idleClip, IdleClipName);
		TryAddClip(selectedClip, SelectedClipName);
		TryAddClip(disabledClip, DisabledClipName);
	}

	private void TryAddClip(AnimationClip clip, string clipName)
	{
		if (clip != null && _animationComponent != null && _animationComponent.GetClip(clipName) == null)
		{
			_animationComponent.AddClip(clip, clipName);
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!CanHandlePointerEvent(eventData))
		{
			return;
		}
		PlayClip(SelectedClipName, selectedClip);
		OnClickDown.Invoke();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!CanHandlePointerEvent(eventData))
		{
			return;
		}
		PlayClip(IdleClipName, idleClip);
		OnClickUp.Invoke();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!CanHandlePointerEvent(eventData))
		{
			return;
		}
		PlayHaptics();
		PlayAudio();
		OnClick.Invoke();
	}

	private bool CanHandlePointerEvent(PointerEventData eventData)
	{
		if (!interactable || !isActiveAndEnabled || !gameObject.activeInHierarchy || Time.frameCount == _enabledFrame)
		{
			return false;
		}

		if (eventData == null || eventData.pointerPress == null)
		{
			return true;
		}

		Transform pressedTransform = eventData.pointerPress.transform;
		return pressedTransform == transform || pressedTransform.IsChildOf(transform) || transform.IsChildOf(pressedTransform);
	}
	private void UpdateVisualState()
	{
		if (interactable)
		{
			PlayClip(IdleClipName, idleClip);
		}
		else
		{
			PlayClip(DisabledClipName, disabledClip);
		}
	}

	private bool PlayClip(string clipName, AnimationClip clip)
	{
		if (clip == null || _animationComponent == null)
		{
			return false;
		}
		TryAddClip(clip, clipName);
		return _animationComponent.Play(clipName);
	}

	private void PlayHaptics()
	{
		if (hapticTypeOnClick != HapticType.None)
		{
			HapticManager.Play(hapticTypeOnClick);
		}
	}

	private void PlayAudio()
	{
		if (audioTypeOnClick != Audio.AudioType.None)
		{
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(audioTypeOnClick);
		}
	}
}
