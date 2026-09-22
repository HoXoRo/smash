using UnityEngine;

namespace Gameplay.Objects
{
	public interface IContactNodeOwner
	{
		Table TouchingTable { get; set; }

		Vector3 ApplyRocketHitBoomMultiplier(Vector3 impulse);
	}
}
