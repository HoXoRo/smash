using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using DG.Tweening;
using Gameplay.Collisions;
using Gameplay.Particles;
using Haptics;
using Service;
using UnityEngine;

namespace Gameplay.Objects
{
	public class WormholeObject : BaseObject
	{
		private enum HitType
		{
			Edge = 0,
			Face = 1
		}

		private const float WormHoleDestroyWait = 0.05f;

		private const float WormHoleScaleDownDuration = 0.16f;

		private const float EdgeThreshold = 0.7f;

		[SerializeField]
		private BoxCollider boxCollider;

		[SerializeField]
		private List<GameObject> insideParts;

		private Vector3 _angularVelocityBeforeHit;

		private bool _endBlockerRaised;

		protected override int GetLayer()
		{
			return LayerMask.NameToLayer("ObjectCollider");
		}

		public override Audio.AudioType GetHitAudioType()
		{
			return Audio.AudioType.WormholeHit;
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			if (rb != null)
			{
				_angularVelocityBeforeHit = rb.angularVelocity;
			}
		}

		protected override void OnGroundHit(Collision collision)
		{
			if (BeingDestroyed)
			{
				return;
			}
			Vector3 particlePosition = rb != null ? rb.position : transform.position;
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.WormholeHitGround, particlePosition, GetLastRotation(), GetLastVelocity());
			DestroyObject();
			PlayBreakAudio();
		}

		private void PlayBreakAudio()
		{
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.WormholeBreak);
		}

		public override BallObjectCollisionResult OnBallHit(BallObjectCollision collision)
		{
			if (BeingDestroyed)
			{
				return BallObjectCollisionResult.None;
			}
			if (collision != null && CalculateHitType(collision.Point) == HitType.Face)
			{
				OnFaceHit();
				return BallObjectCollisionResult.BallDestroy;
			}
			return BallObjectCollisionResult.None;
		}

		public override void DestroyByTnt()
		{
			if (BeingDestroyed)
			{
				return;
			}
			Vector3 particlePosition = rb != null ? rb.position : transform.position;
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.WormholeHitTntExplode, particlePosition, GetLastRotation(), GetLastVelocity());
			DestroyObject();
			PlayBreakAudio();
		}

		public override void DestroyByRocket()
		{
			if (BeingDestroyed)
			{
				return;
			}
			Vector3 particlePosition = (rb != null ? rb.position : transform.position) + Vector3.back;
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.WormholeHitTntExplode, particlePosition, GetLastRotation(), Vector3.zero);
			DestroyObject();
			PlayBreakAudio();
			HapticManager.PlayObjectBreak(objectType, size.GetVolume());
		}

		private HitType CalculateHitType(Vector3 contactPoint)
		{
			Vector3 localPoint = transform.InverseTransformPoint(contactPoint);
			Vector3 colliderSize = boxCollider != null ? boxCollider.size : Vector3.one;
			Vector3 scale = boxCollider != null ? boxCollider.transform.lossyScale : transform.lossyScale;
			Vector3 halfExtents = new Vector3(
				Mathf.Abs(colliderSize.x * scale.x) * 0.5f,
				Mathf.Abs(colliderSize.y * scale.y) * 0.5f,
				Mathf.Abs(colliderSize.z * scale.z) * 0.5f
			);
			int edgeAxisCount = 0;
			if (halfExtents.x > 0f && Mathf.Abs(localPoint.x) / halfExtents.x > EdgeThreshold)
			{
				edgeAxisCount++;
			}
			if (halfExtents.y > 0f && Mathf.Abs(localPoint.y) / halfExtents.y > EdgeThreshold)
			{
				edgeAxisCount++;
			}
			if (halfExtents.z > 0f && Mathf.Abs(localPoint.z) / halfExtents.z > EdgeThreshold)
			{
				edgeAxisCount++;
			}
			return edgeAxisCount >= 2 ? HitType.Edge : HitType.Face;
		}

		private void OnFaceHit()
		{
			if (BeingDestroyed)
			{
				return;
			}
			Vector3 particlePosition = rb != null ? rb.position : transform.position;
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.WormholeHit, particlePosition, GetLastRotation(), GetLastVelocity());
			BeingDestroyed = true;
			if (rb != null)
			{
				rb.linearVelocity = GetLastVelocity();
				rb.angularVelocity = _angularVelocityBeforeHit;
			}
			StartCoroutine(HitDestroyRoutine());
			_endBlockerRaised = true;
			ServiceLocator.Get<GameController>()?.IncreaseEndBlocker();
			PlayBreakAudio();
			HapticManager.PlayObjectBreak(objectType, size.GetVolume());
		}

		private IEnumerator HitDestroyRoutine()
		{
			if (insideParts != null)
			{
				insideParts.ForEach((GameObject x) =>
				{
					if (x != null)
					{
						x.SetActive(false);
					}
				});
			}
			yield return new WaitForSeconds(WormHoleDestroyWait);
			if (boxCollider != null)
			{
				yield return boxCollider.transform.DOScale(Vector3.zero, WormHoleScaleDownDuration).WaitForCompletion();
			}
			ReleaseEndBlocker();
			DestroyObject();
		}

		private void OnDisable()
		{
			CleanupDestroyRoutineState();
		}

		private void OnDestroy()
		{
			CleanupDestroyRoutineState();
		}

		private void CleanupDestroyRoutineState()
		{
			StopAllCoroutines();
			boxCollider?.transform.DOKill();
			ReleaseEndBlocker();
		}

		private void ReleaseEndBlocker()
		{
			if (!_endBlockerRaised)
			{
				return;
			}
			_endBlockerRaised = false;
			ServiceLocator.Get<GameController>()?.DecreaseEndBlocker();
		}
	}
}
