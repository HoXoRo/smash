using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class RemoteTableManager
{
    private static readonly HashSet<string> s_SyncTableNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, RemoteTableKind> s_TableKinds = new Dictionary<string, RemoteTableKind>(StringComparer.Ordinal);
    private static bool s_Initialized;

    public static void EnsureInitialized(AppConfigs appConfigs)
    {
        if (s_Initialized || appConfigs == null)
        {
            return;
        }

        s_SyncTableNames.Clear();
        s_TableKinds.Clear();

        if (appConfigs.Configs != null)
        {
            foreach (string configName in appConfigs.Configs)
            {
                RegisterTable(configName, RemoteTableKind.Config);
            }
        }

        if (appConfigs.DataTables != null)
        {
            foreach (string dataTableName in appConfigs.DataTables)
            {
                RegisterTable(dataTableName, RemoteTableKind.DataTable);
            }
        }

        s_Initialized = true;
    }

    public static bool IsEnabled()
    {
        AppSettings settings = AppSettings.Instance;
        return settings != null
            && settings.EnableRemoteTableSync
            && !string.IsNullOrWhiteSpace(settings.RemoteTableBaseUrl);
    }

    public static int GetSyncTimeoutSeconds()
    {
        AppSettings settings = AppSettings.Instance;
        return settings != null && settings.RemoteTableSyncTimeoutSeconds > 0
            ? settings.RemoteTableSyncTimeoutSeconds
            : 10;
    }

    public static string GetAppVersion()
    {
        return Application.version;
    }

    public static string GetRemoteBaseUrl()
    {
        string baseUrl = AppSettings.Instance?.RemoteTableBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        string normalizedBaseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        return UtilityBuiltin.AssetsPath.GetCombinePath(normalizedBaseUrl, GetAppVersion()) + "/";
    }

    public static bool IsRemoteSyncTable(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName) && s_SyncTableNames.Contains(tableName);
    }

    public static bool TryGetTableKind(string tableName, out RemoteTableKind kind)
    {
        return s_TableKinds.TryGetValue(tableName, out kind);
    }

    public static async UniTask SyncAsync(Action<float> onProgress = null)
    {
        if (!IsEnabled())
        {
            Log.Info("Remote table sync disabled or base url empty.");
            onProgress?.Invoke(1f);
            return;
        }

        if (!s_Initialized)
        {
            Log.Warning("Remote table whitelist is not initialized, skip sync.");
            onProgress?.Invoke(1f);
            return;
        }

        await RemoteTableSyncService.SyncAsync(GetSyncTimeoutSeconds(), onProgress);
    }

    public static string ComputeMd5(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return string.Empty;
        }

        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(bytes);
        StringBuilder builder = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }

    public static string ComputeMd5(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return ComputeMd5(Encoding.UTF8.GetBytes(text));
    }

    private static void RegisterTable(string tableName, RemoteTableKind kind)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return;
        }

        s_SyncTableNames.Add(tableName);
        s_TableKinds[tableName] = kind;
    }
}
