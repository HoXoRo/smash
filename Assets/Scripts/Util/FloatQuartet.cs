using System;

namespace Util
{
	[Serializable]
	public struct FloatQuartet
	{
		public float x;

		public float y;

		public float z;

		public float w;

		public FloatQuartet(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}
}
