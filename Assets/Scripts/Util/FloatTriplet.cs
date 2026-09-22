using System;

namespace Util
{
	[Serializable]
	public struct FloatTriplet : IEquatable<FloatTriplet>
	{
		public float x;

		public float y;

		public float z;

		private const float HashPrecision = 0.0001f;

		public FloatTriplet(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		private static int Quantize(float v)
		{
			return (int)Math.Round(v / HashPrecision);
		}

		public override int GetHashCode()
		{
			return (((Quantize(x) * 397) ^ Quantize(y)) * 397) ^ Quantize(z);
		}

		public float GetVolume()
		{
			return x * y * z;
		}

		public bool Equals(FloatTriplet other)
		{
			return Quantize(x) == Quantize(other.x) && Quantize(y) == Quantize(other.y) && Quantize(z) == Quantize(other.z);
		}

		public override bool Equals(object obj)
		{
			return obj is FloatTriplet other && Equals(other);
		}

		public static bool operator ==(FloatTriplet a, FloatTriplet b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(FloatTriplet a, FloatTriplet b)
		{
			return !a.Equals(b);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}, {2})", x, y, z);
		}
	}
}
