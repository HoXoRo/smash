using UnityEngine;

namespace Core
{
	public class RoutineRunner : MonoBehaviour
	{
		private static RoutineRunner _instance;

		public static RoutineRunner Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Object.FindObjectOfType<RoutineRunner>();
					if (_instance == null)
					{
						GameObject gameObject = new GameObject("RoutineRunner");
						Object.DontDestroyOnLoad(gameObject);
						_instance = gameObject.AddComponent<RoutineRunner>();
					}
				}
				return _instance;
			}
		}

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Object.Destroy(gameObject);
				return;
			}
			_instance = this;
			Object.DontDestroyOnLoad(gameObject);
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
