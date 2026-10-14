using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using LocalSave;
using Newtonsoft.Json;
using Segmentation;
using UnityEngine;

namespace RemoteConfig
{
	public static class RemoteConfigService
	{
		private const string BundledAssetName = "Assets/ArrowGame/Config/Flow/config.json";

		private const string CacheFileName = "config.json";

		private static RemoteConfigData _activeConfig;

		private static ConfigSource _activeSource;

		private static bool _bundledLoaded;

		public static bool IsInitialized { get; private set; }

		public static ConfigSource ActiveSource => _activeSource;

		public static int ActiveVersion => _activeConfig?.Version ?? 0;

		public static string GetCachePath()
		{
			return Path.Combine(Application.persistentDataPath, "config_v1", "config.json");
		}

		public static void InitializeLocal()
		{
			if (!IsInitialized)
			{
				_activeConfig = new RemoteConfigData
				{
					Defaults = new Dictionary<string, string>(),
					Overrides = new List<OverrideRuleDto>()
				};
				IsInitialized = true;
			}
		}

		public static async UniTask InitializeLocalAsync()
		{
			InitializeLocal();
			try
			{
				if (_bundledLoaded || GF.Resource == null || GF.Resource.HasAsset(BundledAssetName) == GameFramework.Resource.HasAssetResult.NotExist)
				{
					return;
				}
				TextAsset asset = await GF.Resource.LoadAssetAwait<TextAsset>(BundledAssetName);
				RemoteConfigData bundled = Deserialize(asset.text);
				if (bundled != null)
				{
					_bundledLoaded = true;
					if (_activeSource == ConfigSource.Bundled)
					{
						Activate(bundled, ConfigSource.Bundled);
					}
				}
			}
			catch (System.Exception exception)
			{
				Debug.LogWarning("[RemoteConfig] Failed to load bundled config: " + exception.Message);
			}
		}

		public static void Activate(RemoteConfigData config, ConfigSource source)
		{
			if (config == null)
			{
				Debug.LogError("[RemoteConfig] Cannot activate null config");
				return;
			}
			_activeConfig = config;
			_activeSource = source;
			IsInitialized = true;
		}

		public static void SaveToCache(RemoteConfigData config)
		{
			if (config == null)
			{
				return;
			}
			string cachePath = GetCachePath();
			string directory = Path.GetDirectoryName(cachePath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}
			File.WriteAllText(cachePath, JsonConvert.SerializeObject(config, Formatting.Indented));
		}

		public static RemoteConfigData LoadCached()
		{
			string cachePath = GetCachePath();
			return File.Exists(cachePath) ? Deserialize(File.ReadAllText(cachePath)) : null;
		}

		public static RemoteConfigData Deserialize(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<RemoteConfigData>(json);
		}

		public static string GetDefaultValue(string controlledKey)
		{
			if (_activeConfig == null && !IsInitialized)
			{
				InitializeLocal();
			}
			if (_activeConfig?.Defaults != null && _activeConfig.Defaults.TryGetValue(controlledKey, out string value))
			{
				return value;
			}
			return null;
		}

		public static string GetOverrideValue(string country, UserSegment segment, string controlledKey)
		{
			if (_activeConfig == null && !IsInitialized)
			{
				InitializeLocal();
			}
			OverrideResolveResult result = OverrideRuleResolver.Resolve(_activeConfig?.Overrides, country, segment, controlledKey);
			return result.HasValue ? result.Value : null;
		}

		public static string GetValue(string key)
		{
			string value = GetOverrideValue(SaveService.Data?.Country, SegmentationHelper.GetSegment(), key);
			return string.IsNullOrWhiteSpace(value) ? GetDefaultValue(key) : value;
		}

		public static bool GetBool(string key, bool fallback)
		{
			string value = GetValue(key);
			if (string.IsNullOrWhiteSpace(value))
			{
				return fallback;
			}
			if (bool.TryParse(value, out bool parsed))
			{
				return parsed;
			}
			Debug.LogWarning("[RemoteConfig] Invalid bool for " + key + ": '" + value + "'");
			return fallback;
		}
	}
}
