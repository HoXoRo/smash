using DG.Tweening;
using UnityEngine;

namespace Menu.BottomNavigation
{
	public class BottomNavigationBigItem : MonoBehaviour
	{
		public RectTransform rectTransform;

		[SerializeField]
		private RectTransform contentTransform;

		public void PlaySelectAnimation()
		{
			if (contentTransform == null)
			{
				return;
			}
			contentTransform.DOKill();
			contentTransform.localScale = Vector3.one;
			contentTransform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f);
		}

		private void OnDisable()
		{
			ResetAnimationState();
		}

		private void OnDestroy()
		{
			ResetAnimationState();
		}

		private void ResetAnimationState()
		{
			if (contentTransform == null)
			{
				return;
			}
			contentTransform.DOKill();
			contentTransform.localScale = Vector3.one;
		}
	}
}
