using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Core;
using Gameplay.Objects;
using Gameplay.UI;
using Haptics;
using Service;
using UnityEngine;

namespace Gameplay
{
	public class Cannon : MonoBehaviour
	{
		private const float BallSpeed = 180f;
		private const float BallMass = 1.5f;
		private const float PreshootDelay = 0.033333335f;

		[SerializeField]
		private UnityEngine.Camera mainCamera;

		[SerializeField]
		private GameObject ballPrefab;

		[SerializeField]
		private Transform rootTransform;

		[SerializeField]
		private Transform spawnTransform;

		[SerializeField]
		private Animator animator;

		[SerializeField]
		private ParticleSystem shootParticle;

		private BoundingPlane _boundingPlane;
		private int _targetLayerMask;
		private Queue<Ball> _ballPool;
		private readonly HashSet<Ball> _activeBalls = new HashSet<Ball>();

		public void SetPlaneBounds(List<BaseObject> objects, List<Table> tables)
		{
			_boundingPlane = new BoundingPlane(objects, tables);
			_targetLayerMask = LayerMask.GetMask("BreakableCollider", "ObjectCollider");
		}

		public void InitializeBallPool(int ballPoolSize)
		{
			_ballPool = new Queue<Ball>();
			_activeBalls.Clear();
			for (int i = 0; i < ballPoolSize; i++)
			{
				_ballPool.Enqueue(CreateBall());
			}
		}

		private Ball GetBallFromPool()
		{
			if (_ballPool.Count > 0)
			{
				return _ballPool.Dequeue();
			}
			return CreateBall();
		}

		private Ball CreateBall()
		{
			GameObject instance = UnityEngine.Object.Instantiate(ballPrefab);
			Ball ball = instance.GetComponent<Ball>();
			ball.Initialize(this);
			ball.Recycle();
			return ball;
		}

		public void PrewarmBalls()
		{
			StartCoroutine(PrewarmBallsRoutine());
		}

		private IEnumerator PrewarmBallsRoutine()
		{
			Ball ball = _ballPool.Dequeue();
			ball.gameObject.SetActive(true);
			yield return null;
			yield return null;
			ball.gameObject.SetActive(false);
			_ballPool.Enqueue(ball);
		}

		public void LookAtTarget()
		{
			if (TryGetTarget(out Vector3 target) && TryCalculateBallisticVelocity(spawnTransform.position, target, BallSpeed, out Vector3 direction))
			{
				direction.x *= 5f;
				direction.y += 0.7f;
				rootTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
			}
		}

		public bool TryShoot(out Ball ball)
		{
			ball = null;
			if (!TryGetTarget(out Vector3 target))
			{
				float viewportY = GetPointerViewportPosition().y;
				ServiceLocator.Get<SlidingTextController>()?.Play(viewportY, SlidingTextContentType.TapOnTable);
				return false;
			}

			if (!TryCalculateBallisticVelocity(spawnTransform.position, target, BallSpeed, out Vector3 direction))
			{
				return false;
			}

			GameController gameController = ServiceLocator.Get<GameController>();
			if (gameController == null)
			{
				return false;
			}
			gameController.IncreaseEndBlocker();
			animator.Play("CannonRigPreshoot");
			DelayedWorker.CallAfter(PreshootDelay, delegate
			{
				HapticManager.PlayEmphasis(1f, 1f);
				shootParticle.Play();
				ServiceLocator.Get<AudioHelper>().PlaySfx(Audio.AudioType.CannonShoot);
				Ball shotBall = GetBallFromPool();
				shotBall.gameObject.SetActive(true);
				shotBall.transform.position = spawnTransform.position;
				IgnoreCollisionWithActiveBalls(shotBall);
				_activeBalls.Add(shotBall);
				shotBall.SetMass(BallMass);
				shotBall.Shoot(direction, BallSpeed);
				ServiceLocator.Get<GameController>().AddToLiveBalls(shotBall);
				ServiceLocator.Get<GameController>().DecreaseEndBlocker();
			});
			return true;
		}

		public void OnBallDestroyed(Ball ball)
		{
			if (ball == null || !_activeBalls.Remove(ball))
			{
				return;
			}
			ball.Recycle();
			_ballPool.Enqueue(ball);
			ServiceLocator.Get<GameController>().RemoveFromLiveBalls(ball);
		}

		private void IgnoreCollisionWithActiveBalls(Ball shotBall)
		{
			if (shotBall == null)
			{
				return;
			}

			Collider[] shotColliders = shotBall.GetComponentsInChildren<Collider>(true);
			foreach (Ball otherBall in _activeBalls)
			{
				if (otherBall == null || !otherBall.gameObject.activeInHierarchy)
				{
					continue;
				}

				Collider[] otherColliders = otherBall.GetComponentsInChildren<Collider>(true);
				for (int i = 0; i < shotColliders.Length; i++)
				{
					if (shotColliders[i] == null)
					{
						continue;
					}
					for (int j = 0; j < otherColliders.Length; j++)
					{
						if (otherColliders[j] != null)
						{
							Physics.IgnoreCollision(shotColliders[i], otherColliders[j], true);
						}
					}
				}
			}
		}

		private bool TryGetTarget(out Vector3 target)
		{
			if (mainCamera == null)
			{
				target = Vector3.zero;
				return false;
			}

			Ray ray = mainCamera.ViewportPointToRay(GetPointerViewportPosition());
			return TryRayCastToObject(ray, out target) || TryRayCastToPlane(ray, out target);
		}

		private Vector3 GetPointerViewportPosition()
		{
			if (Screen.width <= 0 || Screen.height <= 0)
			{
				return Vector3.zero;
			}

			Vector3 mousePosition = Input.mousePosition;
			return new Vector3(
				mousePosition.x / Screen.width,
				mousePosition.y / Screen.height,
				0f);
		}

		private bool TryRayCastToObject(Ray ray, out Vector3 hitPoint)
		{
			if (Physics.Raycast(ray, out RaycastHit hit, 100f, _targetLayerMask))
			{
				hitPoint = hit.point;
				return true;
			}

			hitPoint = Vector3.zero;
			return false;
		}

		private bool TryRayCastToPlane(Ray ray, out Vector3 hitPoint)
		{
			if (_boundingPlane == null)
			{
				hitPoint = Vector3.zero;
				return false;
			}
			return _boundingPlane.TryCast(ray, out hitPoint);
		}

		private bool TryCalculateBallisticVelocity(Vector3 a, Vector3 b, float initialSpeed, out Vector3 direction, bool highArc = false)
		{
			Vector3 displacement = b - a;
			float horizontalDistance = Mathf.Sqrt(displacement.x * displacement.x + displacement.z * displacement.z);
			float gravity = Mathf.Abs(Physics.gravity.y);
			float speedSquared = initialSpeed * initialSpeed;
			float discriminant = speedSquared * speedSquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2f * displacement.y * speedSquared);

			if (discriminant < 0f)
			{
				if (discriminant <= -0.00001f)
				{
					direction = Vector3.zero;
					return false;
				}
				discriminant = 0f;
			}

			float root = Mathf.Sqrt(discriminant);
			float angle = Mathf.Atan2(speedSquared + (highArc ? root : -root), horizontalDistance * gravity) * Mathf.Rad2Deg;
			Vector3 planarDirection = new Vector3(displacement.x, 0f, displacement.z).normalized;
			Vector3 rotationAxis = Vector3.Cross(Physics.gravity, planarDirection);
			direction = Quaternion.AngleAxis(angle, rotationAxis) * planarDirection;
			return true;
		}
	}
}
