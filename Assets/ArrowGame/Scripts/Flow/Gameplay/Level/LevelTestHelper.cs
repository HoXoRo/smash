using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using UnityEngine;

namespace Gameplay.Level
{
	public class LevelTestHelper : MonoBehaviour
	{
		private const string Bucket = "cannon-fest.firebasestorage.app";

		public static void SyncLevels(string collectionName)
		{
			Debug.Log("[Levels] Syncing collection: " + collectionName);
			DownloadAndExtract(collectionName);
			Debug.Log("[Levels] Ready at: " + GetExtractedPath(collectionName));
		}

		public static void DownloadAndExtract(string collectionName)
		{
			if (string.IsNullOrWhiteSpace(collectionName))
			{
				throw new ArgumentException("Collection name is required", "collectionName");
			}
			string root = Path.Combine(Application.persistentDataPath, "levels", collectionName);
			Directory.CreateDirectory(root);
			string zipPath = Path.Combine(root, "latest.zip");
			string tmpDir = Path.Combine(root, "tmp");
			string finalDir = Path.Combine(root, "extracted");
			DownloadFile(BuildDownloadUrl(collectionName), zipPath);
			ExtractZipSafe(zipPath, tmpDir);
			Swap(tmpDir, finalDir);
		}

		public static string GetExtractedPath(string collectionName)
		{
			return Path.Combine(Application.persistentDataPath, "levels", collectionName, "extracted");
		}

		private static string BuildDownloadUrl(string collectionName)
		{
			string path = "levels/" + collectionName + ".zip";
			return "https://firebasestorage.googleapis.com/v0/b/" + Bucket + "/o/" + Uri.EscapeDataString(path) + "?alt=media";
		}

		private static void DownloadFile(string url, string outputPath)
		{
			using (HttpClient client = new HttpClient())
			{
				byte[] bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
				File.WriteAllBytes(outputPath, bytes);
			}
		}

		private static void ExtractZipSafe(string zipPath, string targetDir)
		{
			if (Directory.Exists(targetDir))
			{
				Directory.Delete(targetDir, true);
			}
			Directory.CreateDirectory(targetDir);
			string targetRoot = Path.GetFullPath(targetDir);
			using (ZipArchive archive = ZipFile.OpenRead(zipPath))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					if (string.IsNullOrEmpty(entry.Name))
					{
						continue;
					}
					string destination = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
					if (!destination.StartsWith(targetRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
					{
						throw new IOException("Zip entry escapes target directory: " + entry.FullName);
					}
					Directory.CreateDirectory(Path.GetDirectoryName(destination));
					entry.ExtractToFile(destination, true);
				}
			}
		}

		private static void Swap(string tmpDir, string finalDir)
		{
			string oldDir = finalDir + "_old";
			if (Directory.Exists(oldDir))
			{
				Directory.Delete(oldDir, true);
			}
			if (Directory.Exists(finalDir))
			{
				Directory.Move(finalDir, oldDir);
			}
			Directory.Move(tmpDir, finalDir);
			if (Directory.Exists(oldDir))
			{
				Directory.Delete(oldDir, true);
			}
		}
	}
}
