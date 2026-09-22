using UnityEngine;

namespace Util
{
	public static class CustomTypeExtensions
	{
		public static Vector3 ToVector3(this IntTriplet triplet)
		{
			return new Vector3(triplet.x, triplet.y, triplet.z);
		}

		public static Vector3 ToVector3(this FloatTriplet triplet)
		{
			return new Vector3(triplet.x, triplet.y, triplet.z);
		}
	}
}
