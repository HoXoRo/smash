using System.Collections.Generic;
using System.Linq;
using Service;
using UnityEngine;

namespace Gameplay.Particles
{
	public class ParticleController : ServiceMonoBehaviour
	{
		private ParticleData _data;

		private readonly Dictionary<ParticleType, ParticlePlayer> _cache = new Dictionary<ParticleType, ParticlePlayer>();

		public void Initialize()
		{
			_data = Resources.Load<ParticleData>("ParticleData");
		}

		private ParticlePlayer Spawn(ParticleType type)
		{
			if (_cache.TryGetValue(type, out ParticlePlayer cached))
			{
				if (cached != null)
				{
					return cached;
				}
				_cache.Remove(type);
			}
			ParticleDataItem item = _data?.items?.FirstOrDefault((ParticleDataItem x) => x.type == type);
			if (item == null || item.prefab == null)
			{
				_cache[type] = null;
				return null;
			}
			GameObject instance = Object.Instantiate(item.prefab);
			ParticlePlayer player = instance.GetComponent<ParticlePlayer>();
			_cache[type] = player;
			return player;
		}

		public void PlayOnPosition(ParticleType type, Vector3 worldPos, Quaternion worldRotation, Vector3 spawnVelocity)
		{
			ParticlePlayer player = Spawn(type);
			if (player != null)
			{
				player.Play(type, worldPos, worldRotation, spawnVelocity);
			}
		}

		public bool IsAnyParticleObjectAlive()
		{
			return _cache.Any((KeyValuePair<ParticleType, ParticlePlayer> x) => x.Value != null && x.Value.AnyParticleObjectsAlive());
		}
	}
}
