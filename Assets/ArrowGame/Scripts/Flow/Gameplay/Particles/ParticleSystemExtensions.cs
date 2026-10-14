using UnityEngine;

namespace Gameplay.Particles
{
	public static class ParticleSystemExtensions
	{
		public static void PlayWithoutEmission(this ParticleSystem system)
		{
			if (system == null)
			{
				return;
			}
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
			system.Play(true);
		}
	}
}
