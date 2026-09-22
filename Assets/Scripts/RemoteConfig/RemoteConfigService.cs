using System.Collections.Generic;
using System.IO;
using ABTesting;
using Newtonsoft.Json;
using Segmentation;
using UnityEngine;

namespace RemoteConfig
{
	public static class RemoteConfigService
	{
		private const string BundledResourcePath = "RemoteConfig/config";

		private const string CacheFileName = "config.json";

		private static RemoteConfigData _activeConfig;

		private static List<ABDefinition> _experiments;

		private static ConfigSource _activeSource;

		public static bool IsInitialized { get; private set; }

		public static ConfigSource ActiveSource => _activeSource;

		public static int ActiveVersion => _activeConfig?.Version ?? 0;

		public static string GetCachePath()
		{
			return Path.Combine(Application.persistentDataPath, "config_v1", "config.json");
		}

		public static void InitializeLocal()
		{
			RemoteConfigData bundled = LoadBundled();
			RemoteConfigData cached = LoadCached();
			if (cached != null)
			{
				Activate(cached, ConfigSource.Cached);
			}
			else if (bundled != null)
			{
				Activate(bundled, ConfigSource.Bundled);
			}
			else
			{
				_activeConfig = new RemoteConfigData
				{
					Defaults = new Dictionary<string, string>(),
					Overrides = new List<OverrideRuleDto>(),
					Experiments = new List<ExperimentDto>()
				};
				_experiments = new List<ABDefinition>();
			}
			IsInitialized = true;
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
			_experiments = ExperimentMapper.ToDefinitions(config.Experiments);
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

		public static RemoteConfigData LoadBundled()
		{
			TextAsset asset = Resources.Load<TextAsset>("RemoteConfig/config") ?? Resources.Load<TextAsset>("remoteconfig/config");
			if (asset == null)
			{
				Debug.LogError("[RemoteConfig] Bundled config not found at Resources/RemoteConfig/config");
				return null;
			}
			return Deserialize(asset.text);
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

		public static List<ABDefinition> GetExperiments()
		{
			if (_activeConfig == null && !IsInitialized)
			{
				InitializeLocal();
			}
			return _experiments ?? new List<ABDefinition>();
		}
	}
}
