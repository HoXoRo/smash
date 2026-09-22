using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Collisions;
using Level;
using Service;
using UnityEngine;
using Util;

namespace Gameplay.Objects
{
	public class Table : MonoBehaviour
	{
		private const float WheelPerimeter = 2.6703537f;

		public MeshRenderer[] meshRenderers;

		[SerializeField]
		private Transform[] partTransforms;

		[SerializeField]
		private BoxCollider boxCollider;

		[SerializeField]
		private Rigidbody rb;

		[SerializeField]
		private Transform tableBottomNormal;

		[SerializeField]
		private Transform tableBottomWheels;

		[SerializeField]
		private Transform tableTopBottomMarker;

		[SerializeField]
		private Transform tableBottomNormalMarker;

		[SerializeField]
		private Transform poleNormalTransform;

		[SerializeField]
		private List<GameObject> ropeAlternatives;

		[SerializeField]
		private List<Transform> wheelTransforms;

		[SerializeField]
		private float width;

		[SerializeField]
		private float depth;

		[SerializeField]
		private float zOffset;

		[SerializeField]
		private bool doesRotate;

		[SerializeField]
		private float rotationSpeed;

		[SerializeField]
		private bool movesHorizontal;

		[SerializeField]
		private float moveHorizontalMin;

		[SerializeField]
		private float moveHorizontalMax;

		[SerializeField]
		private HorizontalDirection initialHorizontalDirection;

		[SerializeField]
		private float horizontalMoveSpeed;

		[SerializeField]
		public bool movesVertical;

		[SerializeField]
		public float moveVerticalMin;

		[SerializeField]
		public float moveVerticalMax;

		[SerializeField]
		private VerticalDirection initialVerticalDirection;

		[SerializeField]
		private float verticalMoveSpeed;

		private float _currentRotationSpeed;

		private float _currentHorizontalVelocity;

		private float _currentVerticalVelocity;

		[NonSerialized]
		public int Id;

		[NonSerialized]
		public ContactNode ContactNode;

		private readonly HashSet<ContactNode> _cluster = new HashSet<ContactNode>();

		private readonly Queue<ContactNode> _queue = new Queue<ContactNode>();

		private float timer;

		private bool DoesMove()
		{
			return movesHorizontal || movesVertical;
		}

		public void SetDimensions(float w, float d, float zOff)
		{
			width = w;
			depth = d;
			zOffset = zOff;
			Resize();
			UpdateBottomVisuals();
		}

		private void Start()
		{
			if (rb == null)
			{
				rb = GetComponent<Rigidbody>();
			}
			if (rb == null)
			{
				return;
			}
			ContactNode = rb.gameObject.GetComponent<ContactNode>() ?? rb.gameObject.AddComponent<ContactNode>();
			ContactNode.SetType(ContactNodeType.Table);
			Resize();
			UpdateBottomVisuals();
			if (DoesMove())
			{
				StartCoroutine(MovementRoutine());
			}
		}

		private void OnValidate()
		{
			Resize();
			UpdateBottomVisuals();
		}

		private void ChangeHorizontalVelocity(float newVelocity)
		{
			float delta = newVelocity;
			if (rb != null)
			{
				delta -= rb.linearVelocity.x;
				rb.linearVelocity = Vector3.right * newVelocity;
			}
			_currentHorizontalVelocity = newVelocity;
			if (!Mathf.Approximately(delta, 0f))
			{
				ApplyImpulseToTouchingOwners(Vector3.right, delta);
			}
		}

		private void ChangeVerticalVelocity(float newVelocity)
		{
			float delta = newVelocity;
			if (rb != null)
			{
				delta -= rb.linearVelocity.y;
				rb.linearVelocity = Vector3.up * newVelocity;
			}
			_currentVerticalVelocity = newVelocity;
			if (!Mathf.Approximately(delta, 0f))
			{
				ApplyImpulseToTouchingOwners(Vector3.up, delta);
			}
		}

		private void ApplyImpulseToTouchingOwners(Vector3 axis, float impulse)
		{
			GameController gameController = ServiceLocator.Get<GameController>();
			if (gameController == null)
			{
				return;
			}
			foreach (IContactNodeOwner owner in gameController.GetOwnersTouchingTable(this))
			{
				if (owner is BaseObject baseObject && baseObject.rb != null)
				{
					baseObject.rb.AddForce(axis * (baseObject.rb.mass * impulse), ForceMode.Impulse);
				}
				else if (owner is Ball ball && ball.rb != null)
				{
					ball.rb.AddForce(axis * (ball.rb.mass * impulse), ForceMode.Impulse);
				}
			}
		}

		private IEnumerator MovementRoutine()
		{
			WaitForFixedUpdate waitNextFixedUpdate = new WaitForFixedUpdate();
			yield return new WaitForSeconds(0.5f);
			Vector3 basePos = rb != null ? rb.position : transform.position;
			Vector3 posOffset = Vector3.zero;
			float horizontalSpeed = Mathf.Abs(horizontalMoveSpeed);
			float verticalSpeed = Mathf.Abs(verticalMoveSpeed);
			ChangeHorizontalVelocity(movesHorizontal ? ((initialHorizontalDirection == HorizontalDirection.Left) ? (0f - horizontalSpeed) : horizontalSpeed) : 0f);
			ChangeVerticalVelocity(movesVertical ? ((initialVerticalDirection == VerticalDirection.Up) ? verticalSpeed : (0f - verticalSpeed)) : 0f);
			while (this != null)
			{
				if (movesHorizontal)
				{
					posOffset.x += _currentHorizontalVelocity * Time.fixedDeltaTime;
					if (posOffset.x <= moveHorizontalMin)
					{
						posOffset.x = moveHorizontalMin;
						ChangeHorizontalVelocity(horizontalSpeed);
					}
					else if (posOffset.x >= moveHorizontalMax)
					{
						posOffset.x = moveHorizontalMax;
						ChangeHorizontalVelocity(0f - horizontalSpeed);
					}
				}
				if (movesVertical)
				{
					posOffset.y += _currentVerticalVelocity * Time.fixedDeltaTime;
					if (posOffset.y <= moveVerticalMin)
					{
						posOffset.y = moveVerticalMin;
						ChangeVerticalVelocity(verticalSpeed);
					}
					else if (posOffset.y >= moveVerticalMax)
					{
						posOffset.y = moveVerticalMax;
						ChangeVerticalVelocity(0f - verticalSpeed);
					}
				}
				Vector3 nextPosition = basePos + posOffset;
				if (rb != null)
				{
					rb.position = nextPosition;
				}
				else
				{
					transform.position = nextPosition;
				}
				if (!Mathf.Approximately(_currentHorizontalVelocity, 0f) && wheelTransforms != null)
				{
					float wheelRotation = _currentHorizontalVelocity * (360f / WheelPerimeter) * Time.fixedDeltaTime;
					for (int i = 0; i < wheelTransforms.Count; i++)
					{
						Transform wheelTransform = wheelTransforms[i];
						if (wheelTransform != null)
						{
							wheelTransform.Rotate(Vector3.forward, wheelRotation, Space.Self);
						}
					}
				}
				yield return waitNextFixedUpdate;
			}
		}

		public HashSet<ContactNode> GetContactCluster()
		{
			return _cluster;
		}

		public void UpdateContactGraph()
		{
			ContactGraph.CollectConnected(ContactNode, _cluster, _queue, ContactNodeType.Table | ContactNodeType.Object | ContactNodeType.Ball);
		}

		private void Update()
		{
			if (!movesVertical || poleNormalTransform == null || tableTopBottomMarker == null || tableBottomNormalMarker == null)
			{
				return;
			}
			Vector3 top = tableTopBottomMarker.position;
			Vector3 bottom = tableBottomNormalMarker.position;
			poleNormalTransform.position = (top + bottom) * 0.5f;
			poleNormalTransform.localScale = new Vector3(1f, top.y - bottom.y, 1f);
		}

		private void FixedUpdate()
		{
			_currentRotationSpeed = doesRotate ? rotationSpeed : 0f;
			if (!doesRotate)
			{
				return;
			}
			UpdateContactGraph();
			transform.Rotate(0f, _currentRotationSpeed * Time.fixedDeltaTime, 0f, Space.Self);
		}

		public Vector3 GetCurrentMovementVector()
		{
			return new Vector3(_currentHorizontalVelocity, _currentVerticalVelocity, 0f);
		}

		public Vector3 GetTangentialVelocity(Vector3 objectPosition)
		{
			return Vector3.Cross(GetCurrentRotationVector(), objectPosition - transform.position);
		}

		public Vector3 GetCurrentRotationVector()
		{
			if (!doesRotate || Mathf.Approximately(_currentRotationSpeed, 0f))
			{
				return Vector3.zero;
			}
			return Vector3.up * (_currentRotationSpeed * Mathf.Deg2Rad);
		}

		private void OnDisable()
		{
			CleanupMovementState();
		}

		private void OnDestroy()
		{
			CleanupMovementState();
		}

		private void CleanupMovementState()
		{
			StopAllCoroutines();
			_currentRotationSpeed = 0f;
			_currentHorizontalVelocity = 0f;
			_currentVerticalVelocity = 0f;
			if (rb != null)
			{
				Vector3 velocity = rb.linearVelocity;
				if (movesHorizontal)
				{
					velocity.x = 0f;
				}
				if (movesVertical)
				{
					velocity.y = 0f;
				}
				rb.linearVelocity = velocity;
			}
		}

		public void Resize()
		{
			if (partTransforms != null)
			{
				for (int i = 0; i < partTransforms.Length; i++)
				{
					Transform partTransform = partTransforms[i];
					if (partTransform == null)
					{
						continue;
					}
					Vector3 localPosition = partTransform.localPosition;
					Vector3 localScale = partTransform.localScale;
					switch (partTransform.name)
					{
					case "Top":
						localPosition = new Vector3(0f, 0f, zOffset);
						localScale = new Vector3(width, localScale.y, depth);
						break;
					case "Front":
						localPosition = new Vector3(0f, 0f, zOffset - depth * 0.5f);
						localScale = new Vector3(width, localScale.y, 1f);
						break;
					case "Back":
						localPosition = new Vector3(0f, 0f, zOffset + depth * 0.5f);
						localScale = new Vector3(width, localScale.y, 1f);
						break;
					case "Left":
						localPosition = new Vector3(0f - width * 0.5f, 0f, zOffset);
						localScale = new Vector3(1f, localScale.y, depth);
						break;
					case "Right":
						localPosition = new Vector3(width * 0.5f, 0f, zOffset);
						localScale = new Vector3(1f, localScale.y, depth);
						break;
					case "FrontLeft":
						localPosition = new Vector3(0f - width * 0.5f, 0f, zOffset - depth * 0.5f);
						localScale = new Vector3(1f, localScale.y, 1f);
						break;
					case "FrontRight":
						localPosition = new Vector3(width * 0.5f, 0f, zOffset - depth * 0.5f);
						localScale = new Vector3(1f, localScale.y, 1f);
						break;
					case "BackLeft":
						localPosition = new Vector3(0f - width * 0.5f, 0f, zOffset + depth * 0.5f);
						localScale = new Vector3(1f, localScale.y, 1f);
						break;
					case "BackRight":
						localPosition = new Vector3(width * 0.5f, 0f, zOffset + depth * 0.5f);
						localScale = new Vector3(1f, localScale.y, 1f);
						break;
					}
					partTransform.localPosition = localPosition;
					partTransform.localScale = localScale;
				}
			}
			if (boxCollider != null)
			{
				Vector3 center = boxCollider.center;
				center.z = zOffset;
				boxCollider.center = center;
				Vector3 size = boxCollider.size;
				size.x = width;
				size.z = depth;
				boxCollider.size = size;
			}
			timer = 0f;
		}

		public void AutoFit()
		{
			BaseObject[] baseObjects = GetComponentsInChildren<BaseObject>(true);
			if (baseObjects == null || baseObjects.Length == 0)
			{
				zOffset = 0f;
				width = 1f;
				depth = 1f;
				Resize();
				return;
			}
			ObjectData objectData = Resources.Load<ObjectData>("ObjectData");
			float minX = float.MaxValue;
			float maxX = float.MinValue;
			float minZ = float.MaxValue;
			float maxZ = float.MinValue;
			for (int i = 0; i < baseObjects.Length; i++)
			{
				BaseObject baseObject = baseObjects[i];
				if (baseObject == null)
				{
					continue;
				}
				Vector3 localPosition = transform.InverseTransformPoint(baseObject.transform.position);
				FloatTriplet objectSize = baseObject.size;
				if (objectSize.x <= 0f || objectSize.z <= 0f)
				{
					objectSize = FindObjectSize(objectData, baseObject.objectType, objectSize);
				}
				float halfWidth = objectSize.x * 0.5f;
				float halfDepth = objectSize.z * 0.5f;
				minX = Mathf.Min(minX, localPosition.x - halfWidth);
				maxX = Mathf.Max(maxX, localPosition.x + halfWidth);
				minZ = Mathf.Min(minZ, localPosition.z - halfDepth);
				maxZ = Mathf.Max(maxZ, localPosition.z + halfDepth);
			}
			if (minX == float.MaxValue || maxX == float.MinValue || minZ == float.MaxValue || maxZ == float.MinValue)
			{
				zOffset = 0f;
				width = 1f;
				depth = 1f;
				Resize();
				return;
			}
			float centerX = (maxX + minX) * 0.5f;
			float centerZ = (maxZ + minZ) * 0.5f;
			for (int j = 0; j < baseObjects.Length; j++)
			{
				BaseObject baseObject = baseObjects[j];
				if (baseObject == null)
				{
					continue;
				}
				Vector3 localPosition = baseObject.transform.localPosition;
				baseObject.transform.localPosition = new Vector3(localPosition.x - centerX, localPosition.y, localPosition.z - centerZ);
			}
			width = maxX - minX;
			depth = maxZ - minZ;
			zOffset = 0f;
			Resize();
		}

		private static FloatTriplet FindObjectSize(ObjectData objectData, ObjectType objectType, FloatTriplet fallback)
		{
			if (objectData?.items != null)
			{
				for (int i = 0; i < objectData.items.Count; i++)
				{
					ObjectDataItem item = objectData.items[i];
					if (item != null && item.type == objectType)
					{
						return item.size;
					}
				}
			}
			return fallback.x > 0f && fallback.z > 0f ? fallback : new FloatTriplet(1f, 1f, 1f);
		}

		public void SetId(int id)
		{
			Id = id;
		}

		public LevelTableData GetDataForLevel()
		{
			return new LevelTableData
			{
				id = Id,
				pos = new FloatTriplet(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z),
				rot = new FloatQuartet(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w),
				scl = new FloatTriplet(transform.localScale.x, transform.localScale.y, transform.localScale.z),
				dim = new FloatTriplet(width, depth, zOffset),
				doRot = doesRotate,
				rotSpd = rotationSpeed,
				movH = movesHorizontal,
				movHMin = moveHorizontalMin,
				movHMax = moveHorizontalMax,
				dirH = initialHorizontalDirection,
				movSpdH = horizontalMoveSpeed,
				movV = movesVertical,
				movVMin = moveVerticalMin,
				movVMax = moveVerticalMax,
				dirV = initialVerticalDirection,
				movSpdV = verticalMoveSpeed
			};
		}

		public void LoadDataFromLevel(LevelTableData data)
		{
			if (data == null)
			{
				return;
			}
			Id = data.id;
			transform.localPosition = new Vector3(data.pos.x, data.pos.y, data.pos.z);
			transform.localRotation = new Quaternion(data.rot.x, data.rot.y, data.rot.z, data.rot.w);
			transform.localScale = new Vector3(data.scl.x, data.scl.y, data.scl.z);
			doesRotate = data.doRot;
			rotationSpeed = data.rotSpd;
			movesHorizontal = data.movH;
			moveHorizontalMin = data.movHMin;
			moveHorizontalMax = data.movHMax;
			initialHorizontalDirection = data.dirH;
			horizontalMoveSpeed = data.movSpdH;
			movesVertical = data.movV;
			moveVerticalMin = data.movVMin;
			moveVerticalMax = data.movVMax;
			initialVerticalDirection = data.dirV;
			verticalMoveSpeed = data.movSpdV;
			SetDimensions(data.dim.x, data.dim.y, data.dim.z);
			if (tableBottomNormal != null)
			{
				tableBottomNormal.rotation = Quaternion.identity;
			}
			if (tableBottomWheels != null)
			{
				tableBottomWheels.rotation = Quaternion.identity;
			}
			if (ropeAlternatives != null && ropeAlternatives.Count > 0)
			{
				for (int i = 0; i < ropeAlternatives.Count; i++)
				{
					if (ropeAlternatives[i] != null)
					{
						ropeAlternatives[i].SetActive(false);
					}
				}
				GameObject ropeAlternative = ropeAlternatives[UnityEngine.Random.Range(0, ropeAlternatives.Count)];
				if (ropeAlternative != null)
				{
					ropeAlternative.SetActive(true);
				}
			}
		}

		public void UpdateBottomVisuals()
		{
			if (tableBottomWheels != null)
			{
				tableBottomWheels.gameObject.SetActive(movesHorizontal);
			}
			if (tableBottomNormal != null)
			{
				tableBottomNormal.gameObject.SetActive(!movesHorizontal);
			}
		}

		public float GetMaxObjectX()
		{
			BaseObject[] baseObjects = GetComponentsInChildren<BaseObject>();
			float maxObjectX = 0f;
			for (int i = 0; i < baseObjects.Length; i++)
			{
				BaseObject baseObject = baseObjects[i];
				if (baseObject != null)
				{
					Vector3 localPosition = transform.InverseTransformPoint(baseObject.transform.position);
					maxObjectX = Mathf.Max(maxObjectX, Mathf.Abs(localPosition.x) + Mathf.Max(baseObject.size.x, 0f));
				}
			}
			return maxObjectX;
		}
	}
}
