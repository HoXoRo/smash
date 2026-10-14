using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Particles
{
	[CreateAssetMenu(fileName = "ParticleData", menuName = "Flow/Particle Data", order = 0)]
	public class ParticleData : ScriptableObject
	{
		public List<ParticleDataItem> items;
	}
}
