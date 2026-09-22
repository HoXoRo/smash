using Gameplay.Objects;
using UnityEngine;

namespace Gameplay.Collisions
{
	public class BreakableTrigger : MonoBehaviour
	{
		private BaseObject _objectInstance;

		private Collider _collider;

		private Rigidbody _masterRb;

		public void Init(BaseObject objectInstance, Collider colliderToCopy, Rigidbody rb)
		{
			_masterRb = rb;
			_objectInstance = objectInstance;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
			if (colliderToCopy != null)
			{
				_collider = ColliderUtils.CopyCollider(colliderToCopy, gameObject);
			}
			gameObject.layer = LayerMask.NameToLayer("BreakableTrigger");
			gameObject.tag = "Object";
		}

		public BaseObject GetObjectInstance()
		{
			return _objectInstance;
		}

		public Collider GetCollider()
		{
			return _collider;
		}

		public Rigidbody GetMasterRb()
		{
			return _masterRb;
		}
	}
}
