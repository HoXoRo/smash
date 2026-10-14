using System;
using System.Collections;
using UnityEngine;

namespace Core
{
	public class DelayedWorker : MonoBehaviour
	{
		private static DelayedWorker _appInstance;

		private static DelayedWorker _sceneInstance;

		private static DelayedWorker AppInstance
		{
			get
			{
				if (_appInstance == null)
				{
					_appInstance = FindExistingInstance("AppDelayedWorker") ?? CreateInstance("AppDelayedWorker", true);
				}
				return _appInstance;
			}
		}

		private static DelayedWorker SceneInstance
		{
			get
			{
				if (_sceneInstance == null)
				{
					_sceneInstance = FindExistingInstance("SceneDelayedWorker") ?? CreateInstance("SceneDelayedWorker", false);
				}
				return _sceneInstance;
			}
		}

		public static Coroutine CallAfter(float delay, Action action, LifeType type = LifeType.Scene)
		{
			return (type == LifeType.App ? AppInstance : SceneInstance).CallAfterOnInstance(delay, action);
		}

		private Coroutine CallAfterOnInstance(float delay, Action action)
		{
			return StartCoroutine(CallAfterRoutine(delay, action));
		}

		public static Coroutine CallAtFrameEnd(Action action)
		{
			return SceneInstance.CallAtFrameEndOnInstance(action);
		}

		private Coroutine CallAtFrameEndOnInstance(Action action)
		{
			return StartCoroutine(CallAtFrameEndRoutine(action));
		}

		public static void Kill(Coroutine routine)
		{
			if (routine == null)
			{
				return;
			}
			if (_sceneInstance != null)
			{
				_sceneInstance.KillOnInstance(routine);
			}
			if (_appInstance != null)
			{
				_appInstance.KillOnInstance(routine);
			}
		}

		private void KillOnInstance(Coroutine routine)
		{
			StopCoroutine(routine);
		}

		private IEnumerator CallAfterRoutine(float delay, Action action)
		{
			yield return new WaitForSeconds(delay);
			action?.Invoke();
		}

		private IEnumerator CallAtFrameEndRoutine(Action action)
		{
			yield return new WaitForEndOfFrame();
			action?.Invoke();
		}

		private void Awake()
		{
			if (gameObject.name == "AppDelayedWorker")
			{
				if (_appInstance != null && _appInstance != this)
				{
					Destroy(gameObject);
					return;
				}
				_appInstance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (gameObject.name == "SceneDelayedWorker")
			{
				if (_sceneInstance != null && _sceneInstance != this)
				{
					Destroy(gameObject);
					return;
				}
				_sceneInstance = this;
			}
		}

		private void OnDestroy()
		{
			if (_appInstance == this)
			{
				_appInstance = null;
			}
			if (_sceneInstance == this)
			{
				_sceneInstance = null;
			}
		}

		private static DelayedWorker CreateInstance(string name, bool dontDestroyOnLoad)
		{
			GameObject gameObject = new GameObject(name);
			if (dontDestroyOnLoad)
			{
				DontDestroyOnLoad(gameObject);
			}
			return gameObject.AddComponent<DelayedWorker>();
		}

		private static DelayedWorker FindExistingInstance(string name)
		{
			DelayedWorker[] workers = FindObjectsOfType<DelayedWorker>();
			for (int i = 0; i < workers.Length; i++)
			{
				DelayedWorker worker = workers[i];
				if (worker != null && worker.gameObject.name == name)
				{
					return worker;
				}
			}
			return null;
		}
	}
}
