using System;
using Util;

namespace Level
{
	[Serializable]
	public class LevelTableData
	{
		public int id;

		public FloatTriplet pos;

		public FloatQuartet rot;

		public FloatTriplet scl;

		public FloatTriplet dim;

		public bool doRot;

		public float rotSpd;

		public bool movH;

		public float movHMin;

		public float movHMax;

		public HorizontalDirection dirH;

		public float movSpdH;

		public bool movV;

		public float movVMin;

		public float movVMax;

		public VerticalDirection dirV;

		public float movSpdV;
	}
}
