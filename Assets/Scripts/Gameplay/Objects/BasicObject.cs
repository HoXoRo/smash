using Audio;
using Gameplay.Collisions;
using Gameplay.Particles;
using Haptics;
using Service;
using UnityEngine;

namespace Gameplay.Objects
{
	public class BasicObject : BaseObject
	{
		private const float IceDynamicFriction = 0.05f;

		private const float IceStaticFriction = 0.05f;

		protected override void Start()
		{
			base.Start();
			if ((objectType == ObjectType.IceSquare || objectType == ObjectType.IceCylinder) && Collider != null)
			{
				Collider.material = new PhysicsMaterial
				{
					dynamicFriction = IceDynamicFriction,
					staticFriction = IceStaticFriction,
					bounciness = 0f
				};
			}
		}

		protected override int GetLayer()
		{
			return LayerMask.NameToLayer("ObjectCollider");
		}

		protected override void OnGroundHit(Collision collision)
		{
			ParticleType? particleType = GetParticleTypeToPlay();
			Vector3 particlePosition = rb != null ? rb.position : transform.position;
			Quaternion particleRotation = GetLastRotation();
			if (particleType.HasValue)
			{
				ServiceLocator.Get<ParticleController>()?.PlayOnPosition(particleType.Value, particlePosition, particleRotation, GetLastVelocity());
			}
			Vector3 impactPoint = collision != null && collision.contactCount > 0 ? collision.GetContact(0).point : particlePosition;
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.ObjectGroundHit, impactPoint, Quaternion.identity, Vector3.zero);
			PlayBreakAudio();
			DestroyObject();
		}

		public override Audio.AudioType GetHitAudioType()
		{
			return objectType switch
			{
				ObjectType.CanCylinder or ObjectType.CanCone or ObjectType.CanSquare => Audio.AudioType.CanHit,
				ObjectType.StoneSquare or ObjectType.StoneCylinder => Audio.AudioType.StoneHit,
				ObjectType.BoxCylinder => Audio.AudioType.BoxCylinderHit,
				ObjectType.BoxSquare => Audio.AudioType.BoxHit,
				ObjectType.IceSquare or ObjectType.IceCylinder => Audio.AudioType.IceHit,
				_ => Audio.AudioType.None
			};
		}

		private void PlayBreakAudio()
		{
            Audio.AudioType audioType = objectType switch
			{
				ObjectType.CanCylinder or ObjectType.CanCone or ObjectType.CanSquare => Audio.AudioType.CanBreak,
				ObjectType.StoneSquare or ObjectType.StoneCylinder => Audio.AudioType.StoneBreak,
				ObjectType.BoxCylinder => Audio.AudioType.BoxCylinderBreak,
				ObjectType.BoxSquare => Audio.AudioType.BoxBreak,
				ObjectType.IceSquare or ObjectType.IceCylinder => Audio.AudioType.IceBreak,
				_ => Audio.AudioType.None
			};
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(audioType);
		}

		public override BallObjectCollisionResult OnBallHit(BallObjectCollision collision)
		{
			return BallObjectCollisionResult.None;
		}

		public override void DestroyByTnt()
		{
			ParticleType? particleType = GetParticleTypeToPlayOnTntExplode();
			if (particleType.HasValue)
			{
				Vector3 particlePosition = rb != null ? rb.position : transform.position;
				ServiceLocator.Get<ParticleController>()?.PlayOnPosition(particleType.Value, particlePosition, GetLastRotation(), GetLastVelocity());
			}
			PlayBreakAudio();
			DestroyObject();
		}

		public override void DestroyByRocket()
		{
			ParticleType? particleType = GetParticleTypeToPlayOnTntExplode();
			if (particleType.HasValue)
			{
				Vector3 particlePosition = (rb != null ? rb.position : transform.position) + Vector3.back;
				ServiceLocator.Get<ParticleController>()?.PlayOnPosition(particleType.Value, particlePosition, GetLastRotation(), Vector3.zero);
			}
			PlayBreakAudio();
			DestroyObject();
			HapticManager.PlayObjectBreak(objectType, size.GetVolume());
		}

		private ParticleType? GetParticleTypeToPlayOnTntExplode()
		{
			int volume = Mathf.RoundToInt(size.GetVolume());
			if (ObjectParticleMaps.TntExplodeParticles.TryGetValue((objectType, volume), out ParticleType particleType))
			{
				return particleType;
			}
			return null;
		}

		private ParticleType? GetParticleTypeToPlay()
		{
			int volume = Mathf.RoundToInt(size.GetVolume());
			if (ObjectParticleMaps.PlayParticles.TryGetValue((objectType, volume), out ParticleType particleType))
			{
				return particleType;
			}
			return null;
		}
	}
}
