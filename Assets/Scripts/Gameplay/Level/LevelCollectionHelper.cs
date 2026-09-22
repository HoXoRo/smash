using System.Collections.Generic;
using System.IO;
using ABTesting;
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
			string configured = ControlledKeyHelper.GetString(ControlledKeys.LevelCollection);
			return string.IsNullOrEmpty(configured) ? DefaultCollectionName : configured;
		}

		public static string GetCollectionToUse()
		{
			string configured = GetConfiguredCollection();
			if (!string.IsNullOrEmpty(ExternalForcedCollectionName) || !string.IsNullOrEmpty(ForcedCollectionName) || HasLocalCollection(configured))
			{
				return configured;
			}
			if (configured != DefaultCollectionName)
			{
				Debug.LogWarning("[LevelCollection] Configured collection '" + configured + "' not available locally, falling back to " + DefaultCollectionName);
			}
			return DefaultCollectionName;
		}

		public static bool HasLocalCollection(string collection)
		{
			return GetMetaDataForCollection(collection) != null;
		}

		public static string GetLevelJson(int levelIndex)
		{
			string collection = GetCollectionToUse();
			if (!LevelLoader.ForceLocal && !string.IsNullOrEmpty(collection))
			{
				string extractedPath = GetExtractedLevelPath(levelIndex, collection);
				if (File.Exists(extractedPath))
				{
					return File.ReadAllText(extractedPath);
				}
			}
			TextAsset asset = Resources.Load<TextAsset>(string.Format("levels/{0}/Level{1}", collection, levelIndex));
			return asset != null ? asset.text : null;
		}

		public static string GetExtractedCollectionPath(string collection)
		{
			return Path.Combine(Application.persistentDataPath, "levels", collection, "extracted");
		}

		private static string GetExtractedLevelPath(int levelIndex, string collection)
		{
			return Path.Combine(GetExtractedCollectionPath(collection), string.Format("Level{0}.txt", levelIndex));
		}

		public static LevelCollectionMetaData GetMetaData()
		{
			return GetMetaDataForCollection(GetCollectionToUse());
		}

		public static LevelCollectionMetaData GetMetaDataForCollection(string collection)
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
			TextAsset asset = Resources.Load<TextAsset>(string.Format("levels/{0}/metadata", collection));
			return asset != null ? JsonConvert.DeserializeObject<LevelCollectionMetaData>(asset.text) : null;
		}
	}
}
