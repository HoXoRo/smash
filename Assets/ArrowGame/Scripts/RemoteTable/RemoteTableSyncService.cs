using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.DataTable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityGameFramework.Runtime;

public static class RemoteTableSyncService
{
    private static bool s_Syncing;

    public static async UniTask SyncAsync(int timeoutSeconds, Action<float> onProgress = null)
    {
        if (s_Syncing || !RemoteTableManager.IsEnabled()) return;
        s_Syncing = true;
        string batch = null;
        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        CancellationToken token = timeout.Token;
        try
        {
            onProgress?.Invoke(0f);
            string root = RemoteTableManager.GetRemoteBaseUrl();
            if (!Uri.TryCreate(root, UriKind.Absolute, out Uri endpoint)
                || (endpoint.Scheme != "https" && endpoint.Scheme != "http")
                || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
                throw new InvalidDataException("Invalid remote table API base URL.");

            string gaid = await GetGaidAsync(token);
            var payload = new Dictionary<string, object>
            {
                ["package_name"] = Application.identifier,
                ["version"] = RemoteTableManager.GetAppVersion(),
                ["platform"] = RemoteTableManager.GetPlatform(),
                ["config_id"] = RemoteTableLocalStore.LoadAssignedConfigId()
            };
            if (gaid != null) payload["gaid"] = gaid;
            string body = JsonConvert.SerializeObject(payload);
            string resolveUrl = root + "/api/public/config-file-groups/resolve";
            Log.Info("Remote table resolve request. Method=POST, Url={0}, Body={1}", resolveUrl, body);
            using UnityWebRequest resolve = new UnityWebRequest(resolveUrl, "POST");
            resolve.uploadHandler = new UploadHandlerRaw(RemoteTableTextUtility.Utf8NoBom.GetBytes(body));
            resolve.downloadHandler = new DownloadHandlerBuffer();
            resolve.SetRequestHeader("Content-Type", "application/json");
            byte[] manifestBytes = await SendAsync(resolve, token, p => onProgress?.Invoke(p * 0.1f));
            string configId = resolve.GetResponseHeader("X-Config-Id");
            if (string.IsNullOrWhiteSpace(configId))
                throw new InvalidDataException("Resolve response is missing a valid X-Config-Id.");
            string configKey = resolve.GetResponseHeader("X-Config-Key");
            // Older deployed servers return the download key in X-Config-Id only.
            // Never override a supplied key or use a numeric/invalid ID as a path segment.
            if (string.IsNullOrEmpty(configKey) && IsDownloadKey(configId))
            {
                configKey = configId;
                Log.Info("Remote table compatibility: X-Config-Key absent; using legacy X-Config-Id download key.");
            }
            if (!IsDownloadKey(configKey))
                throw new InvalidDataException("Resolve response has no valid download key (X-Config-Key or legacy X-Config-Id).");
            string audience = resolve.GetResponseHeader("X-Config-Audience");
            if (audience != "all" && audience != "paid" && audience != "organic") audience = "unknown";
            JObject manifestJson = JObject.Parse(RemoteTableTextUtility.BytesToJsonString(manifestBytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (!(manifestJson["files"] is JObject)) throw new InvalidDataException("Resolve response has no files object.");
            RemoteTableManifest manifest = manifestJson.ToObject<RemoteTableManifest>();
            RemoteTableManifest selected = SelectTables(manifest);
            // Keep assignment even if downloading this batch later fails. Do not redraw.
            RemoteTableLocalStore.SaveAssignedConfigId(configId);
            Log.Info("Remote table assignment resolved. ConfigId={0}, Audience={1}.", configId, audience);
            RemoteTableLocalStore.Snapshot active = RemoteTableLocalStore.LoadActive();
            if (IsUnchanged(active, configId, selected))
            {
                Log.Info("Remote tables unchanged. ConfigId={0}, Total={1}, Downloaded=0, Reused={1}.", configId, selected.Files.Count);
                return;
            }

            var added = selected.Files.Keys.Where(name => active == null || !active.Manifest.Files.ContainsKey(name)).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var changed = selected.Files.Where(pair => active != null
                && active.Manifest.Files.TryGetValue(pair.Key, out RemoteTableManifestEntry old)
                && !SameContent(pair.Value, old)).Select(pair => pair.Key).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var removed = active == null ? Array.Empty<string>()
                : active.Manifest.Files.Keys.Where(name => !selected.Files.ContainsKey(name)).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            Log.Info("Remote table sync plan. PreviousConfigId={0}, ConfigId={1}, Total={2}, Added=[{3}], Changed=[{4}], Removed=[{5}].",
                active?.ConfigId ?? "none", configId, selected.Files.Count, string.Join(", ", added), string.Join(", ", changed), string.Join(", ", removed));

            batch = RemoteTableLocalStore.CreateBatch();
            var pending = new RemoteTableLocalStore.Snapshot
            {
                ConfigId = configId, Batch = batch, Manifest = selected
            };
            int completed = 0;
            int downloaded = 0;
            int reused = 0;
            foreach (var pair in selected.Files)
            {
                token.ThrowIfCancellationRequested();
                string name = pair.Key;
                RemoteTableManifestEntry entry = pair.Value;
                RemoteTableManager.TryGetTableKind(name, out RemoteTableKind kind);
                byte[] tableBytes;
                bool downloadedTable = false;
                // Identical content can be reused across configuration groups.
                if (active != null
                    && active.Manifest.Files.TryGetValue(name, out RemoteTableManifestEntry oldEntry)
                    && SameContent(entry, oldEntry))
                {
                    tableBytes = active.Data[name];
                    if (!string.Equals(RemoteTableManager.ComputeMd5(tableBytes), active.BytesMd5[name], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Cached table changed during sync: " + name);
                }
                else
                {
                    string path = string.Join("/", entry.Path.Split('/').Select(Uri.EscapeDataString));
                    string url = root + "/api/public/config-folders/" + configKey + "/" + path;
                    using UnityWebRequest download = UnityWebRequest.Get(url);
                    byte[] raw = await SendAsync(download, token,
                        p => onProgress?.Invoke(0.1f + 0.9f * (completed + p) / Math.Max(1, selected.Files.Count)));
                    // The manifest hashes the actual UTF-8 file, including any BOM or whitespace.
                    if (raw.LongLength != entry.Size || !string.Equals(RemoteTableManager.ComputeMd5(raw), entry.Md5, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Remote table size/MD5 mismatch: " + name);
                    tableBytes = JsonTableConverter.ConvertToBytes(kind, name, RemoteTableTextUtility.BytesToJsonString(raw));
                    downloadedTable = true;
                }
                ValidateTableBytes(kind, name, tableBytes);
                File.WriteAllBytes(RemoteTableLocalStore.GetBatchFile(batch, name), tableBytes);
                pending.BytesMd5.Add(name, RemoteTableManager.ComputeMd5(tableBytes));
                pending.Data.Add(name, tableBytes);
                if (downloadedTable)
                {
                    downloaded++;
                    Log.Info("Remote table downloaded and validated (pending batch activation). ConfigId={0}, Table='{1}', Path='{2}', Size={3}, Md5={4}.",
                        configId, name, entry.Path, entry.Size, entry.Md5);
                }
                else
                {
                    reused++;
                }
                completed++;
                onProgress?.Invoke(0.1f + 0.9f * completed / Math.Max(1, selected.Files.Count));
            }
            token.ThrowIfCancellationRequested();
            RemoteTableLocalStore.Commit(pending);
            batch = null;
            Log.Info("Remote table batch activated. ConfigId={0}, Total={1}, Downloaded={2}, Reused={3}, Removed={4}.", configId, selected.Files.Count, downloaded, reused, removed.Length);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Remote table sync timed out; keep the previous complete batch.");
        }
        catch (Exception e)
        {
            Log.Warning("Remote table sync failed; keep the previous complete batch. Error='{0}'.", e.Message);
        }
        finally
        {
            RemoteTableLocalStore.DiscardBatch(batch);
            s_Syncing = false;
            onProgress?.Invoke(1f);
        }
    }

    private static bool IsDownloadKey(string value)
    {
        return value != null && value.Length == 32
            && value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
    }

    private static async UniTask<string> GetGaidAsync(CancellationToken token)
    {
        // 暂时不需要做区分
        return null;
#if UNITY_ANDROID && !UNITY_EDITOR
        // AdvertisingIdClient blocks: call CommonHelper off the Unity main thread.
        // Bound the wait; a late native result cannot change this sync's request.
        Task<string> request = Task.Run(() =>
        {
            bool attached = false;
            try
            {
                attached = AndroidJNI.AttachCurrentThread() == 0;
                return attached ? CommonHelper.GetGAID() : null;
            }
            catch { return null; }
            finally { if (attached) AndroidJNI.DetachCurrentThread(); }
        });
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!request.IsCompleted && elapsed.ElapsedMilliseconds < 1000)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        token.ThrowIfCancellationRequested();
        if (!request.IsCompleted || request.IsFaulted || request.IsCanceled) return null;
        return NormalizeGaid(request.Result);
#else
        await UniTask.Yield(PlayerLoopTiming.Update);
        token.ThrowIfCancellationRequested();
        return null;
#endif
    }

    internal static string NormalizeGaid(string value)
    {
        value = value?.Trim();
        if (value == null || value.Length > 64 || !Guid.TryParseExact(value, "D", out Guid id)
            || id == Guid.Empty || id == new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")) return null;
        return id.ToString("D");
    }

    internal static RemoteTableManifest SelectTables(RemoteTableManifest manifest)
    {
        if (manifest?.Files == null) throw new InvalidDataException("Invalid remote manifest.");
        var selected = new RemoteTableManifest { Version = manifest.Version };
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in manifest.Files)
        {
            RemoteTableManifestEntry entry = pair.Value;
            if (entry == null) throw new InvalidDataException("Null manifest entry.");
            ValidatePath(entry.Path);
            if (!paths.Add(entry.Path)) throw new InvalidDataException("Duplicate manifest path: " + entry.Path);
            // Server dictionary keys are not stable when filenames collide; use path only.
            if (!TryResolveTable(entry.Path, out string name, out _)) continue;
            if (entry.Size <= 0 || entry.Md5 == null || entry.Md5.Length != 32 || !entry.Md5.All(Uri.IsHexDigit))
                throw new InvalidDataException("Invalid size/MD5 for table: " + name);
            if (selected.Files.ContainsKey(name)) throw new InvalidDataException("Duplicate table: " + name);
            selected.Files.Add(name, entry);
        }
        return selected;
    }

    internal static bool TryResolveTable(string path, out string name, out RemoteTableKind kind)
    {
        ValidatePath(path);
        name = null;
        kind = default;
        string prefix;
        if (path.StartsWith("config/", StringComparison.Ordinal)) { prefix = "config/"; kind = RemoteTableKind.Config; }
        else if (path.StartsWith("datatable/", StringComparison.Ordinal)) { prefix = "datatable/"; kind = RemoteTableKind.DataTable; }
        else return false;
        if (!path.EndsWith(".json", StringComparison.Ordinal)) return false;
        name = path.Substring(prefix.Length, path.Length - prefix.Length - 5);
        return RemoteTableManager.IsRemoteSyncTable(name) && RemoteTableManager.TryGetTableKind(name, out RemoteTableKind registered) && registered == kind;
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(new[] { '\\', ':', '?', '#', '%', '\0' }) >= 0
            || path.Any(char.IsControl) || path.Split('/').Any(p => p.Length == 0 || p == "." || p == ".."))
            throw new InvalidDataException("Invalid manifest relative path.");
    }

    private static bool SameContent(RemoteTableManifestEntry a, RemoteTableManifestEntry b)
    {
        return b != null && a.Path == b.Path && a.Size == b.Size && string.Equals(a.Md5, b.Md5, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnchanged(RemoteTableLocalStore.Snapshot active, string configId, RemoteTableManifest selected)
    {
        return active != null && active.ConfigId == configId && active.Manifest.Files.Count == selected.Files.Count
            && selected.Files.All(p => active.Manifest.Files.TryGetValue(p.Key, out RemoteTableManifestEntry old) && SameContent(p.Value, old));
    }

    // Validate without adding anything to the live GF.Config/GF.DataTable registries.
    internal static void ValidateTableBytes(RemoteTableKind kind, string name, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) throw new InvalidDataException("Empty table: " + name);
        using var stream = new MemoryStream(bytes, false);
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8);
        if (kind == RemoteTableKind.Config)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            while (stream.Position < stream.Length)
            {
                string key = reader.ReadString();
                reader.ReadString();
                if (string.IsNullOrEmpty(key) || !keys.Add(key)) throw new InvalidDataException("Invalid/duplicate config key: " + key);
            }
            return;
        }
        Type rowType = Type.GetType(Path.GetFileName(name.Split('_')[0]));
        if (rowType == null || !typeof(DataRowBase).IsAssignableFrom(rowType)) throw new InvalidDataException("Unknown row type: " + name);
        var ids = new HashSet<int>();
        while (stream.Position < stream.Length)
        {
            int length = reader.Read7BitEncodedInt32();
            if (length <= 0 || length > stream.Length - stream.Position) throw new InvalidDataException("Invalid row length: " + name);
            var row = (DataRowBase)Activator.CreateInstance(rowType);
            if (!row.ParseDataRow(bytes, (int)stream.Position, length, null) || !ids.Add(row.Id))
                throw new InvalidDataException("Invalid/duplicate row: " + name);
            stream.Position += length;
        }
    }

    private static async UniTask<byte[]> SendAsync(UnityWebRequest request, CancellationToken token, Action<float> progress)
    {
        token.ThrowIfCancellationRequested();
        request.timeout = Math.Max(1, RemoteTableManager.GetSyncTimeoutSeconds());
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            if (token.IsCancellationRequested) { request.Abort(); token.ThrowIfCancellationRequested(); }
            progress?.Invoke(Mathf.Clamp01(request.downloadProgress));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        token.ThrowIfCancellationRequested();
        if (request.result != UnityWebRequest.Result.Success)
            throw new IOException("Remote API HTTP " + request.responseCode + ": " + request.error);
        byte[] data = request.downloadHandler?.data;
        if (data == null || data.Length == 0) throw new InvalidDataException("Empty remote API response.");
        progress?.Invoke(1f);
        return data;
    }
}
