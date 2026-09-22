namespace Gameplay.Particles
{
	public static class ParticleDelayMap
	{
		public static float GetDelay(ParticleType type)
		{
			switch (type)
			{
			case ParticleType.JamJarBlue1xHit:
			case ParticleType.JamJarBlue2xHit:
			case ParticleType.JamJarBlue3xHit:
			case ParticleType.JamJarPink1xHit:
			case ParticleType.JamJarPink2xHit:
			case ParticleType.JamJarPink3xHit:
			case ParticleType.JamJarYellow1xHit:
			case ParticleType.JamJarYellow2xHit:
			case ParticleType.JamJarYellow3xHit:
			case ParticleType.JamJarRed1xHit:
			case ParticleType.JamJarRed2xHit:
			case ParticleType.JamJarRed3xHit:
			case ParticleType.JamJarOrange1xHit:
			case ParticleType.JamJarOrange2xHit:
			case ParticleType.JamJarOrange3xHit:
			case ParticleType.JamJarPurple1xHit:
			case ParticleType.JamJarPurple2xHit:
			case ParticleType.JamJarPurple3xHit:
				return 0.03f;
			default:
				return 0f;
			}
		}
	}
}
