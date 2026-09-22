using System.Collections;
using System.Collections.Generic;
using Gameplay.Objects;
using Service;
using UnityEngine;
using Util;

namespace Gameplay.Collisions
{
	public class PhysicsWarmup : ServiceMonoBehaviour
	{
		private Coroutine _warmupRoutine;

		private readonly List<BaseObject> _activeWarmupObjects = new List<BaseObject>();

		public void RunWarmup(List<BaseObject> objects)
		{
			CancelActiveWarmup();
			_warmupRoutine = StartCoroutine(WarmupRoutine(objects));
		}

		private IEnumerator WarmupRoutine(List<BaseObject> objects)
		{
			if (objects == null || objects.Count == 0)
			{
				_warmupRoutine = null;
				yield break;
			}
			PhysicsPerformanceSettings settings = PhysicsPerformanceSettingsProvider.GetSettings(DevicePerformanceClassifier.GetTier());
			_activeWarmupObjects.Clear();
			foreach (BaseObject obj in objects)
			{
				if (obj != null && obj.rb != null)
				{
					_activeWarmupObjects.Add(obj);
					obj.rb.solverIterations = settings.WarmupSolverIterations;
					obj.rb.solverVelocityIterations = settings.WarmupSolverVelocityIterations;
				}
			}
			int steps = Mathf.Max(1, Mathf.CeilToInt(settings.WarmupSeconds / Time.fixedDeltaTime));
			for (int i = 0; i < steps; i++)
				{
					yield return new WaitForFixedUpdate();
				}
			RestoreNormalSolverIterations(settings);
			_warmupRoutine = null;
			_activeWarmupObjects.Clear();
		}

		protected override void OnDestroy()
		{
			CancelActiveWarmup();
			base.OnDestroy();
		}

		protected virtual void OnDisable()
		{
			CancelActiveWarmup();
		}

		private void CancelActiveWarmup()
		{
			if (_warmupRoutine != null)
			{
				StopCoroutine(_warmupRoutine);
				_warmupRoutine = null;
			}
			RestoreNormalSolverIterations();
			_activeWarmupObjects.Clear();
		}

		private void RestoreNormalSolverIterations(PhysicsPerformanceSettings? cachedSettings = null)
		{
			PhysicsPerformanceSettings settings = cachedSettings ?? PhysicsPerformanceSettingsProvider.GetSettings(DevicePerformanceClassifier.GetTier());
			for (int i = 0; i < _activeWarmupObjects.Count; i++)
			{
				BaseObject obj = _activeWarmupObjects[i];
				if (obj != null && obj.rb != null)
				{
					obj.rb.solverIterations = settings.NormalSolverIterations;
					obj.rb.solverVelocityIterations = settings.NormalSolverVelocityIterations;
				}
			}
		}
	}
}
