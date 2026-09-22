using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Objects
{
	public static class ObjectMassMaps
	{
		public static readonly Dictionary<(ObjectType type, int volume), float> MassMap = new Dictionary<(ObjectType type, int volume), float>
		{
			[(ObjectType.CanCylinder, 1)] = 1f,
			[(ObjectType.CanCylinder, 2)] = 2f,
			[(ObjectType.CanCylinder, 3)] = 3f,
			[(ObjectType.CanCylinder, 4)] = 4f,
			[(ObjectType.CanCylinder, 5)] = 5f,
			[(ObjectType.CanCone, 0)] = 0.75f,
			[(ObjectType.CanSquare, 1)] = 1f,
			[(ObjectType.CanSquare, 2)] = 2f,
			[(ObjectType.CanSquare, 3)] = 3f,
			[(ObjectType.CanSquare, 4)] = 4f,
			[(ObjectType.CanSquare, 5)] = 5f,
			[(ObjectType.JamJarBlue, 1)] = 1f,
			[(ObjectType.JamJarBlue, 2)] = 2f,
			[(ObjectType.JamJarBlue, 3)] = 3f,
			[(ObjectType.JamJarPink, 1)] = 1f,
			[(ObjectType.JamJarPink, 2)] = 2f,
			[(ObjectType.JamJarPink, 3)] = 3f,
			[(ObjectType.JamJarYellow, 1)] = 1f,
			[(ObjectType.JamJarYellow, 2)] = 2f,
			[(ObjectType.JamJarYellow, 3)] = 3f,
			[(ObjectType.JamJarRed, 1)] = 1f,
			[(ObjectType.JamJarRed, 2)] = 2f,
			[(ObjectType.JamJarRed, 3)] = 3f,
			[(ObjectType.JamJarOrange, 1)] = 1f,
			[(ObjectType.JamJarOrange, 2)] = 2f,
			[(ObjectType.JamJarOrange, 3)] = 3f,
			[(ObjectType.JamJarPurple, 1)] = 1f,
			[(ObjectType.JamJarPurple, 2)] = 2f,
			[(ObjectType.JamJarPurple, 3)] = 3f,
			[(ObjectType.StoneSquare, 1)] = 4f,
			[(ObjectType.StoneSquare, 2)] = 6f,
			[(ObjectType.StoneSquare, 3)] = 8f,
			[(ObjectType.StoneSquare, 4)] = 10f,
			[(ObjectType.StoneSquare, 5)] = 12f,
			[(ObjectType.StoneCylinder, 1)] = 4f,
			[(ObjectType.StoneCylinder, 2)] = 6f,
			[(ObjectType.StoneCylinder, 3)] = 8f,
			[(ObjectType.StoneCylinder, 4)] = 10f,
			[(ObjectType.StoneCylinder, 5)] = 12f,
			[(ObjectType.Wormhole, 0)] = 1f,
			[(ObjectType.BoxSquare, 1)] = 2.5f,
			[(ObjectType.BoxSquare, 2)] = 3.75f,
			[(ObjectType.BoxSquare, 3)] = 5f,
			[(ObjectType.BoxSquare, 4)] = 6.25f,
			[(ObjectType.BoxSquare, 5)] = 7.5f,
			[(ObjectType.BoxCylinder, 1)] = 3f,
			[(ObjectType.BoxCylinder, 2)] = 4.5f,
			[(ObjectType.BoxCylinder, 3)] = 6f,
			[(ObjectType.BoxCylinder, 4)] = 7.5f,
			[(ObjectType.BoxCylinder, 5)] = 9f,
			[(ObjectType.IceSquare, 1)] = 3f,
			[(ObjectType.IceSquare, 2)] = 4.5f,
			[(ObjectType.IceSquare, 3)] = 6f,
			[(ObjectType.IceSquare, 4)] = 7.5f,
			[(ObjectType.IceSquare, 5)] = 9f,
			[(ObjectType.IceSquare, 6)] = 10.5f,
			[(ObjectType.IceSquare, 7)] = 12f,
			[(ObjectType.IceSquare, 8)] = 13.5f,
			[(ObjectType.IceCylinder, 1)] = 3f,
			[(ObjectType.IceCylinder, 2)] = 4.5f,
			[(ObjectType.IceCylinder, 3)] = 6f,
			[(ObjectType.IceCylinder, 4)] = 7.5f,
			[(ObjectType.IceCylinder, 5)] = 9f,
			[(ObjectType.Disc, 0)] = 1f
		};

		private static readonly float MinMass;

		private static readonly float MaxMass;

		static ObjectMassMaps()
		{
			MinMass = MassMap.Values.Min();
			MaxMass = MassMap.Values.Max();
		}

		public static float GetNormalizedMass(ObjectType type, int volume)
		{
			float mass = GetRemainingObjectMass(type, volume);
			if (Mathf.Approximately(MinMass, MaxMass))
			{
				return 0f;
			}
			return Mathf.Clamp01((mass - MinMass) / (MaxMass - MinMass));
		}

		public static float GetRemainingObjectMass(ObjectType type, float volume)
		{
			int roundedVolume = Mathf.RoundToInt(volume);
			if (type == ObjectType.IceSquare || type == ObjectType.IceCylinder || type == ObjectType.Disc)
			{
				return roundedVolume;
			}
			if (MassMap.TryGetValue((type, roundedVolume), out float mass))
			{
				return mass;
			}
			if (MassMap.TryGetValue((type, 0), out float fallbackMass))
			{
				return fallbackMass;
			}
			return 1f;
		}
	}
}
