using System;
using System.Collections.Generic;
using Gameplay.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Util;

namespace Level
{
	[Serializable]
	public class LevelObjectData
	{
		public int tableId;

		public ObjectType type;

		public FloatTriplet size;

		public FloatTriplet pos;

		public FloatQuartet rot;

		[JsonExtensionData]
		public IDictionary<string, JToken> extra;
	}
}
