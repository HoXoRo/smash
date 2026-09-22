using UnityEngine;

namespace Util
{
	public class VelocityPair
	{
		public Vector3 LinearVelocity;

		public Vector3 AngularVelocity;

		public VelocityPair(Vector3 linearVelocity, Vector3 angularVelocity)
		{
			LinearVelocity = linearVelocity;
			AngularVelocity = angularVelocity;
		}
	}
}
