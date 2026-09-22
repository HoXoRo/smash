using Audio;
using Gameplay.Collisions;
using Gameplay.Particles;
using Haptics;
using Level;
using Newtonsoft.Json.Linq;
using Service;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Gameplay.Objects
{
	public class DiscObject : BaseObject
	{
		[Range(0.1f, 10f)]
		[SerializeField]
		private float diameter;

		public override void Initialize(LevelObjectData objectData)
		{
			ObjectData = objectData;
			if (objectData != null)
			{
				size = objectData.size;
				if (objectData.extra != null && objectData.extra.TryGetValue("diameter", out JToken diameterToken))
				{
					diameter = diameterToken.Value<float>();
				}
			}
			Resize();
		}

		private void OnValidate()
		{
			Resize();
		}

		private void Resize()
		{
			if (rb == null)
			{
				return;
			}
			float height = size.z == 0f ? rb.transform.localScale.z : size.z;
			rb.transform.localScale = new Vector3(diameter, diameter, height);
			size = new FloatTriplet(diameter, diameter, height);
		}

		public override void SetObjectMass()
		{
			if (rb != null)
			{
				rb.mass = GetMass(objectType, size.GetVolume());
			}
		}

		protected override int GetLayer()
		{
			return LayerMask.NameToLayer("ObjectCollider");
		}

		protected override void OnGroundHit(Collision collision)
		{
			ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.ObjectGroundHit, transform.position, transform.rotation, GetLastVelocity());
			DestroyObject();
		}

		public override Audio.AudioType GetHitAudioType()
		{
			return Audio.AudioType.None;
		}

		public override BallObjectCollisionResult OnBallHit(BallObjectCollision collision)
		{
			return BallObjectCollisionResult.None;
		}

		public override LevelObjectData GetDataForLevel()
		{
			Table table = GetComponentInParent<Table>(true);
			return new LevelObjectData
			{
				tableId = table != null ? table.Id : 0,
				type = objectType,
				size = size,
				pos = new FloatTriplet(transform.position.x, transform.position.y, transform.position.z),
				rot = new FloatQuartet(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w),
				extra = new Dictionary<string, JToken>
				{
					["diameter"] = diameter
				}
			};
		}

		public override void DestroyByTnt()
		{
			DestroyObject();
		}

		public override void DestroyByRocket()
		{
			DestroyObject();
			HapticManager.PlayObjectBreak(objectType, size.GetVolume());
		}
	}
}
