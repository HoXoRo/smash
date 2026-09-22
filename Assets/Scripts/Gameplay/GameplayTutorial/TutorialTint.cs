using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Gameplay.GameplayTutorial
{
	public class TutorialTint : MonoBehaviour
	{
		[SerializeField]
		private Image tintImage;

		[SerializeField]
		private FlowButton tintButton;

		private Material _tintMaterial;

		private int _holePositionXProp;

		private int _holePositionYProp;

		private int _holeInnerWidthProp;

		private int _holeOuterWidthProp;

		private int _panelColorProp;

		private void Awake()
		{
			if (tintImage != null && tintImage.material != null)
			{
				_tintMaterial = Instantiate(tintImage.material);
				tintImage.material = _tintMaterial;
			}
			_holePositionXProp = Shader.PropertyToID("_HolePositionX");
			_holePositionYProp = Shader.PropertyToID("_HolePositionY");
			_holeInnerWidthProp = Shader.PropertyToID("_HoleInnerWidth");
			_holeOuterWidthProp = Shader.PropertyToID("_HoleOuterWidth");
			_panelColorProp = Shader.PropertyToID("_PanelColor");
		}

		public void SetOnClick(UnityAction action)
		{
			ClearOnClick();
			tintButton?.OnClick.AddListener(action);
		}

		public void SetOnClickDown(UnityAction action)
		{
			ClearOnClickDown();
			tintButton?.OnClickDown.AddListener(action);
		}

		public void SetOnClickUp(UnityAction action)
		{
			ClearOnClickUp();
			tintButton?.OnClickUp.AddListener(action);
		}

		public void ClearOnClick()
		{
			tintButton?.OnClick.RemoveAllListeners();
		}

		public void ClearOnClickDown()
		{
			tintButton?.OnClickDown.RemoveAllListeners();
		}

		public void ClearOnClickUp()
		{
			tintButton?.OnClickUp.RemoveAllListeners();
		}

		public void BecomeTransparentInstant()
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holeInnerWidthProp, 0f);
			_tintMaterial.SetFloat(_holeOuterWidthProp, 0f);
		}

		public void SetColor(Color color)
		{
			_tintMaterial?.SetColor(_panelColorProp, color);
		}

		public void Set(float posX, float posY, float innerWidth, float outerWidth, float animDuration)
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holePositionXProp, posX);
			_tintMaterial.SetFloat(_holePositionYProp, posY);
			if (animDuration <= 0f)
			{
				_tintMaterial.SetFloat(_holeInnerWidthProp, innerWidth);
				_tintMaterial.SetFloat(_holeOuterWidthProp, outerWidth);
				return;
			}
			_tintMaterial.SetFloat(_holeInnerWidthProp, 0f);
			_tintMaterial.SetFloat(_holeOuterWidthProp, 0f);
			_tintMaterial.DOFloat(innerWidth, _holeInnerWidthProp, animDuration).SetEase(Ease.Linear);
			_tintMaterial.DOFloat(outerWidth, _holeOuterWidthProp, animDuration).SetEase(Ease.Linear);
		}

		private void OnDisable()
		{
			CleanupTintState();
		}

		private void OnDestroy()
		{
			CleanupTintState();
			if (_tintMaterial != null)
			{
				Destroy(_tintMaterial);
			}
		}

		private void CleanupTintState()
		{
			ClearOnClick();
			ClearOnClickDown();
			ClearOnClickUp();
			_tintMaterial?.DOKill();
			BecomeTransparentInstant();
		}
	}
}
