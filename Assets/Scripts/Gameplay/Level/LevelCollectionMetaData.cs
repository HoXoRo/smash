using System;
using System.Collections.Generic;

namespace Gameplay.Level
{
	[Serializable]
	public class LevelCollectionMetaData
	{
		public int version;

		public int levelCount;

		public List<int> loopList;
	}
}
