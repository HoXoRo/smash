using UnityEngine;

namespace Service
{
	public class ServiceMonoBehaviour : MonoBehaviour
	{
		protected virtual void Awake()
		{
			ServiceLocator.Register(GetType(), this);
		}

		protected virtual void OnDestroy()
		{
			ServiceLocator.Unregister(GetType(), this);
		}
	}
}
