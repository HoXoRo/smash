#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class RemoteTableJsonExporter
    {
        private const string ExportRootFolderName = "RemoteTableExport";

        [MenuItem("Game Framework/GameTools/Export Remote Table Json", false, 42)]
        public static void ExportRemoteTableJson()
        {
            AppConfigs appConfig = AppConfigs.GetInstanceEditor();
            if (appConfig == null)
            {
                Debug.LogError("AppConfigs not found.");
                return;
            }

            string exportRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ExportRootFolderName, Application.version));
            Directory.CreateDirectory(exportRoot);

            RemoteTableManifest manifest = new RemoteTableManifest
            {
                Version = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Files = new Dictionary<string, RemoteTableManifestEntry>(StringComparer.Ordinal)
            };

            if (appConfig.Configs != null)
            {
                foreach (string configName in appConfig.Configs)
                {
                    ExportConfig(configName, exportRoot, manifest);
                }
            }

            if (appConfig.DataTables != null)
            {
                foreach (string dataTableName in appConfig.DataTables)
                {
                    if (RemoteTableManager.IsExcludedDataTable(dataTableName))
                    {
                        continue;
                    }

                    ExportDataTable(dataTableName, exportRoot, manifest);
                }
            }

            string manifestPath = Path.Combine(exportRoot, "manifest.json");
            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented), RemoteTableTextUtility.Utf8NoBom);
            AssetDatabase.Refresh();
            Debug.Log($"Export remote table json success: {exportRoot}");
            EditorUtility.RevealInFinder(exportRoot);
        }

        private static void ExportConfig(string configName, string exportRoot, RemoteTableManifest manifest)
        {
            string txtPath = UtilityBuiltin.AssetsPath.GetConfigPath(configName, false);
            string fullTxtPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", txtPath));
            if (!File.Exists(fullTxtPath))
            {
                Debug.LogWarning($"Config txt not found: {fullTxtPath}");
                return;
            }

            Dictionary<string, string> configDict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(fullTxtPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                string[] columns = line.Split('\t');
                if (columns.Length < 4)
                {
                    continue;
                }

                configDict[columns[1]] = columns[3];
            }

            string relativePath = $"config/{configName}.json";
            string outputPath = Path.Combine(exportRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            string plainJson = JsonConvert.SerializeObject(configDict, Formatting.Indented);
            string json = BuildExportJson(configName, plainJson, isBase64Payload: false);
            File.WriteAllText(outputPath, json, RemoteTableTextUtility.Utf8NoBom);
            AddManifestEntry(manifest, configName, relativePath, json);
        }

        private static void ExportDataTable(string dataTableName, string exportRoot, RemoteTableManifest manifest)
        {
            string bytesAssetPath = UtilityBuiltin.AssetsPath.GetDataTablePath(dataTableName, true);
            string fullBytesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", bytesAssetPath));
            if (!File.Exists(fullBytesPath))
            {
                Debug.LogWarning($"DataTable bytes not found: {fullBytesPath}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(fullBytesPath);
            string plainPayload = Convert.ToBase64String(bytes);
            string json = BuildExportJson(Path.GetFileName(dataTableName), plainPayload, isBase64Payload: true);

            string relativePath = $"datatable/{dataTableName}.json";
            string outputPath = Path.Combine(exportRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, json, RemoteTableTextUtility.Utf8NoBom);
            AddManifestEntry(manifest, dataTableName, relativePath, json);
        }

        private static string BuildExportJson(string tableName, string plainPayload, bool isBase64Payload)
        {
            if (!RemoteTableCrypto.IsEnabled())
            {
                if (isBase64Payload)
                {
                    return JsonConvert.SerializeObject(new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["tableName"] = tableName,
                        ["bytesBase64"] = plainPayload
                    }, Formatting.Indented);
                }

                return plainPayload;
            }

            string encrypted = RemoteTableCrypto.EncryptPayload(plainPayload);
            return RemoteTableCrypto.WrapEncryptedJson(tableName, encrypted);
        }

        private static void AddManifestEntry(RemoteTableManifest manifest, string tableName, string relativePath, string json)
        {
            byte[] jsonBytes = RemoteTableTextUtility.Utf8NoBom.GetBytes(json);
            manifest.Files[tableName] = new RemoteTableManifestEntry
            {
                Version = manifest.Version,
                Md5 = ComputeMd5(jsonBytes),
                Size = jsonBytes.LongLength,
                Path = relativePath.Replace('\\', '/')
            };
        }

        private static string ComputeMd5(byte[] bytes)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
#endif
