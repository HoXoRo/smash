using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using RemoteConfig;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Level
{
	public static class LevelCollectionHelper
	{
		public static string ExternalForcedCollectionName;

		public const string DefaultCollectionName = "prod-14";

		public static HashSet<string> IncludedCollectionsForProd = new HashSet<string>
		{
			DefaultCollectionName
		};

		public static string ForcedCollectionName => PlayerPrefs.GetString("ForcedCollectionName", string.Empty);

		public static string GetConfiguredCollection()
		{
			if (!string.IsNullOrEmpty(ExternalForcedCollectionName))
			{
				return ExternalForcedCollectionName;
			}
			if (!string.IsNullOrEmpty(ForcedCollectionName))
			{
				return ForcedCollectionName;
			}
			string configured = RemoteConfigService.GetValue("level_collection");
			return string.IsNullOrEmpty(configured) ? DefaultCollectionName : configured;
		}

		public static async UniTask<string> GetCollectionToUseAsync()
		{
			string configured = GetConfiguredCollection();
			if (!string.IsNullOrEmpty(ExternalForcedCollectionName) || !string.IsNullOrEmpty(ForcedCollectionName) || await HasLocalCollectionAsync(configured))
			{
				return configured;
			}
			if (configured != DefaultCollectionName)
			{
				Debug.LogWarning("[LevelCollection] Configured collection '" + configured + "' not available locally, falling back to " + DefaultCollectionName);
			}
			return DefaultCollectionName;
		}

		public static async UniTask<bool> HasLocalCollectionAsync(string collection)
		{
			return await GetMetaDataForCollectionAsync(collection) != null;
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
