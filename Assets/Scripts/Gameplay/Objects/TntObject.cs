using System.Collections.Generic;
using Audio;
using Gameplay.Collisions;
using Gameplay.Particles;
using Haptics;
using Service;
using UnityEngine;

namespace Gameplay.Objects
{
	public class TntObject : BaseObject
	{
		protected override int GetLayer()
		{
			return LayerMask.NameToLayer("ObjectCollider");
		}

		protected override void OnGroundHit(Collision collision)
		{
			DestroyAndExplode(ParticleType.TntExplodeGround, GetLastVelocity(), false);
		}

		public override BallObjectCollisionResult OnBallHit(BallObjectCollision collision)
		{
			DestroyAndExplode(ParticleType.TntExplode, GetLastVelocity(), false);
			return BallObjectCollisionResult.None;
		}

		public override Audio.AudioType GetHitAudioType()
		{
			return Audio.AudioType.None;
		}

		public void PlayExplodeAudio()
		{
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.TntExplode);
		}

		public override void DestroyByTnt()
		{
			DestroyAndExplode(ParticleType.TntExplode, GetLastVelocity(), false);
		}

		public override void DestroyByRocket()
		{
			DestroyAndExplode(ParticleType.TntExplode, Vector3.zero, true);
		}

		private void Explode()
		{
			PlayExplodeAudio();
			if (ContactNode == null)
			{
				return;
			}
			List<ContactNode> neighbors = new List<ContactNode>(ContactNode.Neighbors);
			foreach (ContactNode neighbor in neighbors)
			{
				if (neighbor == null || neighbor.MasterObject == this)
				{
					continue;
				}
				if (neighbor.MasterObject is BaseObject baseObject && !baseObject.BeingDestroyed)
				{
					baseObject.DestroyByTnt();
				}
			}
		}

		private void DestroyAndExplode(ParticleType particleType, Vector3 velocity, bool playHaptic)
		{
			if (BeingDestroyed)
			{
				return;
			}
			DestroyObject();
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(particleType, transform.position, transform.rotation, velocity);
			Explode();
			if (playHaptic)
			{
				HapticManager.PlayObjectBreak(objectType, size.GetVolume());
			}
		}
	}
}
