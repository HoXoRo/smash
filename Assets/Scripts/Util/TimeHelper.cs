using System;
using System.Collections;
using Service;
using UnityEngine;

namespace Util
{
	public class TimeHelper : ServiceMonoBehaviour
	{
		public Action OnTick;

		private Coroutine _timerRoutine;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void CreateInstance()
		{
			if (ServiceLocator.Get<TimeHelper>() != null || UnityEngine.Object.FindObjectOfType<TimeHelper>() != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("TimeHelper");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<TimeHelper>();
		}

		protected override void Awake()
		{
			TimeHelper existing = ServiceLocator.Get<TimeHelper>();
			if (existing != null && existing != this)
			{
				Destroy(gameObject);
				return;
			}
			TimeHelper[] helpers = FindObjectsOfType<TimeHelper>();
			for (int i = 0; i < helpers.Length; i++)
			{
				if (helpers[i] != this)
				{
					Destroy(gameObject);
					return;
				}
			}
			base.Awake();
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
		}

		private void Start()
		{
			EnsureTimerRoutine();
		}

		private void OnEnable()
		{
			EnsureTimerRoutine();
		}

		private void OnDisable()
		{
			CancelTimerRoutine();
		}

		private IEnumerator TimerRoutine()
		{
			WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
			while (true)
			{
				yield return wait;
				Tick();
			}
		}

		private void Tick()
		{
			OnTick?.Invoke();
		}

		protected override void OnDestroy()
		{
			CancelTimerRoutine();
			base.OnDestroy();
		}

		private void EnsureTimerRoutine()
		{
			if (_timerRoutine == null)
			{
				_timerRoutine = StartCoroutine(TimerRoutine());
			}
		}

		private void CancelTimerRoutine()
		{
			if (_timerRoutine != null)
			{
				StopCoroutine(_timerRoutine);
				_timerRoutine = null;
			}
		}
	}
}
