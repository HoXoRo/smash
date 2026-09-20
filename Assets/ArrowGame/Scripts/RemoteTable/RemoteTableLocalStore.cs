using System;
using System.IO;
using System.Text;
using GameFramework;
using Newtonsoft.Json;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class RemoteTableLocalStore
{
    public const string RootFolderName = "RemoteTables";
    public const string ManifestFileName = "manifest.json";

    public static string RootPath => Utility.Path.GetRegularPath(
        Path.Combine(Application.persistentDataPath, RootFolderName, RemoteTableManager.GetAppVersion()));

    public static string GetBytesFilePath(RemoteTableKind kind, string tableName)
    {
        string folder = kind == RemoteTableKind.Config ? "config" : "datatable";
        return Utility.Path.GetRegularPath(Path.Combine(RootPath, folder, tableName + ".bytes"));
    }

    public static string GetManifestPath()
    {
        return Utility.Path.GetRegularPath(Path.Combine(RootPath, ManifestFileName));
    }

    public static bool TryReadBytes(RemoteTableKind kind, string tableName, out byte[] bytes)
    {
        bytes = null;
        string filePath = GetBytesFilePath(kind, tableName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            bytes = File.ReadAllBytes(filePath);
            return bytes != null && bytes.Length > 0;
        }
        catch (Exception exception)
        {
            Log.Warning("Read remote table cache failed. Table='{0}', Error='{1}'.", tableName, exception.Message);
            return false;
        }
    }

    public static RemoteTableManifest LoadManifest()
    {
        string manifestPath = GetManifestPath();
        if (!File.Exists(manifestPath))
        {
            return new RemoteTableManifest();
        }

        try
        {
            string json = File.ReadAllText(manifestPath, RemoteTableTextUtility.Utf8NoBom);
            json = RemoteTableTextUtility.TrimBom(json);
            return JsonConvert.DeserializeObject<RemoteTableManifest>(json) ?? new RemoteTableManifest();
        }
        catch (Exception exception)
        {
            Log.Warning("Load local remote table manifest failed. Error='{0}'.", exception.Message);
            return new RemoteTableManifest();
        }
    }

    public static void SaveManifest(RemoteTableManifest manifest)
    {
        if (manifest == null)
        {
            return;
        }

        string manifestPath = GetManifestPath();
        WriteAllTextAtomic(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
    }

    public static void SaveBytes(RemoteTableKind kind, string tableName, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            throw new ArgumentException("Remote table bytes is invalid.", nameof(bytes));
        }

        string filePath = GetBytesFilePath(kind, tableName);
        WriteAllBytesAtomic(filePath, bytes);
    }

    private static void WriteAllBytesAtomic(string filePath, byte[] bytes)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = filePath + ".tmp";
        File.WriteAllBytes(tempPath, bytes);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        File.Move(tempPath, filePath);
    }

    private static void WriteAllTextAtomic(string filePath, string content)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, content, RemoteTableTextUtility.Utf8NoBom);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        File.Move(tempPath, filePath);
    }
}
