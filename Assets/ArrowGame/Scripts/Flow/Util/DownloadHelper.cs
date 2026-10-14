using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Util
{
	public static class DownloadHelper
	{
		private static readonly HttpClient Client;

		private static readonly TimeSpan DefaultTimeout;

		static DownloadHelper()
		{
			DefaultTimeout = TimeSpan.FromSeconds(30.0);
			Client = new HttpClient
			{
				Timeout = Timeout.InfiniteTimeSpan
			};
		}

		public static async Task DownloadToFileAsync(string url, string outputPath, TimeSpan? timeout = null)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentException("URL is empty.", nameof(url));
			}
			if (string.IsNullOrWhiteSpace(outputPath))
			{
				throw new ArgumentException("Output path is empty.", nameof(outputPath));
			}

			string directory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string tmpPath = outputPath + ".tmp";
			if (File.Exists(tmpPath))
			{
				File.Delete(tmpPath);
			}

			using (CancellationTokenSource cts = new CancellationTokenSource(timeout ?? DefaultTimeout))
			using (HttpResponseMessage response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();
				using (Stream networkStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
				using (FileStream fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					await networkStream.CopyToAsync(fileStream).ConfigureAwait(false);
				}
			}

			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
			File.Move(tmpPath, outputPath);
		}

		public static async Task<string> DownloadTextAsync(string url, TimeSpan? timeout = null)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentException("URL is empty.", nameof(url));
			}

			using (CancellationTokenSource cts = new CancellationTokenSource(timeout ?? DefaultTimeout))
			using (HttpResponseMessage response = await Client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			}
		}

		public static void DownloadText(string url, Action<string> onSuccess, Action<Exception> onError = null, TimeSpan? timeout = null)
		{
			DownloadTextAsync(url, timeout).ContinueWith(delegate(Task<string> task)
			{
				if (task.IsCanceled)
				{
					onError?.Invoke(new TimeoutException("Download timed out: " + url));
				}
				else if (task.IsFaulted)
				{
					onError?.Invoke(task.Exception?.GetBaseException() ?? new Exception("Download failed."));
				}
				else
				{
					onSuccess?.Invoke(task.Result);
				}
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}
