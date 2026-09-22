using UnityEngine;

namespace Core
{
	public class FocusHelper : MonoBehaviour
	{
		private static FocusHelper _instance;

		private bool _hasFocus;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			if (Object.FindObjectOfType<FocusHelper>() != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("FocusHelper");
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<FocusHelper>();
		}

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			_instance = this;
			Object.DontDestroyOnLoad(gameObject);
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			HandleFocusChange(hasFocus);
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			HandleFocusChange(!pauseStatus);
		}

		private void HandleFocusChange(bool hasFocus)
		{
			_hasFocus = hasFocus;
		}

		private void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}
	}
}
