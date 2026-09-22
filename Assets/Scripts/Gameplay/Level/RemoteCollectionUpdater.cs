using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Core;
using Newtonsoft.Json;
using UnityEngine;
using Util;

namespace Gameplay.Level
{
	public static class RemoteCollectionUpdater
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass11_0
		{
			public string infoString;

			public Exception error;

			public Task installTask;

			internal void _003CRunDownloadCoroutine_003Eb__0(string result)
			{
			}

			internal void _003CRunDownloadCoroutine_003Eb__1(Exception ex)
			{
			}

			internal bool _003CRunDownloadCoroutine_003Eb__2()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass12_0
		{
			public Task<string> task;

			internal bool _003CGetCollectionInfoCoroutine_003Eb__0()
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003CEnsureCollectionReadyCoroutine_003Ed__10 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public float timeoutSeconds;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CEnsureCollectionReadyCoroutine_003Ed__10(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CGetCollectionInfoCoroutine_003Ed__12 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public string collectionName;

			public TimeSpan timeout;

			private _003C_003Ec__DisplayClass12_0 _003C_003E8__1;

			public Action<string> onSuccess;

			public Action<Exception> onError;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CGetCollectionInfoCoroutine_003Ed__12(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CRunDownloadCoroutine_003Ed__11 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public float timeoutSeconds;

			private _003C_003Ec__DisplayClass11_0 _003C_003E8__1;

			private string _003CcollectionName_003E5__2;

			private float _003CstartTime_003E5__3;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CRunDownloadCoroutine_003Ed__11(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		private const string Bucket = "cannon-fest-prod.firebasestorage.app";

		private const string RootFolderName = "collections_v1";

		private const float DefaultTimeoutSeconds = 8f;

		private static bool _isRunning;

		private static bool _rerunRequested;

		public static bool IsFetchComplete { get; private set; }

		public static void RequestCollectionUpdateIfNeeded()
		{
			_rerunRequested = true;
			if (!_isRunning)
			{
				RoutineRunner.Instance.StartCoroutine(EnsureCollectionReadyCoroutine(DefaultTimeoutSeconds));
			}
		}

		[IteratorStateMachine(typeof(_003CEnsureCollectionReadyCoroutine_003Ed__10))]
		public static IEnumerator EnsureCollectionReadyCoroutine(float timeoutSeconds = 8f)
		{
			return EnsureCollectionReadyCoroutineImpl(timeoutSeconds);
		}

		[IteratorStateMachine(typeof(_003CRunDownloadCoroutine_003Ed__11))]
		private static IEnumerator RunDownloadCoroutine(float timeoutSeconds)
		{
			return RunDownloadCoroutineImpl(timeoutSeconds);
		}

		[IteratorStateMachine(typeof(_003CGetCollectionInfoCoroutine_003Ed__12))]
		private static IEnumerator GetCollectionInfoCoroutine(string collectionName, Action<string> onSuccess, Action<Exception> onError, TimeSpan timeout)
		{
			return GetCollectionInfoCoroutineImpl(collectionName, onSuccess, onError, timeout);
		}

		private static string BuildCollectionZipUrl(string collectionName, string zipName)
		{
			string path = string.Concat(RootFolderName, "/", collectionName, "/", zipName);
			return "https://firebasestorage.googleapis.com/v0/b/" + Bucket + "/o/" + Uri.EscapeDataString(path) + "?alt=media";
		}

		private static string BuildCollectionInfoUrl(string collectionName)
		{
			string path = string.Concat(RootFolderName, "/", collectionName, "/info.json");
			return "https://firebasestorage.googleapis.com/v0/b/" + Bucket + "/o/" + Uri.EscapeDataString(path) + "?alt=media";
		}

		private static IEnumerator EnsureCollectionReadyCoroutineImpl(float timeoutSeconds)
		{
			if (_isRunning)
			{
				_rerunRequested = true;
				yield break;
			}

			_isRunning = true;
			IsFetchComplete = false;
			yield return RunDownloadCoroutine(timeoutSeconds);
			_isRunning = false;
			IsFetchComplete = true;

			if (_rerunRequested)
			{
				_rerunRequested = false;
				RoutineRunner.Instance.StartCoroutine(EnsureCollectionReadyCoroutine(timeoutSeconds));
			}
		}

		private static IEnumerator RunDownloadCoroutineImpl(float timeoutSeconds)
		{
			string collectionName = LevelCollectionHelper.GetConfiguredCollection();
			if (string.IsNullOrEmpty(collectionName))
			{
				yield break;
			}

			TimeSpan timeout = TimeSpan.FromSeconds(timeoutSeconds);
			string infoString = null;
			Exception error = null;
			float startTime = Time.realtimeSinceStartup;

			yield return GetCollectionInfoCoroutine(collectionName, delegate(string result)
			{
				infoString = result;
			}, delegate(Exception ex)
			{
				error = ex;
			}, timeout);

			LevelCollectionMetaData localMeta = LevelCollectionHelper.GetMetaDataForCollection(collectionName);
			if (error != null)
			{
                UnityEngine.Debug.LogWarning("[RemoteCollection] Couldn't fetch " + collectionName + "/info.json: " + error.Message);
				yield break;
			}
			if (string.IsNullOrEmpty(infoString))
			{
				if (localMeta == null)
				{
                    UnityEngine.Debug.LogWarning("[RemoteCollection] Invalid info.json for " + collectionName + ".");
				}
				yield break;
			}

			RemoteCollectionInfo info = null;
			try
			{
				info = JsonConvert.DeserializeObject<RemoteCollectionInfo>(infoString);
			}
			catch (Exception ex)
			{
                UnityEngine.Debug.LogWarning("[RemoteCollection] Invalid info.json for " + collectionName + ": " + ex.Message);
			}
			if (info == null || string.IsNullOrEmpty(info.latestZipName))
			{
                UnityEngine.Debug.LogWarning("[RemoteCollection] Invalid info.json for " + collectionName + ".");
				yield break;
			}
			if (localMeta != null && localMeta.version >= info.latestVersion)
			{
                UnityEngine.Debug.Log(string.Format("[RemoteCollection] {0} already at remote version {1}.", collectionName, localMeta.version));
				yield break;
			}

			float remainingSeconds = timeoutSeconds - (Time.realtimeSinceStartup - startTime);
			if (remainingSeconds <= 0f)
			{
                UnityEngine.Debug.LogWarning("[RemoteCollection] Timed out before download for " + collectionName + ".");
				yield break;
			}

			int localVersion = localMeta != null ? localMeta.version : 0;
            UnityEngine.Debug.Log(string.Format("[RemoteCollection] Downloading {0} v{1}, local v{2}.", collectionName, info.latestVersion, localVersion));
			string zipUrl = BuildCollectionZipUrl(collectionName, info.latestZipName);
			string extractedPath = LevelCollectionHelper.GetExtractedCollectionPath(collectionName);
			Task installTask = ZipInstallHelper.DownloadAndInstallZipAsync(zipUrl, extractedPath, TimeSpan.FromSeconds(remainingSeconds));
			yield return new WaitUntil(() => installTask.IsCompleted || Time.realtimeSinceStartup - startTime >= timeoutSeconds);

			if (!installTask.IsCompleted)
			{
                UnityEngine.Debug.LogWarning("[RemoteCollection] Timed out before download completed for " + collectionName + ".");
				yield break;
			}
			if (installTask.IsFaulted)
			{
				Exception ex = installTask.Exception?.GetBaseException();
                UnityEngine.Debug.LogWarning("[RemoteCollection] Zip download & install failed for " + collectionName + ": " + ex?.Message);
				yield break;
			}

			LevelCollectionMetaData updatedMeta = LevelCollectionHelper.GetMetaDataForCollection(collectionName);
			int? updatedVersion = updatedMeta != null ? updatedMeta.version : (int?)null;
            UnityEngine.Debug.Log(string.Format("[RemoteCollection] Update successful for {0}, version {1}.", collectionName, updatedVersion));
		}

		private static IEnumerator GetCollectionInfoCoroutineImpl(string collectionName, Action<string> onSuccess, Action<Exception> onError, TimeSpan timeout)
		{
			Task<string> task = DownloadHelper.DownloadTextAsync(BuildCollectionInfoUrl(collectionName), timeout);
			yield return new WaitUntil(() => task.IsCompleted);
			if (task.IsCanceled)
			{
				onError?.Invoke(new TimeoutException());
			}
			else if (task.IsFaulted)
			{
				onError?.Invoke(task.Exception?.GetBaseException() ?? new Exception("Download failed."));
			}
			else
			{
				onSuccess?.Invoke(task.Result);
			}
		}
	}
}
