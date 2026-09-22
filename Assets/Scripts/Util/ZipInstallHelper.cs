using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Util
{
	public static class ZipInstallHelper
	{
		private static readonly object InFlightLock;

		private static readonly Dictionary<string, Task> InFlightByCollection;

		static ZipInstallHelper()
		{
			InFlightLock = new object();
			InFlightByCollection = new Dictionary<string, Task>();
		}

		public static Task DownloadAndInstallZipAsync(string zipUrl, string extractedDir, TimeSpan? timeout = null, CancellationToken ct = default(CancellationToken))
		{
			if (string.IsNullOrWhiteSpace(zipUrl))
			{
				return Task.FromException(new ArgumentException("URL is empty.", nameof(zipUrl)));
			}
			if (string.IsNullOrWhiteSpace(extractedDir))
			{
				return Task.FromException(new ArgumentException("Extracted dir is empty.", nameof(extractedDir)));
			}

			lock (InFlightLock)
			{
				if (InFlightByCollection.TryGetValue(extractedDir, out Task existingTask))
				{
					return existingTask;
				}

				Task task = RunInstallGuardedAsync(zipUrl, extractedDir, timeout, ct);
				InFlightByCollection[extractedDir] = task;
				return task;
			}
		}

		private static async Task RunInstallGuardedAsync(string zipUrl, string extractedDir, TimeSpan? timeout, CancellationToken ct)
		{
			try
			{
				await Task.Yield();
				await InstallAsync(zipUrl, extractedDir, timeout, ct).ConfigureAwait(false);
			}
			finally
			{
				lock (InFlightLock)
				{
					InFlightByCollection.Remove(extractedDir);
				}
			}
		}

		private static async Task InstallAsync(string zipUrl, string extractedDir, TimeSpan? timeout, CancellationToken ct)
		{
			string parent = Path.GetDirectoryName(extractedDir);
			if (!string.IsNullOrEmpty(parent))
			{
				Directory.CreateDirectory(parent);
			}

			string suffix = Guid.NewGuid().ToString("N");
			string zipPath = extractedDir + "." + suffix + ".zip";
			string tmpExtractDir = extractedDir + "." + suffix + ".tmp";

			try
			{
				await DownloadHelper.DownloadToFileAsync(zipUrl, zipPath, timeout).ConfigureAwait(false);
				if (Directory.Exists(tmpExtractDir))
				{
					Directory.Delete(tmpExtractDir, true);
				}
				await ExtractZipToDirectoryAsync(zipPath, tmpExtractDir, ct).ConfigureAwait(false);
				if (Directory.Exists(extractedDir))
				{
					Directory.Delete(extractedDir, true);
				}
				Directory.Move(tmpExtractDir, extractedDir);
			}
			finally
			{
				if (File.Exists(zipPath))
				{
					File.Delete(zipPath);
				}
				if (Directory.Exists(tmpExtractDir))
				{
					Directory.Delete(tmpExtractDir, true);
				}
			}
		}

		private static async Task ExtractZipToDirectoryAsync(string zipPath, string targetDir, CancellationToken ct)
		{
			Directory.CreateDirectory(targetDir);
			string rootFullPath = Path.GetFullPath(targetDir);
			if (!rootFullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				rootFullPath += Path.DirectorySeparatorChar;
			}

			using (FileStream fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					ct.ThrowIfCancellationRequested();
					if (string.IsNullOrEmpty(entry.Name))
					{
						continue;
					}

					string destinationPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
					if (!destinationPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
					{
						throw new IOException("Zip entry escapes target directory: " + entry.FullName);
					}

					string directory = Path.GetDirectoryName(destinationPath);
					if (!string.IsNullOrEmpty(directory))
					{
						Directory.CreateDirectory(directory);
					}

					using (Stream entryStream = entry.Open())
					using (FileStream outStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						await entryStream.CopyToAsync(outStream).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
