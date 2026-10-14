using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Gameplay.Particles
{
	public class ParticlePlayer : MonoBehaviour
	{
		[SerializeField]
		private GameObject[] particleObjectPrefabs;

		[SerializeField]
		private ParticleSystem[] systems;

		private List<ParticleObject> _activeParticleObjects = new List<ParticleObject>();

		private readonly List<Coroutine> _pendingParticleObjectRoutines = new List<Coroutine>();

		public void Play(ParticleType type, Vector3 worldPos, Quaternion worldRotation, Vector3 spawnVelocity)
		{
			transform.SetPositionAndRotation(worldPos, worldRotation);
			if (systems != null)
			{
				foreach (ParticleSystem system in systems)
				{
					if (system != null)
					{
						system.Clear(true);
						system.Play(true);
					}
				}
			}
			float delay = ParticleDelayMap.GetDelay(type);
			if (delay > 0f)
			{
				Coroutine routine = null;
				routine = DelayedWorker.CallAfter(delay, delegate
				{
					_pendingParticleObjectRoutines.Remove(routine);
					if (!this || !isActiveAndEnabled)
					{
						return;
					}
					PlayParticleObjects(worldPos, worldRotation, spawnVelocity);
				});
				_pendingParticleObjectRoutines.Add(routine);
			}
			else
			{
				PlayParticleObjects(worldPos, worldRotation, spawnVelocity);
			}
		}

		private void PlayParticleObjects(Vector3 worldPos, Quaternion worldRotation, Vector3 spawnVelocity)
		{
			if (particleObjectPrefabs == null)
			{
				return;
			}
			foreach (GameObject prefab in particleObjectPrefabs)
			{
				if (prefab == null)
				{
					continue;
				}
				GameObject instance = Object.Instantiate(prefab, worldPos, worldRotation);
				foreach (ParticleObject particleObject in instance.GetComponentsInChildren<ParticleObject>())
				{
					_activeParticleObjects.Add(particleObject);
					particleObject.SetPlayer(this);
					particleObject.SetSpawnVelocity(spawnVelocity);
					particleObject.Play();
				}
			}
		}

		public void OnParticleObjectDestroyed(ParticleObject particleObject)
		{
			_activeParticleObjects.Remove(particleObject);
		}

		public void GatherParticleSystems()
		{
			systems = GetComponentsInChildren<ParticleSystem>();
		}

		public bool AnyParticleObjectsAlive()
		{
			return _activeParticleObjects.Count > 0;
		}

		private void OnDisable()
		{
			CancelPendingParticleObjectRoutines();
		}

		private void OnDestroy()
		{
			CancelPendingParticleObjectRoutines();
		}

		private void CancelPendingParticleObjectRoutines()
		{
			if (_pendingParticleObjectRoutines.Count == 0)
			{
				return;
			}
			for (int i = 0; i < _pendingParticleObjectRoutines.Count; i++)
			{
				Coroutine routine = _pendingParticleObjectRoutines[i];
				if (routine != null)
				{
					DelayedWorker.Kill(routine);
				}
			}
			_pendingParticleObjectRoutines.Clear();
		}
	}
}
