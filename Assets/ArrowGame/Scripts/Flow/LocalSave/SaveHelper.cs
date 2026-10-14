using System.Collections;
using UnityEngine;

namespace LocalSave
{
	public class SaveHelper : MonoBehaviour
	{
		private static SaveHelper _instance;

		private Coroutine _autoSaveCoroutine;

		private float _autoSaveInterval;

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}

		public void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				SaveService.Save();
			}
		}

		public void OnApplicationQuit()
		{
			SaveService.Save();
		}

		public void StartAutoSave(float interval)
		{
			_autoSaveInterval = interval;
			if (_autoSaveCoroutine != null)
			{
				StopCoroutine(_autoSaveCoroutine);
			}
			_autoSaveCoroutine = StartCoroutine(AutoSaveRoutine(interval));
		}

		private void OnEnable()
		{
			if (_autoSaveInterval > 0f && _autoSaveCoroutine == null)
			{
				_autoSaveCoroutine = StartCoroutine(AutoSaveRoutine(_autoSaveInterval));
			}
		}

		private void OnDisable()
		{
			if (_autoSaveCoroutine != null)
			{
				StopCoroutine(_autoSaveCoroutine);
				_autoSaveCoroutine = null;
			}
		}

		private IEnumerator AutoSaveRoutine(float interval)
		{
			WaitForSecondsRealtime intervalTime = new WaitForSecondsRealtime(interval);
			while (true)
			{
				yield return intervalTime;
				SaveService.Save();
			}
		}

		private void OnDestroy()
		{
			OnDisable();
			if (_instance == this)
			{
				_instance = null;
			}
		}
	}
}
