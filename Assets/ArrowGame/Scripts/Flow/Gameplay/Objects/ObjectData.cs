using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Util;

namespace Gameplay.Objects
{
	[CreateAssetMenu(fileName = "ObjectData", menuName = "Flow/Object Data", order = 0)]
	public class ObjectData : ScriptableObject
	{
		public List<ObjectDataItem> items;

		public GameObject tablePrefab;

		public GameObject blockerPrefab;

		public GameObject Get(ObjectType type, FloatTriplet size)
		{
			ObjectDataItem item = items?.FirstOrDefault((ObjectDataItem x) => x.type == type && x.size == size);
			return item != null ? item.prefab : null;
		}
	}
}
