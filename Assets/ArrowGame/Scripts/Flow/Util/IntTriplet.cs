using System;

namespace Util
{
	[Serializable]
	public struct IntTriplet : IEquatable<IntTriplet>
	{
		public int x;

		public int y;

		public int z;

		public IntTriplet(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public bool Equals(IntTriplet other)
		{
			return x == other.x && y == other.y && z == other.z;
		}

		public override bool Equals(object obj)
		{
			return obj is IntTriplet other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (((x * 397) ^ y) * 397) ^ z;
		}

		public int GetVolume()
		{
			return x * y * z;
		}

		public static bool operator ==(IntTriplet a, IntTriplet b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(IntTriplet a, IntTriplet b)
		{
			return !a.Equals(b);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}, {2})", x, y, z);
		}
	}
}
