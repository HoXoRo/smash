using System.Collections.Generic;
using Audio;
using Gameplay.Collisions;
using Gameplay.Particles;
using Haptics;
using Service;
using UnityEngine;

namespace Gameplay.Objects
{
	public class BreakableObject : BaseObject
	{
		public enum BreakSource
		{
			Ground = 0,
			Ball = 1,
			Tnt = 2
		}

		private const float BreakSpeedThreshold = 18f;

		private static readonly Dictionary<(ObjectType type, int volume, BreakSource source), ParticleType> JamJarBreakParticles = new Dictionary<(ObjectType type, int volume, BreakSource source), ParticleType>
		{
			[(ObjectType.JamJarBlue, 1, BreakSource.Ball)] = ParticleType.JamJarBlue1xHit,
			[(ObjectType.JamJarBlue, 2, BreakSource.Ball)] = ParticleType.JamJarBlue2xHit,
			[(ObjectType.JamJarBlue, 3, BreakSource.Ball)] = ParticleType.JamJarBlue3xHit,
			[(ObjectType.JamJarBlue, 1, BreakSource.Ground)] = ParticleType.JamJarBlue1xGround,
			[(ObjectType.JamJarBlue, 2, BreakSource.Ground)] = ParticleType.JamJarBlue2xGround,
			[(ObjectType.JamJarBlue, 3, BreakSource.Ground)] = ParticleType.JamJarBlue3xGround,
			[(ObjectType.JamJarBlue, 1, BreakSource.Tnt)] = ParticleType.JamJarBlue1xTntExplode,
			[(ObjectType.JamJarBlue, 2, BreakSource.Tnt)] = ParticleType.JamJarBlue2xTntExplode,
			[(ObjectType.JamJarBlue, 3, BreakSource.Tnt)] = ParticleType.JamJarBlue3xTntExplode,
			[(ObjectType.JamJarPink, 1, BreakSource.Ball)] = ParticleType.JamJarPink1xHit,
			[(ObjectType.JamJarPink, 2, BreakSource.Ball)] = ParticleType.JamJarPink2xHit,
			[(ObjectType.JamJarPink, 3, BreakSource.Ball)] = ParticleType.JamJarPink3xHit,
			[(ObjectType.JamJarPink, 1, BreakSource.Ground)] = ParticleType.JamJarPink1xGround,
			[(ObjectType.JamJarPink, 2, BreakSource.Ground)] = ParticleType.JamJarPink2xGround,
			[(ObjectType.JamJarPink, 3, BreakSource.Ground)] = ParticleType.JamJarPink3xGround,
			[(ObjectType.JamJarPink, 1, BreakSource.Tnt)] = ParticleType.JamJarPink1xTntExplode,
			[(ObjectType.JamJarPink, 2, BreakSource.Tnt)] = ParticleType.JamJarPink2xTntExplode,
			[(ObjectType.JamJarPink, 3, BreakSource.Tnt)] = ParticleType.JamJarPink3xTntExplode,
			[(ObjectType.JamJarYellow, 1, BreakSource.Ball)] = ParticleType.JamJarYellow1xHit,
			[(ObjectType.JamJarYellow, 2, BreakSource.Ball)] = ParticleType.JamJarYellow2xHit,
			[(ObjectType.JamJarYellow, 3, BreakSource.Ball)] = ParticleType.JamJarYellow3xHit,
			[(ObjectType.JamJarYellow, 1, BreakSource.Ground)] = ParticleType.JamJarYellow1xGround,
			[(ObjectType.JamJarYellow, 2, BreakSource.Ground)] = ParticleType.JamJarYellow2xGround,
			[(ObjectType.JamJarYellow, 3, BreakSource.Ground)] = ParticleType.JamJarYellow3xGround,
			[(ObjectType.JamJarYellow, 1, BreakSource.Tnt)] = ParticleType.JamJarYellow1xTntExplode,
			[(ObjectType.JamJarYellow, 2, BreakSource.Tnt)] = ParticleType.JamJarYellow2xTntExplode,
			[(ObjectType.JamJarYellow, 3, BreakSource.Tnt)] = ParticleType.JamJarYellow3xTntExplode,
			[(ObjectType.JamJarRed, 1, BreakSource.Ball)] = ParticleType.JamJarRed1xHit,
			[(ObjectType.JamJarRed, 2, BreakSource.Ball)] = ParticleType.JamJarRed2xHit,
			[(ObjectType.JamJarRed, 3, BreakSource.Ball)] = ParticleType.JamJarRed3xHit,
			[(ObjectType.JamJarRed, 1, BreakSource.Ground)] = ParticleType.JamJarRed1xGround,
			[(ObjectType.JamJarRed, 2, BreakSource.Ground)] = ParticleType.JamJarRed2xGround,
			[(ObjectType.JamJarRed, 3, BreakSource.Ground)] = ParticleType.JamJarRed3xGround,
			[(ObjectType.JamJarRed, 1, BreakSource.Tnt)] = ParticleType.JamJarRed1xTntExplode,
			[(ObjectType.JamJarRed, 2, BreakSource.Tnt)] = ParticleType.JamJarRed2xTntExplode,
			[(ObjectType.JamJarRed, 3, BreakSource.Tnt)] = ParticleType.JamJarRed3xTntExplode,
			[(ObjectType.JamJarOrange, 1, BreakSource.Ball)] = ParticleType.JamJarOrange1xHit,
			[(ObjectType.JamJarOrange, 2, BreakSource.Ball)] = ParticleType.JamJarOrange2xHit,
			[(ObjectType.JamJarOrange, 3, BreakSource.Ball)] = ParticleType.JamJarOrange3xHit,
			[(ObjectType.JamJarOrange, 1, BreakSource.Ground)] = ParticleType.JamJarOrange1xGround,
			[(ObjectType.JamJarOrange, 2, BreakSource.Ground)] = ParticleType.JamJarOrange2xGround,
			[(ObjectType.JamJarOrange, 3, BreakSource.Ground)] = ParticleType.JamJarOrange3xGround,
			[(ObjectType.JamJarOrange, 1, BreakSource.Tnt)] = ParticleType.JamJarOrange1xTntExplode,
			[(ObjectType.JamJarOrange, 2, BreakSource.Tnt)] = ParticleType.JamJarOrange2xTntExplode,
			[(ObjectType.JamJarOrange, 3, BreakSource.Tnt)] = ParticleType.JamJarOrange3xTntExplode,
			[(ObjectType.JamJarPurple, 1, BreakSource.Ball)] = ParticleType.JamJarPurple1xHit,
			[(ObjectType.JamJarPurple, 2, BreakSource.Ball)] = ParticleType.JamJarPurple2xHit,
			[(ObjectType.JamJarPurple, 3, BreakSource.Ball)] = ParticleType.JamJarPurple3xHit,
			[(ObjectType.JamJarPurple, 1, BreakSource.Ground)] = ParticleType.JamJarPurple1xGround,
			[(ObjectType.JamJarPurple, 2, BreakSource.Ground)] = ParticleType.JamJarPurple2xGround,
			[(ObjectType.JamJarPurple, 3, BreakSource.Ground)] = ParticleType.JamJarPurple3xGround,
			[(ObjectType.JamJarPurple, 1, BreakSource.Tnt)] = ParticleType.JamJarPurple1xTntExplode,
			[(ObjectType.JamJarPurple, 2, BreakSource.Tnt)] = ParticleType.JamJarPurple2xTntExplode,
			[(ObjectType.JamJarPurple, 3, BreakSource.Tnt)] = ParticleType.JamJarPurple3xTntExplode
		};

		protected override int GetLayer()
		{
			return LayerMask.NameToLayer("BreakableCollider");
		}

		public override Audio.AudioType GetHitAudioType()
		{
			return Audio.AudioType.None;
		}

		protected override void Start()
		{
			base.Start();
			CreateTrigger();
		}

		private void CreateTrigger()
		{
			if (rb == null)
			{
				return;
			}
			GameObject triggerObject = new GameObject("BreakableTrigger");
			triggerObject.transform.SetParent(rb.transform, false);
			BreakableTrigger trigger = triggerObject.AddComponent<BreakableTrigger>();
			trigger.Init(this, rb.GetComponent<Collider>(), rb);
		}

		protected override void OnGroundHit(Collision collision)
		{
			Break(BreakSource.Ground);
			Vector3 impactPoint = collision != null && collision.contactCount > 0 ? collision.GetContact(0).point : (rb != null ? rb.position : transform.position);
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.ObjectGroundHit, impactPoint, Quaternion.identity, Vector3.zero);
			PlayBreakAudio();
		}

		private void PlayBreakAudio()
		{
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.JarBreak);
		}

		public override BallObjectCollisionResult OnBallHit(BallObjectCollision collision)
		{
			if (collision == null || collision.RelativeVelocity.sqrMagnitude < BreakSpeedThreshold * BreakSpeedThreshold)
			{
				return BallObjectCollisionResult.None;
			}
			Break(BreakSource.Ball);
			PlayBreakAudio();
			return BallObjectCollisionResult.Break;
		}

		public override void DestroyByTnt()
		{
			Break(BreakSource.Tnt);
			PlayBreakAudio();
		}

		public override void DestroyByRocket()
		{
			Break(BreakSource.Tnt);
			PlayBreakAudio();
			HapticManager.PlayObjectBreak(objectType, size.GetVolume());
		}

		private void Break(BreakSource breakSource)
		{
			if (BeingDestroyed)
			{
				return;
			}
			int volume = Mathf.RoundToInt(size.GetVolume());
			if (JamJarBreakParticles.TryGetValue((objectType, volume, breakSource), out ParticleType particleType))
			{
				Vector3 particlePosition = rb != null ? rb.position : transform.position;
				if (breakSource == BreakSource.Tnt)
				{
					particlePosition += Vector3.back;
				}
				Vector3 velocity = breakSource == BreakSource.Tnt ? Vector3.zero : GetLastVelocity();
				ServiceLocator.Get<ParticleController>()?.PlayOnPosition(particleType, particlePosition, GetLastRotation(), velocity);
			}
			DestroyObject();
		}
	}
}
