using System;

namespace Util
{
	[Serializable]
	public struct FloatTuple
	{
		public float x;

		public float y;

		public FloatTuple(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
	}
}
