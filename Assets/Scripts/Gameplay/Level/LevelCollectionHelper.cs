using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Level
{
	public static class LevelCollectionHelper
	{
		public const string DefaultCollectionName = "prod-14";

		public static string GetConfiguredCollection()
		{
			return DefaultCollectionName;
		}

		public static UniTask<string> GetCollectionToUseAsync()
		{
			return UniTask.FromResult(DefaultCollectionName);
		}

		public static async UniTask<string> GetLevelJsonAsync(int levelIndex)
		{
			string collection = await GetCollectionToUseAsync();
			if (!LevelLoader.ForceLocal && !string.IsNullOrEmpty(collection))
			{
				string extractedPath = GetExtractedLevelPath(levelIndex, collection);
				if (File.Exists(extractedPath))
				{
					return File.ReadAllText(extractedPath);
				}
			}
			return await LevelBundledAssetReader.ReadTextAsync(UtilityBuiltin.AssetsPath.GetLevelJsonPath(collection, levelIndex));
		}

		public static string GetExtractedCollectionPath(string collection)
		{
			return Path.Combine(Application.persistentDataPath, "levels", collection, "extracted");
		}

		private static string GetExtractedLevelPath(int levelIndex, string collection)
		{
			return Path.Combine(GetExtractedCollectionPath(collection), string.Format("Level{0}.txt", levelIndex));
		}

		public static async UniTask<LevelCollectionMetaData> GetMetaDataAsync()
		{
			return await GetMetaDataForCollectionAsync(await GetCollectionToUseAsync());
		}

		public static async UniTask<LevelCollectionMetaData> GetMetaDataForCollectionAsync(string collection)
		{
			if (string.IsNullOrEmpty(collection))
			{
				return null;
			}
			if (!LevelLoader.ForceLocal)
			{
				string extractedPath = Path.Combine(GetExtractedCollectionPath(collection), "metadata.json");
				if (File.Exists(extractedPath))
				{
					return JsonConvert.DeserializeObject<LevelCollectionMetaData>(File.ReadAllText(extractedPath));
				}
			}
			string metadataJson = await LevelBundledAssetReader.ReadTextAsync(UtilityBuiltin.AssetsPath.GetLevelMetadataPath(collection));
			return !string.IsNullOrEmpty(metadataJson)
				? JsonConvert.DeserializeObject<LevelCollectionMetaData>(metadataJson)
				: null;
		}
	}
}
