using System;
using System.Collections;
using System.Threading.Tasks;
using Core;
using Newtonsoft.Json;
using UnityEngine;
using Util;

namespace RemoteConfig
{
	public static class RemoteConfigUpdater
	{
		private const string Bucket = "cannon-fest-prod.firebasestorage.app";

		private const string RootFolderName = "config_v1";

		private const float DefaultTimeoutSeconds = 5f;

		public static bool IsFetchComplete { get; private set; }

		public static IEnumerator FetchCoroutine(float timeoutSeconds = DefaultTimeoutSeconds)
		{
			IsFetchComplete = false;
			float startTime = Time.realtimeSinceStartup;
			TimeSpan timeout = TimeSpan.FromSeconds(timeoutSeconds);
			Task<string> infoTask = DownloadHelper.DownloadTextAsync(BuildInfoUrl(), timeout);
			yield return new WaitUntil(() => infoTask.IsCompleted || Time.realtimeSinceStartup - startTime >= timeoutSeconds);
			if (!infoTask.IsCompleted)
			{
				Debug.LogWarning("[RemoteConfig] Timed out before info.json download completed.");
				IsFetchComplete = true;
				yield break;
			}
			if (infoTask.IsFaulted)
			{
				Debug.LogWarning("[RemoteConfig] Failed to fetch info.json: " + infoTask.Exception?.GetBaseException().Message);
				IsFetchComplete = true;
				yield break;
			}
			if (infoTask.IsCanceled)
			{
				Debug.LogWarning("[RemoteConfig] Info.json download was canceled.");
				IsFetchComplete = true;
				yield break;
			}

			RemoteConfigInfo info;
			try
			{
				info = JsonConvert.DeserializeObject<RemoteConfigInfo>(infoTask.Result);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[RemoteConfig] Failed to parse info.json: " + ex.Message);
				IsFetchComplete = true;
				yield break;
			}
			if (info == null || string.IsNullOrEmpty(info.LatestConfigFile))
			{
				Debug.LogWarning("[RemoteConfig] Invalid info.json.");
				IsFetchComplete = true;
				yield break;
			}
			if (info.LatestVersion <= RemoteConfigService.ActiveVersion)
			{
				Debug.Log("[RemoteConfig] Remote version not newer: " + info.LatestVersion);
				IsFetchComplete = true;
				yield break;
			}

			Task<string> configTask = DownloadHelper.DownloadTextAsync(BuildConfigUrl(info.LatestConfigFile), timeout);
			yield return new WaitUntil(() => configTask.IsCompleted || Time.realtimeSinceStartup - startTime >= timeoutSeconds);
			if (!configTask.IsCompleted)
			{
				Debug.LogWarning("[RemoteConfig] Timed out before config download completed.");
				IsFetchComplete = true;
				yield break;
			}
			if (configTask.IsFaulted)
			{
				Debug.LogWarning("[RemoteConfig] Config download failed: " + configTask.Exception?.GetBaseException().Message);
				IsFetchComplete = true;
				yield break;
			}
			if (configTask.IsCanceled)
			{
				Debug.LogWarning("[RemoteConfig] Config download was canceled.");
				IsFetchComplete = true;
				yield break;
			}

			RemoteConfigData config;
			try
			{
				config = RemoteConfigService.Deserialize(configTask.Result);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[RemoteConfig] Failed to parse remote config: " + ex.Message);
				IsFetchComplete = true;
				yield break;
			}
			if (config == null)
			{
				IsFetchComplete = true;
				yield break;
			}
			if (config.Version != info.LatestVersion)
			{
				Debug.LogWarning(string.Format("[RemoteConfig] Config version mismatch: {0} != {1}", config.Version, info.LatestVersion));
			}
			RemoteConfigService.SaveToCache(config);
			RemoteConfigService.Activate(config, ConfigSource.Remote);
			Debug.Log(string.Format("[RemoteConfig] Updated to version {0} from remote.", config.Version));
			IsFetchComplete = true;
		}

		private static bool IsTimeout(Exception ex)
		{
			return ex is TimeoutException || ex is TaskCanceledException;
		}

		private static IEnumerator GetInfoCoroutine(Action<Exception> onError, Action<string> onSuccess, TimeSpan timeout)
		{
			Task<string> task = DownloadHelper.DownloadTextAsync(BuildInfoUrl(), timeout);
			yield return new WaitUntil(() => task.IsCompleted);
			if (task.IsFaulted)
			{
				onError?.Invoke(task.Exception?.GetBaseException());
			}
			else if (task.IsCanceled)
			{
				onError?.Invoke(new TimeoutException());
			}
			else
			{
				onSuccess?.Invoke(task.Result);
			}
		}

		private static string BuildInfoUrl()
		{
			return BuildUrl("info.json");
		}

		private static string BuildConfigUrl(string configFileName)
		{
			return BuildUrl(configFileName);
		}

		private static string BuildUrl(string fileName)
		{
			string path = string.Concat(RootFolderName, "/", VersionHelper.GetVersion(), "/", fileName);
			return "https://firebasestorage.googleapis.com/v0/b/" + Bucket + "/o/" + Uri.EscapeDataString(path) + "?alt=media";
		}
	}
}
