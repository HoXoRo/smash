using System;
using System.Collections.Generic;

namespace Level
{
	[Serializable]
	public class LevelStageData
	{
		public List<LevelObjectData> objects;

		public List<LevelTableData> tables;

		public List<LevelBlockerData> blockers;
	}
}
