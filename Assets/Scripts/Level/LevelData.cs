using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Objects;

namespace Level
{
	[Serializable]
	public class LevelData
	{
		public int levelIndex;

		public int moveCount;

		public LevelDifficulty difficulty;

		public List<LevelStageData> stages;

		public int GetStageCount()
		{
			return stages?.Count ?? 0;
		}

		public List<ObjectType> GetDistinctObjectTypes()
		{
			if (stages == null)
			{
				return new List<ObjectType>();
			}
			return stages
				.Where((LevelStageData stage) => stage?.objects != null)
				.SelectMany((LevelStageData stage) => stage.objects)
				.Select((LevelObjectData obj) => obj.type)
				.Distinct()
				.ToList();
		}
	}
}
