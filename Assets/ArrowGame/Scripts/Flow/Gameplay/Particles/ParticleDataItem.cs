using System;
using UnityEngine;

namespace Gameplay.Particles
{
	[Serializable]
	public class ParticleDataItem
	{
		public ParticleType type;

		public string prefabPath;

		[NonSerialized] public GameObject prefab;
	}
}
