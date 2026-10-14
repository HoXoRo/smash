using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Gameplay.GameplayTutorial
{
	public class TutorialTintDoubleHole : MonoBehaviour
	{
		[SerializeField]
		private Image tintImage;

		[SerializeField]
		private FlowButton tintButton;

		private Material _tintMaterial;

		private int _holeA_PositionXProp;

		private int _holeA_PositionYProp;

		private int _holeA_InnerWidthProp;

		private int _holeA_OuterWidthProp;

		private int _holeB_PositionXProp;

		private int _holeB_PositionYProp;

		private int _holeB_InnerWidthProp;

		private int _holeB_OuterWidthProp;

		private int _panelColorProp;

		private void Awake()
		{
			if (tintImage != null && tintImage.material != null)
			{
				_tintMaterial = Instantiate(tintImage.material);
				tintImage.material = _tintMaterial;
			}
			_holeA_PositionXProp = Shader.PropertyToID("_HoleA_PositionX");
			_holeA_PositionYProp = Shader.PropertyToID("_HoleA_PositionY");
			_holeA_InnerWidthProp = Shader.PropertyToID("_HoleA_InnerWidth");
			_holeA_OuterWidthProp = Shader.PropertyToID("_HoleA_OuterWidth");
			_holeB_PositionXProp = Shader.PropertyToID("_HoleB_PositionX");
			_holeB_PositionYProp = Shader.PropertyToID("_HoleB_PositionY");
			_holeB_InnerWidthProp = Shader.PropertyToID("_HoleB_InnerWidth");
			_holeB_OuterWidthProp = Shader.PropertyToID("_HoleB_OuterWidth");
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
			DisableHoleA();
			DisableHoleB();
		}

		public void SetColor(Color color)
		{
			_tintMaterial?.SetColor(_panelColorProp, color);
		}

		public void DisableHoleA()
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holeA_InnerWidthProp, 0f);
			_tintMaterial.SetFloat(_holeA_OuterWidthProp, 0f);
		}

		public void DisableHoleB()
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holeB_InnerWidthProp, 0f);
			_tintMaterial.SetFloat(_holeB_OuterWidthProp, 0f);
		}

		public void SetHoleA(float posX, float posY, float innerWidth, float outerWidth, float animDuration)
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holeA_PositionXProp, posX);
			_tintMaterial.SetFloat(_holeA_PositionYProp, posY);
			SetHoleWidths(_holeA_InnerWidthProp, _holeA_OuterWidthProp, innerWidth, outerWidth, animDuration);
		}

		public void SetHoleB(float posX, float posY, float innerWidth, float outerWidth, float animDuration)
		{
			if (_tintMaterial == null)
			{
				return;
			}
			_tintMaterial.SetFloat(_holeB_PositionXProp, posX);
			_tintMaterial.SetFloat(_holeB_PositionYProp, posY);
			SetHoleWidths(_holeB_InnerWidthProp, _holeB_OuterWidthProp, innerWidth, outerWidth, animDuration);
		}

		private void SetHoleWidths(int innerProp, int outerProp, float innerWidth, float outerWidth, float animDuration)
		{
			if (_tintMaterial == null)
			{
				return;
			}
			if (animDuration <= 0f)
			{
				_tintMaterial.SetFloat(innerProp, innerWidth);
				_tintMaterial.SetFloat(outerProp, outerWidth);
				return;
			}
			_tintMaterial.SetFloat(innerProp, 0f);
			_tintMaterial.SetFloat(outerProp, 0f);
			_tintMaterial.DOFloat(innerWidth, innerProp, animDuration).SetEase(Ease.Linear);
			_tintMaterial.DOFloat(outerWidth, outerProp, animDuration).SetEase(Ease.Linear);
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
