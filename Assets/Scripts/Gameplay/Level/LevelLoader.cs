using Level;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Level
{
	public static class LevelLoader
	{
		public static bool LoopActive;

		public static bool ForceLocal = true;

		public static async UniTask<LevelData> GetLevelAsync(int index)
		{
			int levelNumber = await GetLevelNumberAsync(index);
			string json = await LevelCollectionHelper.GetLevelJsonAsync(levelNumber);
			if (!string.IsNullOrEmpty(json))
			{
				return JsonConvert.DeserializeObject<LevelData>(json);
			}
			Debug.LogError(string.Format("Level not found. Index: {0}, LevelNumber: {1}", index, levelNumber));
			return null;
		}

		private static async UniTask<int> GetLevelNumberAsync(int index)
		{
			if (!LoopActive)
			{
				return index;
			}
			LevelCollectionMetaData metaData = await LevelCollectionHelper.GetMetaDataAsync();
			if (metaData == null || metaData.levelCount <= 0 || metaData.loopList == null || metaData.loopList.Count == 0 || index <= metaData.levelCount)
			{
				return index;
			}
			int loopIndex = (index - metaData.levelCount - 1) % metaData.loopList.Count;
			return metaData.loopList[loopIndex];
		}
	}
}
