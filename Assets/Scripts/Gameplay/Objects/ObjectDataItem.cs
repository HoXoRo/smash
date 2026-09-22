using System;
using UnityEngine;
using Util;

namespace Gameplay.Objects
{
	[Serializable]
	public class ObjectDataItem
	{
		public ObjectType type;

		public FloatTriplet size;

		public GameObject prefab;
	}
}
