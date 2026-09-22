using UnityEngine;

namespace Haptics
{
	public class AnimationHaptics : MonoBehaviour
	{
		public void PlaySelection()
		{
			HapticManager.Play(HapticType.Selection);
		}

		public void PlaySuccess()
		{
			HapticManager.Play(HapticType.Success);
		}

		public void PlayWarning()
		{
			HapticManager.Play(HapticType.Warning);
		}

		public void PlayFailure()
		{
			HapticManager.Play(HapticType.Failure);
		}

		public void PlayRigid()
		{
			HapticManager.Play(HapticType.Rigid);
		}

		public void PlaySoft()
		{
			HapticManager.Play(HapticType.Soft);
		}

		public void PlayLight()
		{
			HapticManager.Play(HapticType.Light);
		}

		public void PlayMedium()
		{
			HapticManager.Play(HapticType.Medium);
		}

		public void PlayHeavy()
		{
			HapticManager.Play(HapticType.Heavy);
		}
	}
}
