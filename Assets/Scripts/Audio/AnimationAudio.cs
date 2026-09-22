using UnityEngine;
using Service;

namespace Audio
{
	public class AnimationAudio : MonoBehaviour
	{
		public void Play(AudioType type)
		{
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(type);
		}
	}
}
