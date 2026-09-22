using System;
using System.Collections.Generic;
using System.ComponentModel;
using Audio;
using DG.Tweening;
using Gameplay.Collisions;
using Gameplay.Objects;
using Gameplay.Particles;
using Haptics;
using SRDebugger;
using Service;
using UnityEngine;
using Util;

namespace Gameplay
{
	public class Ball : MonoBehaviour, IContactNodeOwner
	{
		public Rigidbody rb;

		[SerializeField]
		private MeshRenderer ballRenderer;

		[SerializeField]
		private Collider ballCollider;

		[NonSerialized]
		public BallState State;

		[SROption]
		[Category("Test")]
		public static float BallRadius;

		[SROption]
		[Category("Test")]
		public static float MassMultiplier;

		private Cannon _cannon;

		private Material _material;

		private bool _beingDestroyed;

		private float _destroyTimer;

		private const float BallHitSpeed = 90f;

		private const float FirstHitBoomPower = 3f;

		private static float _ballRelativeVelocityThresholdSqr;

		private bool _slowedAfterFirstHit;

		private RaycastHit[] _hits;

		private readonly List<float> _velocityCutoffsOnBreak = new List<float>
		{
			0.5f,
			0.2f,
			0.2f,
			0.2f,
			0.2f
		};

		private int _velocityCutoffsOnBreakIndex;

		private int _triggerSweepMask;

		private int _colliderSweepMask;

		private int _opacityPropId;

		private int _specularRoughnessPropId;

		private int _metalnessPropId;

		private float _startingOpacity;

		private float _startingSpecularRoughness;

		private float _startingMetalness;

		private bool _changedDrag;

		[NonSerialized]
		public ContactNode ContactNode;

		private Tweener _destroyFadeTween;

		public Table TouchingTable { get; set; }

		public void Initialize(Cannon cannon)
		{
			_cannon = cannon;
			_hits = new RaycastHit[10];
			_triggerSweepMask = LayerMask.GetMask("BreakableTrigger");
			_colliderSweepMask = LayerMask.GetMask("ObjectCollider", "BreakableCollider", "Obstacle");
			_opacityPropId = Shader.PropertyToID("_OPACITY");
			_specularRoughnessPropId = Shader.PropertyToID("_SPECULAR_ROUGHNESS");
			_metalnessPropId = Shader.PropertyToID("_METALNESS");
			if (ballRenderer != null)
			{
				_material = ballRenderer.material;
				if (_material != null)
				{
					_startingOpacity = _material.GetFloat(_opacityPropId);
					_startingSpecularRoughness = _material.GetFloat(_specularRoughnessPropId);
					_startingMetalness = _material.GetFloat(_metalnessPropId);
				}
			}
			ContactNode = GetComponent<ContactNode>() ?? gameObject.AddComponent<ContactNode>();
			ContactNode.SetType(ContactNodeType.Ball);
			ContactNode.MasterObject = this;
			Recycle();
		}

		public void Shoot(Vector3 direction, float ballSpeed)
		{
			KillActiveTweens();
			gameObject.SetActive(true);
			State = BallState.Shot;
			_beingDestroyed = false;
			_destroyTimer = 0f;
			_changedDrag = false;
			_slowedAfterFirstHit = false;
			TouchingTable = null;
			if (ballCollider != null)
			{
				ballCollider.enabled = true;
			}
			if (rb != null)
			{
				rb.isKinematic = false;
				rb.linearDamping = 0f;
				rb.linearVelocity = direction.normalized * ballSpeed;
				rb.angularVelocity = Vector3.zero;
			}
			transform.localScale = Vector3.zero;
			PlayScaleAnimation();
		}

		public void Recycle()
		{
			KillActiveTweens();
			_velocityCutoffsOnBreakIndex = 0;
			_slowedAfterFirstHit = false;
			_destroyTimer = 0f;
			_beingDestroyed = false;
			_changedDrag = false;
			TouchingTable = null;
			State = BallState.HitGround;
			transform.localScale = Vector3.one;
			if (rb != null)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				rb.linearDamping = 0f;
			}
			if (ballCollider != null)
			{
				ballCollider.enabled = true;
			}
			if (_material != null)
			{
				_material.SetFloat(_opacityPropId, _startingOpacity);
				_material.SetFloat(_specularRoughnessPropId, _startingSpecularRoughness);
				_material.SetFloat(_metalnessPropId, _startingMetalness);
			}
			gameObject.SetActive(false);
		}

		private void PlayScaleAnimation()
		{
			transform.DOKill();
			transform.localScale = Vector3.zero;
			transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack);
		}

		public void SetMass(float mass)
		{
			if (rb != null)
			{
				rb.mass = Mathf.Max(0.01f, mass * MassMultiplier);
			}
		}

		private void Update()
		{
			CheckForDragChange();
			CheckForDestroy();
		}

		private void CheckForDragChange()
		{
			if (_changedDrag || State != BallState.Shot || rb == null)
			{
				return;
			}
			GameController gameController = ServiceLocator.Get<GameController>();
			if (gameController != null && transform.position.z > gameController.TableEndLineZ)
			{
				rb.linearDamping = 0.5f;
				_changedDrag = true;
			}
		}

		private void FixedUpdate()
		{
			if (State != BallState.Shot || rb == null)
			{
				return;
			}
			SweepForColliders();
			SweepForTriggers();
		}

		private void SweepForTriggers()
		{
			if (_hits == null || ballCollider == null || rb == null)
			{
				return;
			}
			Vector3 velocity = rb.linearVelocity;
			float distance = velocity.magnitude * Time.fixedDeltaTime;
			if (distance <= 0f)
			{
				return;
			}
			float radius = ballCollider.bounds.extents.x;
			int hitCount = Physics.SphereCastNonAlloc(rb.position, radius, velocity.normalized, _hits, distance, _triggerSweepMask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < hitCount; i++)
			{
				Collider hitCollider = _hits[i].collider;
				if (hitCollider != null)
				{
					OnHitObjectTrigger(hitCollider);
				}
			}
		}

		private void SweepForColliders()
		{
			if (_hits == null || ballCollider == null || rb == null)
			{
				return;
			}
			Vector3 velocity = rb.linearVelocity;
			float distance = velocity.magnitude * Time.fixedDeltaTime;
			if (distance <= 0f)
			{
				return;
			}
			float radius = ballCollider.bounds.extents.x;
			int hitCount = Physics.SphereCastNonAlloc(rb.position, radius, velocity.normalized, _hits, distance, _colliderSweepMask, QueryTriggerInteraction.Ignore);
			if (hitCount > 0)
			{
				ApplySlowBeforeFirstHit(GetCurrentVelocityPair());
			}
		}

		private void CheckForDestroy()
		{
			if (_beingDestroyed)
			{
				return;
			}
			if (State != BallState.HitGround && HasSignificantRelativeMovement())
			{
				_destroyTimer = 0f;
				return;
			}

			if (transform.position.z > 30f)
			{
				DestroyBallInstant();
				return;
			}

			_destroyTimer += Time.deltaTime;
			float destroyDelay = State == BallState.HitGround ? 1.5f : 0.5f;
			if (_destroyTimer > destroyDelay)
			{
				_beingDestroyed = true;
				DestroyBall();
			}
		}

		private void DestroyBall()
		{
			if (_material == null)
			{
				DestroyBallInstant();
				return;
			}
			float fadeDuration = State == BallState.HitGround ? 0.75f : 0.5f;
			KillDestroyFadeTween();
			_destroyFadeTween = DOVirtual.Float(1f, 0f, fadeDuration, delegate(float t)
			{
				_material.SetFloat(_opacityPropId, _startingOpacity * t);
				_material.SetFloat(_specularRoughnessPropId, _startingSpecularRoughness * t);
				_material.SetFloat(_metalnessPropId, _startingMetalness * t);
			}).OnComplete(delegate
			{
				_destroyFadeTween = null;
				DestroyBallInstant();
			});
		}

		private void DestroyBallInstant()
		{
			_cannon?.OnBallDestroyed(this);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other != null && other.CompareTag("Object"))
			{
				OnHitObjectTrigger(other);
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (collision == null || collision.collider == null)
			{
				return;
			}
			Collider other = collision.collider;
			if (other.CompareTag("Object"))
			{
				OnHitObject(collision);
			}
			else if (other.CompareTag("Blocker"))
			{
				OnHitBlocker(collision);
			}
			else if (other.CompareTag("Tabletop"))
			{
				OnHitTabletop(collision);
			}
			else if (other.CompareTag("Ground"))
			{
				OnHitGround(collision);
			}
		}

		private void OnHitBlocker(Collision collision)
		{
			if (State != BallState.Shot)
			{
				return;
			}
			Vector3 hitPoint = collision != null && collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.BallObjectFirstHit, hitPoint, Quaternion.identity, rb != null ? rb.linearVelocity : Vector3.zero);
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.BlockerHit);
			HapticManager.PlayCustom();
			State = BallState.HitObject;
		}

		private void OnHitObject(Collision collision)
		{
			bool isFirstHit = State == BallState.Shot;
			State = BallState.HitObject;
			if (collision == null || collision.collider == null || !collision.collider.TryGetComponent(out CollisionForwarder forwarder))
			{
				return;
			}
			BaseObject objectInstance = forwarder.GetObjectInstance();
			if (objectInstance == null)
			{
				return;
			}
			ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default(ContactPoint);
			BallObjectCollision ballObjectCollision = new BallObjectCollision
			{
				BallCollider = ballCollider,
				ObjectCollider = collision.collider,
				RelativeVelocity = collision.relativeVelocity,
				Point = collision.contactCount > 0 ? contact.point : transform.position
			};
			OnHitInternal(objectInstance, ballObjectCollision, isFirstHit);
		}

		private void ApplyBoomOnFirstHit(BallObjectCollision collision)
		{
			ContactNode startNode = null;
			if (collision?.ObjectCollider != null)
			{
				if (collision.ObjectCollider.TryGetComponent(out CollisionForwarder forwarder))
				{
					startNode = forwarder.GetObjectInstance()?.ContactNode;
				}
				else if (collision.ObjectCollider.TryGetComponent(out BreakableTrigger breakableTrigger))
				{
					startNode = breakableTrigger.GetObjectInstance()?.ContactNode;
				}
			}
			if (startNode == null)
			{
				return;
			}
			HashSet<ContactNode> connected = new HashSet<ContactNode>();
			Queue<ContactNode> queue = new Queue<ContactNode>();
			ContactGraph.CollectConnected(startNode, connected, queue, ContactNodeType.Object);
			Vector3 boomOrigin = rb != null ? rb.worldCenterOfMass : collision.Point;
			foreach (ContactNode node in connected)
			{
				if (node == null || node == startNode || node.MasterObject is not BaseObject baseObject || baseObject.rb == null)
				{
					continue;
				}
				Vector3 direction = baseObject.rb.worldCenterOfMass - boomOrigin;
				if (direction.sqrMagnitude <= 0.0001f)
				{
					direction = Vector3.up;
				}
				Vector3 impulse = baseObject.ApplyRocketHitBoomMultiplier(direction.normalized * FirstHitBoomPower);
				baseObject.rb.AddForce(impulse, ForceMode.VelocityChange);
			}
		}

		private void OnHitObjectTrigger(Collider other)
		{
			bool isFirstHit = State == BallState.Shot;
			State = BallState.HitObject;
			if (other == null || !other.TryGetComponent(out BreakableTrigger breakableTrigger))
			{
				return;
			}
			BaseObject objectInstance = breakableTrigger.GetObjectInstance();
			if (objectInstance == null)
			{
				return;
			}
			Vector3 otherVelocity = breakableTrigger.GetMasterRb() != null ? breakableTrigger.GetMasterRb().linearVelocity : Vector3.zero;
			BallObjectCollision ballObjectCollision = new BallObjectCollision
			{
				BallCollider = ballCollider,
				ObjectCollider = breakableTrigger.GetCollider(),
				RelativeVelocity = (rb != null ? rb.linearVelocity : Vector3.zero) - otherVelocity,
				Point = other.ClosestPoint(transform.position)
			};
			OnHitInternal(objectInstance, ballObjectCollision, isFirstHit);
		}

		private void OnHitInternal(BaseObject objectInstance, BallObjectCollision ballObjectCollision, bool isFirstHit)
		{
			if (objectInstance == null)
			{
				return;
			}
			HapticManager.PlayCustom();
			VelocityPair pair = GetCurrentVelocityPair();
			BallObjectCollisionResult result = objectInstance.OnBallHit(ballObjectCollision);
			if (result == BallObjectCollisionResult.Break)
			{
				ApplyCutoffOnBreak(pair);
				ApplyVelocityPairToRb(pair);
				return;
			}
			if (result == BallObjectCollisionResult.BallDestroy)
			{
				DestroyBallInstant();
				return;
			}
			if (!isFirstHit)
			{
				return;
			}
			Audio.AudioType hitAudioType = objectInstance.GetHitAudioType();
			if (hitAudioType != Audio.AudioType.None)
			{
				ServiceLocator.Get<AudioHelper>()?.PlaySfx(hitAudioType);
			}
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.BallObjectFirstHit, ballObjectCollision.Point, Quaternion.identity, rb != null ? rb.linearVelocity : Vector3.zero);
			ApplyBoomOnFirstHit(ballObjectCollision);
		}

		private VelocityPair GetCurrentVelocityPair()
		{
			return new VelocityPair(rb != null ? rb.linearVelocity : Vector3.zero, rb != null ? rb.angularVelocity : Vector3.zero);
		}

		private bool ApplyCutoffOnBreak(VelocityPair pair)
		{
			if (pair == null || _velocityCutoffsOnBreakIndex >= _velocityCutoffsOnBreak.Count)
			{
				return false;
			}
			float cutoff = _velocityCutoffsOnBreak[_velocityCutoffsOnBreakIndex++];
			float remainingMultiplier = 1f - cutoff;
			pair.LinearVelocity *= remainingMultiplier;
			pair.AngularVelocity *= remainingMultiplier;
			return true;
		}

		private void ApplySlowBeforeFirstHit(VelocityPair pair)
		{
			if (_slowedAfterFirstHit || pair == null)
			{
				return;
			}
			_slowedAfterFirstHit = true;
			Vector3 linearVelocity = pair.LinearVelocity;
			pair.LinearVelocity = linearVelocity.sqrMagnitude > 0.00001f ? linearVelocity.normalized * BallHitSpeed : Vector3.zero;
			pair.AngularVelocity *= 0.5f;
			ApplyVelocityPairToRb(pair);
		}

		private void OnHitTabletop(Collision collision)
		{
			State = BallState.HitObject;
			TouchingTable = collision != null ? collision.collider.GetComponentInParent<Table>() : null;
			HapticManager.PlayCustom();
		}

		private void ApplyVelocityPairToRb(VelocityPair pair)
		{
			if (rb == null || pair == null)
			{
				return;
			}
			rb.linearVelocity = pair.LinearVelocity;
			rb.angularVelocity = pair.AngularVelocity;
		}

		private void OnHitGround(Collision collision)
		{
			if (State != BallState.HitGround)
			{
				Vector3 hitPoint = collision != null && collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
				ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.BallGroundHit, hitPoint, Quaternion.identity, rb != null ? rb.linearVelocity : Vector3.zero);
				VelocityPair pair = GetCurrentVelocityPair();
				pair.LinearVelocity = new Vector3(pair.LinearVelocity.x * 0.6f, 0f, pair.LinearVelocity.z * 0.6f);
				pair.AngularVelocity *= 0.6f;
				ApplyVelocityPairToRb(pair);
			}
			HapticManager.PlayCustom();
			TouchingTable = null;
			State = BallState.HitGround;
		}

		public bool HasSignificantMovement()
		{
			return HasSignificantRelativeMovement();
		}

		private bool HasSignificantRelativeMovement()
		{
			Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
			if (TouchingTable != null)
			{
				velocity -= TouchingTable.GetCurrentMovementVector();
				velocity -= TouchingTable.GetTangentialVelocity(transform.position);
			}
			return velocity.sqrMagnitude > _ballRelativeVelocityThresholdSqr;
		}

		public Vector3 ApplyRocketHitBoomMultiplier(Vector3 impulse)
		{
			return impulse;
		}

		private void OnDestroy()
		{
			KillActiveTweens();
		}

		private void KillActiveTweens()
		{
			transform.DOKill();
			KillDestroyFadeTween();
		}

		private void KillDestroyFadeTween()
		{
			if (_destroyFadeTween != null)
			{
				_destroyFadeTween.Kill();
				_destroyFadeTween = null;
			}
		}

		static Ball()
		{
			BallRadius = 1f;
			MassMultiplier = 1f;
			_ballRelativeVelocityThresholdSqr = 9f;
		}
	}
}
