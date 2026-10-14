using Gameplay.Objects;
using UnityEngine;

namespace Gameplay.Collisions
{
	public class CollisionForwarder : MonoBehaviour
	{
		private BaseObject _objectInstance;

		public void SetObjectInstance(BaseObject objectInstance)
		{
			_objectInstance = objectInstance;
		}

		private void OnCollisionEnter(Collision collision)
		{
			_objectInstance?.OnCollision(collision);
		}

		private void OnTriggerEnter(Collider other)
		{
			_objectInstance?.OnTrigger(other);
		}

		public BaseObject GetObjectInstance()
		{
			return _objectInstance;
		}
	}
}
