using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Particles
{
	public class ParticleObject : MonoBehaviour
	{
		[SerializeField]
		private List<MeshRenderer> meshRenderers;

		[SerializeField]
		private List<Rigidbody> rbs;

		[SerializeField]
		private float lifetime;

		[SerializeField]
		[Header("Movement")]
		private Vector3 minMovement;

		[SerializeField]
		private Vector3 maxMovement;

		[Header("Size")]
		[SerializeField]
		private AnimationCurve sizeOverLifetimeX;

		[SerializeField]
		private AnimationCurve sizeOverLifetimeY;

		[SerializeField]
		private AnimationCurve sizeOverLifetimeZ;

		[Header("Rotation")]
		[SerializeField]
		private Vector3 minRotation;

		[SerializeField]
		private Vector3 maxRotation;

		[Header("Color")]
		[SerializeField]
		private Gradient colorOverLifetime;

		[Header("Physics")]
		[SerializeField]
		private float gravityScale;

		[SerializeField]
		private float bounce;

		[SerializeField]
		private float dampen;

		[SerializeField]
		private float velocityFromObjectMultiplier;

		private ParticlePlayer _attachedPlayer;

		private float _fixedUpdateTimer;

		private float _updateTimer;

		private List<Material> _materials;

		private List<Transform> _transforms;

		private float _lifeNormalized;

		private float[] _lastYVelocities;

		private bool[] _collidedOnceList;

		private Vector3[] _pendingVelocityChanges;

		private List<Color> _startingColors;

		private List<Vector3> _startingSizes;

		private readonly int _colorProperty = Shader.PropertyToID("_Color");

		private static int _particleObjectLayer;

		private static int _groundHitParticleObjectLayer;

		private Vector3 _spawnVelocity;

		private bool _destroyNotified;

		public void SetSpawnVelocity(Vector3 velocity)
		{
			_spawnVelocity = velocity;
		}

		public void SetPlayer(ParticlePlayer player)
		{
			_attachedPlayer = player;
		}

		public void Play()
		{
			if (_particleObjectLayer == 0)
			{
				_particleObjectLayer = LayerMask.NameToLayer("ParticleObject");
				_groundHitParticleObjectLayer = LayerMask.NameToLayer("GroundHitParticleObject");
			}
			GatherAndSaveRefs();
			_fixedUpdateTimer = 0f;
			_updateTimer = 0f;
			_materials = meshRenderers.Select((MeshRenderer x) => x.material).ToList();
			_transforms = meshRenderers.Select((MeshRenderer x) => x.transform).ToList();
			_startingColors = _materials.Select((Material x) => x.HasProperty(_colorProperty) ? x.GetColor(_colorProperty) : Color.white).ToList();
			_startingSizes = _transforms.Select((Transform x) => x.localScale).ToList();
			_lastYVelocities = new float[rbs.Count];
			_collidedOnceList = new bool[rbs.Count];
			_pendingVelocityChanges = new Vector3[rbs.Count];
			for (int i = 0; i < rbs.Count; i++)
			{
				Rigidbody body = rbs[i];
				if (body == null)
				{
					continue;
				}
				body.gameObject.layer = _particleObjectLayer;
				Vector3 randomVelocity = new Vector3(
					Random.Range(minMovement.x, maxMovement.x),
					Random.Range(minMovement.y, maxMovement.y),
					Random.Range(minMovement.z, maxMovement.z));
				body.linearVelocity = new Vector3(
					randomVelocity.x + _spawnVelocity.x * velocityFromObjectMultiplier,
					randomVelocity.y,
					randomVelocity.z + _spawnVelocity.z * velocityFromObjectMultiplier);
				body.angularVelocity = new Vector3(
					Random.Range(minRotation.x, maxRotation.x),
					Random.Range(minRotation.y, maxRotation.y),
					Random.Range(minRotation.z, maxRotation.z));
				body.useGravity = false;
				ParticleObjectForwarder forwarder = body.gameObject.AddComponent<ParticleObjectForwarder>();
				forwarder.Init(this, i);
				Collider collider = body.GetComponent<Collider>();
				if (collider != null)
				{
					PhysicsMaterial material = collider.material;
					if (material != null)
					{
						material.dynamicFriction = 0.6f;
						material.staticFriction = 0.6f;
						material.frictionCombine = PhysicsMaterialCombine.Minimum;
					}
				}
				else
				{
					Debug.LogError("Collider is null");
				}
			}
		}

		private void FixedUpdate()
		{
			if (rbs == null || _pendingVelocityChanges == null)
			{
				return;
			}
			_fixedUpdateTimer += Time.fixedDeltaTime;
			for (int i = 0; i < rbs.Count; i++)
			{
				Rigidbody body = rbs[i];
				if (body == null)
				{
					continue;
				}
				body.AddForce(Physics.gravity * gravityScale, ForceMode.Force);
				_lastYVelocities[i] = body.linearVelocity.y;
				body.linearVelocity += _pendingVelocityChanges[i];
				_pendingVelocityChanges[i] = Vector3.zero;
			}
		}

		private void Update()
		{
			_updateTimer += Time.deltaTime;
			_lifeNormalized = GetNormalizedTime();
			if (_lifeNormalized >= 1f)
			{
				NotifyAttachedPlayerDestroyed();
				Destroy(gameObject);
				return;
			}
			if (_materials != null && colorOverLifetime != null)
			{
				Color tint = colorOverLifetime.Evaluate(_lifeNormalized);
				for (int i = 0; i < _materials.Count; i++)
				{
					if (_materials[i] != null && _materials[i].HasProperty(_colorProperty))
					{
						_materials[i].SetColor(_colorProperty, _startingColors[i] * tint);
					}
				}
			}
			if (_transforms != null && _startingSizes != null)
			{
				Vector3 scale = new Vector3(
					sizeOverLifetimeX != null ? sizeOverLifetimeX.Evaluate(_lifeNormalized) : 1f,
					sizeOverLifetimeY != null ? sizeOverLifetimeY.Evaluate(_lifeNormalized) : 1f,
					sizeOverLifetimeZ != null ? sizeOverLifetimeZ.Evaluate(_lifeNormalized) : 1f);
				for (int i = 0; i < _transforms.Count; i++)
				{
					if (_transforms[i] != null)
					{
						_transforms[i].localScale = Vector3.Scale(_startingSizes[i], scale);
					}
				}
			}
		}

		public void OnCollision(Collision collision, int objectIndex)
		{
			if (rbs == null || objectIndex < 0 || objectIndex >= rbs.Count)
			{
				return;
			}
			Rigidbody body = rbs[objectIndex];
			if (body == null)
			{
				return;
			}
			Collider collider = collision.collider;
			bool isGround = collider != null && collider.CompareTag("Ground");
			if (isGround)
			{
				body.gameObject.layer = _groundHitParticleObjectLayer;
			}
			bool isTabletop = collider != null && collider.CompareTag("Tabletop");
			if (!isGround && !isTabletop)
			{
				return;
			}
			Vector3 velocity = body.linearVelocity;
			float targetX = velocity.x;
			float targetZ = velocity.z;
			if (_collidedOnceList != null && !_collidedOnceList[objectIndex])
			{
				float dampFactor = 1f - dampen;
				targetX *= dampFactor;
				targetZ *= dampFactor;
				_collidedOnceList[objectIndex] = true;
			}
			float targetY = -_lastYVelocities[objectIndex] * bounce;
			Vector3 pendingVelocityChange = _pendingVelocityChanges[objectIndex];
			pendingVelocityChange.x += targetX - velocity.x;
			pendingVelocityChange.y += targetY - velocity.y;
			pendingVelocityChange.z += targetZ - velocity.z;
			_pendingVelocityChanges[objectIndex] = pendingVelocityChange;
		}

		private float GetPassedTime()
		{
			return Mathf.Max(_fixedUpdateTimer, _updateTimer);
		}

		private float GetNormalizedTime()
		{
			return GetPassedTime() / lifetime;
		}

		private void OnDestroy()
		{
			NotifyAttachedPlayerDestroyed();
		}

		public void GatherAndSaveRefs()
		{
			List<MeshRenderer> rendererList = new List<MeshRenderer>();
			List<Rigidbody> rbList = new List<Rigidbody>();
			GatherRecursive(transform, rendererList, rbList);
			meshRenderers = rendererList;
			rbs = rbList;
		}

		private void GatherRecursive(Transform parent, List<MeshRenderer> rendList, List<Rigidbody> rbList)
		{
			if (parent == null)
			{
				return;
			}
			if (parent.TryGetComponent(out MeshRenderer meshRenderer))
			{
				rendList.Add(meshRenderer);
			}
			if (parent.TryGetComponent(out Rigidbody rb))
			{
				rbList.Add(rb);
			}
			for (int i = 0; i < parent.childCount; i++)
			{
				GatherRecursive(parent.GetChild(i), rendList, rbList);
			}
		}

		private void NotifyAttachedPlayerDestroyed()
		{
			if (_destroyNotified)
			{
				return;
			}
			_destroyNotified = true;
			_attachedPlayer?.OnParticleObjectDestroyed(this);
		}
	}
}
