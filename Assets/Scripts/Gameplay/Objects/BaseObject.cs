using System;
using System.ComponentModel;
using Audio;
using Gameplay.Collisions;
using Level;
using Service;
using SRDebugger;
using UnityEngine;
using Util;

namespace Gameplay.Objects
{
	public abstract class BaseObject : MonoBehaviour, IContactNodeOwner
	{
		[SROption]
		[Category("Test")]
		public static float CanRocketBoomMultiplier;

		public ObjectType objectType;

		public FloatTriplet size;

		public Rigidbody rb;

		public MeshRenderer meshRenderer;

		private Quaternion _lastRotation;

		private Vector3 _lastVelocity;

		protected LevelObjectData ObjectData;

		private bool _dampened;

		protected Collider Collider;

		[NonSerialized]
		public bool BeingDestroyed;

		[NonSerialized]
		public ContactNode ContactNode;

		private const float AngularDrag = 0f;

		private const float Drag = 0f;

		public const float DynamicFriction = 0.6f;

		public const float StaticFriction = 0.6f;

		public const float Bounciness = 0f;

		public Table TouchingTable { get; set; }

		public Vector3 ApplyRocketHitBoomMultiplier(Vector3 impulse)
		{
			if (objectType == ObjectType.CanCylinder || objectType == ObjectType.CanCone || objectType == ObjectType.CanSquare || objectType == ObjectType.Wormhole)
			{
				return impulse * CanRocketBoomMultiplier;
			}
			return impulse;
		}

		public virtual void Initialize(LevelObjectData objectData)
		{
			ObjectData = objectData;
			if (objectData == null)
			{
				return;
			}
			objectType = objectData.type;
			size = objectData.size;
		}

		protected abstract int GetLayer();

		public abstract Audio.AudioType GetHitAudioType();

		protected virtual void Start()
		{
			if (rb != null)
			{
				CollisionForwarder collisionForwarder = rb.gameObject.AddComponent<CollisionForwarder>();
				collisionForwarder.SetObjectInstance(this);
				ContactNode = rb.gameObject.AddComponent<ContactNode>();
				ContactNode.SetType(ContactNodeType.Object);
				ContactNode.MasterObject = this;
				rb.gameObject.layer = GetLayer();
				rb.angularDamping = AngularDrag;
				rb.linearDamping = Drag;
				_lastRotation = rb.rotation;
				_lastVelocity = rb.linearVelocity;
			}
			Collider = GetComponentInChildren<Collider>();
			PhysicsMaterial material = Resources.Load<PhysicsMaterial>("PhysicsMat");
			if (Collider != null && material != null)
			{
				Collider.material = material;
				Collider.material.dynamicFriction = DynamicFriction;
				Collider.material.staticFriction = StaticFriction;
				Collider.material.bounciness = Bounciness;
			}
		}

		protected virtual void FixedUpdate()
		{
			if (rb == null)
			{
				return;
			}
			_lastRotation = rb.rotation;
			_lastVelocity = rb.linearVelocity;
		}

		private float GetDampeningRatio()
		{
			return _dampened ? 0.5f : 1f;
		}

		public virtual void SetObjectMass()
		{
			if (rb != null)
			{
				rb.mass = GetMass(objectType, size.GetVolume());
			}
		}

		public void OnCollision(Collision collision)
		{
			if (collision != null && collision.collider != null && collision.collider.CompareTag("Ground"))
			{
				OnGroundHit(collision);
			}
		}

		public void OnTrigger(Collider other)
		{
			if (other != null && other.CompareTag("DampenArea") && !_dampened)
			{
				_dampened = true;
				if (rb != null)
				{
					rb.linearVelocity *= GetDampeningRatio();
					rb.angularVelocity *= GetDampeningRatio();
				}
			}
		}

		protected Quaternion GetRbDeltaRotation()
		{
			if (rb == null)
			{
				return Quaternion.identity;
			}
			return rb.rotation * Quaternion.Inverse(_lastRotation);
		}

		protected Vector3 GetLastVelocity()
		{
			return _lastVelocity;
		}

		protected Quaternion GetLastRotation()
		{
			return _lastRotation;
		}

		public bool HasSignificantMovement()
		{
			Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
			Vector3 angularVelocity = rb != null ? rb.angularVelocity : Vector3.zero;
			if (TouchingTable != null)
			{
				velocity -= TouchingTable.GetCurrentMovementVector();
				velocity -= TouchingTable.GetTangentialVelocity(transform.position);
				angularVelocity -= TouchingTable.GetCurrentRotationVector();
			}
			return velocity.sqrMagnitude > 0.01f || angularVelocity.sqrMagnitude > 0.01f;
		}

		protected abstract void OnGroundHit(Collision collision);

		public abstract BallObjectCollisionResult OnBallHit(BallObjectCollision collision);

		protected void DestroyObject()
		{
			if (BeingDestroyed)
			{
				return;
			}
			BeingDestroyed = true;
			ServiceLocator.Get<GameController>()?.DestroyObject(this);
		}

		public abstract void DestroyByTnt();

		public abstract void DestroyByRocket();

		public virtual LevelObjectData GetDataForLevel()
		{
			Table table = GetComponentInParent<Table>(true);
			Vector3 localPosition = table != null ? table.transform.InverseTransformPoint(transform.position) : transform.position;
			Quaternion localRotation = table != null ? Quaternion.Inverse(table.transform.rotation) * transform.rotation : transform.rotation;
			return new LevelObjectData
			{
				tableId = table != null ? table.Id : 0,
				type = objectType,
				size = size,
				pos = new FloatTriplet(localPosition.x, localPosition.y, localPosition.z),
				rot = new FloatQuartet(localRotation.x, localRotation.y, localRotation.z, localRotation.w)
			};
		}

		public static float GetMass(ObjectType type, float volume)
		{
			return ObjectMassMaps.GetRemainingObjectMass(type, volume);
		}

		static BaseObject()
		{
			CanRocketBoomMultiplier = 0.8f;
		}
	}
}
