using Level;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Level
{
	public static class LevelLoader
	{
		public static bool LoopActive;

		public static bool ForceLocal;

		public static LevelData GetLevel(int index)
		{
			int levelNumber = GetLevelNumber(index);
			string json = LevelCollectionHelper.GetLevelJson(levelNumber);
			if (!string.IsNullOrEmpty(json))
			{
				return JsonConvert.DeserializeObject<LevelData>(json);
			}
			Debug.LogError(string.Format("Level not found. Index: {0}, LevelNumber: {1}", index, levelNumber));
			return null;
		}

		private static int GetLevelNumber(int index)
		{
			if (!LoopActive)
			{
				return index;
			}
			LevelCollectionMetaData metaData = LevelCollectionHelper.GetMetaData();
			if (metaData == null || metaData.levelCount <= 0 || metaData.loopList == null || metaData.loopList.Count == 0 || index <= metaData.levelCount)
			{
				return index;
			}
			int loopIndex = (index - metaData.levelCount - 1) % metaData.loopList.Count;
			return metaData.loopList[loopIndex];
		}
	}
}
