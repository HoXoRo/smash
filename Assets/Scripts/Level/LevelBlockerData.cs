using System;
using UnityEngine;
using Util;

namespace Level
{
	[Serializable]
	public class LevelBlockerData
	{
		public float width = 1f;

		[HideInInspector]
		public float initZ;

		[Range(0f, 1f)]
		public float startNormalized;

		public FloatTuple minPos;

		public FloatTuple maxPos;

		public float posHalfCycleDuration;

		public float initRotEuler;

		public float rotInSecEuler;
	}
}
