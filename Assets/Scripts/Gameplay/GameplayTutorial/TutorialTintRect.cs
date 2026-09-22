using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Gameplay.GameplayTutorial
{
	public class TutorialTintRect : MonoBehaviour
	{
		[SerializeField]
		private Image tintImage;

		[SerializeField]
		private FlowButton tintButton;

		private Material _tintMaterial;

		private int _holePositionXProp;

		private int _holePositionYProp;

		private int _holeInnerWidthProp;

		private int _holeInnerHeightProp;

		private int _holeOuterWidthProp;

		private int _holeOuterHeightProp;

		private int _holeRadiusProp;

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
			_holeInnerHeightProp = Shader.PropertyToID("_HoleInnerHeight");
			_holeOuterWidthProp = Shader.PropertyToID("_HoleOuterWidth");
			_holeOuterHeightProp = Shader.PropertyToID("_HoleOuterHeight");
			_holeRadiusProp = Shader.PropertyToID("_HoleRadius");
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
			_tintMaterial.SetFloat(_holeInnerHeightProp, 0f);
			_tintMaterial.SetFloat(_holeOuterWidthProp, 0f);
			_tintMaterial.SetFloat(_holeOuterHeightProp, 0f);
		}

		public void SetColor(Color color)
		{
			_tintMaterial?.SetColor(_panelColorProp, color);
		}

		public void SetRadius(float radius)
		{
			_tintMaterial?.SetFloat(_holeRadiusProp, radius);
		}

		public void Set(float posX, float posY, float innerWidth, float innerHeight, float outerWidth, float outerHeight, float radius, float animDuration)
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holePositionXProp, posX);
			_tintMaterial.SetFloat(_holePositionYProp, posY);
			_tintMaterial.SetFloat(_holeRadiusProp, radius);
			if (animDuration <= 0f)
			{
				_tintMaterial.SetFloat(_holeInnerWidthProp, innerWidth);
				_tintMaterial.SetFloat(_holeInnerHeightProp, innerHeight);
				_tintMaterial.SetFloat(_holeOuterWidthProp, outerWidth);
				_tintMaterial.SetFloat(_holeOuterHeightProp, outerHeight);
				return;
			}
			_tintMaterial.SetFloat(_holeInnerWidthProp, 0f);
			_tintMaterial.SetFloat(_holeInnerHeightProp, 0f);
			_tintMaterial.SetFloat(_holeOuterWidthProp, 0f);
			_tintMaterial.SetFloat(_holeOuterHeightProp, 0f);
			_tintMaterial.DOFloat(innerWidth, _holeInnerWidthProp, animDuration).SetEase(Ease.Linear);
			_tintMaterial.DOFloat(innerHeight, _holeInnerHeightProp, animDuration).SetEase(Ease.Linear);
			_tintMaterial.DOFloat(outerWidth, _holeOuterWidthProp, animDuration).SetEase(Ease.Linear);
			_tintMaterial.DOFloat(outerHeight, _holeOuterHeightProp, animDuration).SetEase(Ease.Linear);
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
