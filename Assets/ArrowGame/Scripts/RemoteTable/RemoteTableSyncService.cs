using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityGameFramework.Runtime;

public static class RemoteTableSyncService
{
    private const string ManifestFileName = "manifest.json";

    private const float ManifestProgressWeight = 0.1f;

    public static async UniTask SyncAsync(int timeoutSeconds, Action<float> onProgress = null)
    {
        using CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        CancellationToken token = timeoutCts.Token;
        RemoteTableManifest mergedManifest = null;
        bool hasSuccessfulUpdate = false;

        void ReportProgress(float progress)
        {
            onProgress?.Invoke(Mathf.Clamp01(progress));
        }

        ReportProgress(0f);

        try
        {
            string manifestUrl = RemoteTableManager.GetRemoteBaseUrl() + ManifestFileName;
            Log.Info("Remote table sync start. AppVersion='{0}', Url='{1}', Timeout={2}s.",
                RemoteTableManager.GetAppVersion(), manifestUrl, timeoutSeconds);

            byte[] manifestBytes = await DownloadBytesAsync(manifestUrl, token, p =>
                ReportProgress(ManifestProgressWeight * p));
            Log.Info("Remote table manifest downloaded. Size={0}.", manifestBytes?.Length ?? 0);
            ReportProgress(ManifestProgressWeight);

            string manifestJson = RemoteTableTextUtility.BytesToJsonString(manifestBytes);
            RemoteTableManifest remoteManifest = JsonConvert.DeserializeObject<RemoteTableManifest>(manifestJson);
            if (remoteManifest?.Files == null || remoteManifest.Files.Count == 0)
            {
                Log.Warning("Remote table manifest is empty.");
                return;
            }

            Log.Info("Remote table manifest parsed. Version='{0}', FileCount={1}.", remoteManifest.Version, remoteManifest.Files.Count);

            RemoteTableManifest localManifest = RemoteTableLocalStore.LoadManifest();
            List<KeyValuePair<string, RemoteTableManifestEntry>> updateList = BuildUpdateList(remoteManifest, localManifest);
            if (updateList.Count == 0)
            {
                Log.Info("Remote table sync skipped, local cache is up to date.");
                return;
            }

            Log.Info("Remote table sync update count: {0}.", updateList.Count);
            mergedManifest = CloneManifest(localManifest);
            mergedManifest.Version = remoteManifest.Version;

            float tableProgressRange = 1f - ManifestProgressWeight;
            int updateCount = updateList.Count;
            for (int updateIndex = 0; updateIndex < updateCount; updateIndex++)
            {
                KeyValuePair<string, RemoteTableManifestEntry> updateItem = updateList[updateIndex];
                token.ThrowIfCancellationRequested();

                string tableName = updateItem.Key;
                RemoteTableManifestEntry remoteEntry = updateItem.Value;
                float tableBase = ManifestProgressWeight + tableProgressRange * updateIndex / updateCount;
                float tableStep = tableProgressRange / updateCount;

                if (!RemoteTableManager.TryGetTableKind(tableName, out RemoteTableKind kind))
                {
                    ReportProgress(tableBase + tableStep);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(remoteEntry.Path))
                {
                    Log.Warning("Remote table entry path is empty. Table='{0}'.", tableName);
                    ReportProgress(tableBase + tableStep);
                    continue;
                }

                try
                {
                    string fileUrl = RemoteTableManager.GetRemoteBaseUrl() + remoteEntry.Path.TrimStart('/');
                    byte[] jsonBytes = await DownloadBytesAsync(fileUrl, token, p =>
                        ReportProgress(tableBase + tableStep * 0.85f * p));
                    string jsonText = RemoteTableTextUtility.BytesToJsonString(jsonBytes);
                    string jsonMd5 = RemoteTableManager.ComputeMd5(
                        RemoteTableTextUtility.Utf8NoBom.GetBytes(jsonText));
                    if (!string.IsNullOrWhiteSpace(remoteEntry.Md5)
                        && !string.Equals(jsonMd5, remoteEntry.Md5, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning("Remote table md5 mismatch. Table='{0}'.", tableName);
                        ReportProgress(tableBase + tableStep);
                        continue;
                    }

                    byte[] tableBytes = JsonTableConverter.ConvertToBytes(kind, tableName, jsonText);
                    RemoteTableLocalStore.SaveBytes(kind, tableName, tableBytes);

                    mergedManifest.Files[tableName] = new RemoteTableManifestEntry
                    {
                        Version = remoteEntry.Version,
                        Md5 = string.IsNullOrWhiteSpace(remoteEntry.Md5) ? jsonMd5 : remoteEntry.Md5,
                        Size = tableBytes.LongLength,
                        Path = remoteEntry.Path
                    };
                    hasSuccessfulUpdate = true;

                    Log.Info("Remote table synced. Table='{0}'.", tableName);
                    ReportProgress(tableBase + tableStep);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Log.Warning("Remote table sync failed. Table='{0}', Error='{1}'.", tableName, exception.Message);
                    ReportProgress(tableBase + tableStep);
                }
            }

            Log.Info("Remote table sync finished.");
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Remote table sync timeout.");
        }
        catch (Exception exception)
        {
            Log.Warning("Remote table sync failed. Error='{0}'.", exception.Message);
        }
        finally
        {
            ReportProgress(1f);
            if (hasSuccessfulUpdate && mergedManifest != null)
            {
                RemoteTableLocalStore.SaveManifest(mergedManifest);
            }
        }
    }

    private static List<KeyValuePair<string, RemoteTableManifestEntry>> BuildUpdateList(
        RemoteTableManifest remoteManifest,
        RemoteTableManifest localManifest)
    {
        List<KeyValuePair<string, RemoteTableManifestEntry>> updateList = new List<KeyValuePair<string, RemoteTableManifestEntry>>();
        foreach (KeyValuePair<string, RemoteTableManifestEntry> remotePair in remoteManifest.Files)
        {
            string tableName = remotePair.Key;
            if (!RemoteTableManager.IsRemoteSyncTable(tableName))
            {
                continue;
            }

            RemoteTableManifestEntry remoteEntry = remotePair.Value;
            if (remoteEntry == null)
            {
                continue;
            }

            if (!localManifest.Files.TryGetValue(tableName, out RemoteTableManifestEntry localEntry))
            {
                updateList.Add(remotePair);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(remoteEntry.Md5)
                && !string.Equals(remoteEntry.Md5, localEntry.Md5, StringComparison.OrdinalIgnoreCase))
            {
                updateList.Add(remotePair);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(remoteEntry.Version)
                && !string.Equals(remoteEntry.Version, localEntry.Version, StringComparison.Ordinal))
            {
                updateList.Add(remotePair);
            }
        }

        return updateList;
    }

    private static RemoteTableManifest CloneManifest(RemoteTableManifest source)
    {
        RemoteTableManifest clone = new RemoteTableManifest
        {
            Version = source?.Version,
            Files = new Dictionary<string, RemoteTableManifestEntry>(StringComparer.Ordinal)
        };

        if (source?.Files == null)
        {
            return clone;
        }

        foreach (KeyValuePair<string, RemoteTableManifestEntry> pair in source.Files)
        {
            RemoteTableManifestEntry entry = pair.Value;
            if (entry == null)
            {
                continue;
            }

            clone.Files[pair.Key] = new RemoteTableManifestEntry
            {
                Version = entry.Version,
                Md5 = entry.Md5,
                Size = entry.Size,
                Path = entry.Path
            };
        }

        return clone;
    }

    private static async UniTask<byte[]> DownloadBytesAsync(string url, CancellationToken token, Action<float> onDownloadProgress = null)
    {
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = Mathf.Max(RemoteTableManager.GetSyncTimeoutSeconds(), 10);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            if (token.IsCancellationRequested)
            {
                request.Abort();
                token.ThrowIfCancellationRequested();
            }

            onDownloadProgress?.Invoke(request.downloadProgress);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        onDownloadProgress?.Invoke(1f);

        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new GameFrameworkException(Utility.Text.Format(
                "Download remote table failed. Url='{0}', Result='{1}', Error='{2}'.",
                url, request.result, request.error));
        }

        byte[] data = request.downloadHandler?.data;
        if (data == null || data.Length == 0)
        {
            throw new GameFrameworkException(Utility.Text.Format(
                "Download remote table returned empty data. Url='{0}'.", url));
        }

        return data;
    }
}
