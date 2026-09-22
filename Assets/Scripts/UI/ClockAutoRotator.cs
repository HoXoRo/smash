using DG.Tweening;
using UnityEngine;

namespace UI
{
	public class ClockAutoRotator : MonoBehaviour
	{
		[SerializeField]
		private float durationPerQuarter;

		private Sequence _rotateSequence;

		private void OnEnable()
		{
			KillRotateSequence();
			_rotateSequence = DOTween.Sequence();
			for (int i = 0; i < 4; i++)
			{
				_rotateSequence.Append(transform.DOBlendableRotateBy(new Vector3(0f, 0f, -90f), durationPerQuarter).SetEase(Ease.Linear));
			}
			_rotateSequence.SetLoops(-1);
		}

		private void OnDisable()
		{
			KillRotateSequence();
		}

		private void OnDestroy()
		{
			KillRotateSequence();
		}

		private void KillRotateSequence()
		{
			if (_rotateSequence != null)
			{
				_rotateSequence.Kill();
				_rotateSequence = null;
			}
		}
	}
}
