using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class RemoteTableLocalStore
{
    public const string RootFolderName = "RemoteTablesApi";
    public const string ManifestFileName = "manifest.json";
    private static Snapshot s_Active;
    private static string s_LoadedRoot;

    // Include the endpoint so staging and production never share assignments.
    public static string RootPath => Path.Combine(Application.persistentDataPath, RootFolderName,
        RemoteTableManager.ComputeMd5(RemoteTableManager.GetRemoteBaseUrl() + "\n" + Application.identifier
            + "\n" + RemoteTableManager.GetPlatform() + "\n" + RemoteTableManager.GetAppVersion()));

    [Serializable]
    public sealed class Snapshot
    {
        [JsonProperty("configId")] public long ConfigId;
        [JsonProperty("batch")] public string Batch;
        [JsonProperty("manifest")] public RemoteTableManifest Manifest;
        [JsonProperty("bytesMd5")] public Dictionary<string, string> BytesMd5 = new Dictionary<string, string>();
        [JsonIgnore] public Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();
    }

    public static long? LoadAssignedConfigId()
    {
        string path = Path.Combine(RootPath, "assigned-id.txt");
        try
        {
            return File.Exists(path) && long.TryParse(File.ReadAllText(path), out long id) && id > 0 ? id : (long?)null;
        }
        catch (Exception e)
        {
            Log.Warning("Read remote config assignment failed: {0}", e.Message);
            return null;
        }
    }

    public static void SaveAssignedConfigId(long id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        WriteTextAtomic(Path.Combine(RootPath, "assigned-id.txt"), id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static Snapshot LoadActive()
    {
        string root = RootPath;
        if (s_LoadedRoot == root) return s_Active;
        s_LoadedRoot = root;
        s_Active = null;
        try
        {
            string path = Path.Combine(root, "active.json");
            if (!File.Exists(path)) return null;
            Snapshot snapshot = JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(path));
            if (snapshot == null || snapshot.ConfigId <= 0 || !Guid.TryParseExact(snapshot.Batch, "N", out _)
                || snapshot.Manifest?.Files == null || snapshot.BytesMd5 == null)
                throw new InvalidDataException("Invalid remote snapshot metadata.");
            // Reject the entire snapshot if any member is missing or corrupt.
            foreach (var pair in snapshot.Manifest.Files)
            {
                if (!RemoteTableSyncService.TryResolveTable(pair.Value.Path, out string name, out RemoteTableKind kind) || name != pair.Key)
                    throw new InvalidDataException("Snapshot contains an unregistered table.");
                string file = GetBatchFile(snapshot.Batch, name);
                byte[] bytes = File.Exists(file) ? File.ReadAllBytes(file) : null;
                if (!snapshot.BytesMd5.TryGetValue(name, out string hash) || bytes == null
                    || !string.Equals(RemoteTableManager.ComputeMd5(bytes), hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Incomplete remote snapshot: " + name);
                RemoteTableSyncService.ValidateTableBytes(kind, name, bytes);
                snapshot.Data.Add(name, bytes);
            }
            s_Active = snapshot;
        }
        catch (Exception e)
        {
            Log.Warning("Remote snapshot unavailable; use project resources. Error='{0}'.", e.Message);
        }
        return s_Active;
    }

    public static bool TryReadBytes(RemoteTableKind kind, string tableName, out byte[] bytes)
    {
        bytes = null;
        if (!RemoteTableManager.IsEnabled()) return false;
        try
        {
            Snapshot active = LoadActive();
            if (active == null || !active.Manifest.Files.ContainsKey(tableName)
                || !RemoteTableManager.TryGetTableKind(tableName, out RemoteTableKind actualKind) || kind != actualKind)
                return false;
            bytes = active.Data[tableName];
            return bytes.Length > 0;
        }
        catch (Exception e)
        {
            Log.Warning("Read remote table cache failed. Table='{0}', Error='{1}'.", tableName, e.Message);
            return false;
        }
    }

    public static string CreateBatch()
    {
        string batch = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.Combine(RootPath, "batches", batch));
        return batch;
    }

    public static string GetBatchFile(string batch, string tableName)
    {
        if (!Guid.TryParseExact(batch, "N", out _)) throw new InvalidDataException("Invalid batch ID.");
        // Hash the logical name: no server-controlled path is used as a local filesystem path.
        return Path.Combine(RootPath, "batches", batch, RemoteTableManager.ComputeMd5(tableName) + ".bytes");
    }

    public static void Commit(Snapshot snapshot)
    {
        WriteTextAtomic(Path.Combine(RootPath, "active.json"), JsonConvert.SerializeObject(snapshot));
        s_LoadedRoot = RootPath;
        s_Active = snapshot;
        // Cleanup happens only after the atomic pointer switch. Failure is harmless.
        try
        {
            foreach (string directory in Directory.GetDirectories(Path.Combine(RootPath, "batches")))
            {
                string name = Path.GetFileName(directory);
                if (name != snapshot.Batch && Guid.TryParseExact(name, "N", out _)) Directory.Delete(directory, true);
            }
        }
        catch (Exception e) { Log.Warning("Remote batch cleanup failed: {0}", e.Message); }
    }

    public static void DiscardBatch(string batch)
    {
        if (string.IsNullOrEmpty(batch) || !Guid.TryParseExact(batch, "N", out _)) return;
        try { Directory.Delete(Path.Combine(RootPath, "batches", batch), true); }
        catch (Exception e) { Log.Warning("Remote staging cleanup failed: {0}", e.Message); }
    }

    private static void WriteTextAtomic(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, RemoteTableTextUtility.Utf8NoBom);
            // Never delete the old pointer first: failures must leave the previous batch active.
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
