using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Core;
using Inventory;
using Menu;
using Service;
using UnityEngine;

namespace Scene
{
	public static class SceneHandler
	{
		private const float LoadingDelaySeconds = 0.2f;

		private static SceneType _currentScene;

		private static GameObject _loadingCanvas;

		private static bool _isChangingScene;

		private static int _musicSerialId;

		public static bool IsInLoading => PlayLoadingUIForm.IsLoading || (_loadingCanvas != null && _loadingCanvas.activeSelf);

		public static event Action<SceneType, SceneType> OnSceneChange;

		public static event Action OnLoadingDisappear;

		public static void Init()
		{
			_currentScene = SceneType.Splash;
			_musicSerialId = 0;
			RoutineRunner.Instance.StartCoroutine(PlayInitialMusicWhenReady());
		}

		private static IEnumerator PlayInitialMusicWhenReady()
		{
			while (GF.Sound == null || !GF.Sound.HasSoundGroup(Const.SoundGroup.Music.ToString()))
			{
				yield return null;
			}
			if (!_isChangingScene)
			{
				PlayMusicForCurrentSceneSafely();
			}
		}

		public static void LoadScene(SceneType nextScene, bool forceDisableLoading = false)
		{
			nextScene = ResolveAvailableScene(nextScene);
			if (nextScene == SceneType.Gameplay)
			{
				HomePageUIForm.TryEnterGameplayAsync(false).Forget();
				return;
			}
			if (IsInLoading || _isChangingScene)
			{
				return;
			}
			Time.timeScale = 1f;
			_isChangingScene = true;
			SceneType currentScene = _currentScene;
			_currentScene = nextScene;
			InventoryTrackerHelper.RemoveAllTrackers();
			bool showLoading = !forceDisableLoading && ShouldShowLoading(currentScene, nextScene);
			if (!showLoading && !forceDisableLoading && MenuController.IsAfterWin)
			{
				showLoading = true;
			}
			if (showLoading)
			{
				MenuController.IsAfterWin = false;
				RoutineRunner.Instance.StartCoroutine(LoadingRoutine(currentScene, nextScene));
				return;
			}
			RoutineRunner.Instance.StartCoroutine(ChangeSceneWithoutLoading(currentScene, nextScene));
		}

		public static void BeginGameplayLoading()
		{
			Time.timeScale = 1f;
			InventoryTrackerHelper.RemoveAllTrackers();
			MenuController.IsAfterWin = false;
			StopMusicSafely();
			InvokeOnSceneChange(_currentScene, SceneType.Gameplay);
			_currentScene = SceneType.Gameplay;
		}

		public static void FinishGameplayLoading(bool success)
		{
			Time.timeScale = 1f;
			StopMusicSafely();
			_currentScene = success ? SceneType.Gameplay : SceneType.Menu;
			PlayMusicForCurrentSceneSafely();
		}

        public static void FinishReturnToHall()
        {
            Time.timeScale = 1f;
            MenuController.IsAfterWin = false;
            StopMusicSafely();
            InvokeOnSceneChange(_currentScene, SceneType.Menu);
            _currentScene = SceneType.Menu;
            PlayMusicForCurrentSceneSafely();
        }
		private static IEnumerator ChangeSceneWithoutLoading(SceneType currentScene, SceneType nextScene)
		{
			try
			{
				StopMusicSafely();
				InvokeOnSceneChange(currentScene, nextScene);
				yield return LoadSceneContentAsync(nextScene).ToCoroutine();
				PlayMusicForCurrentSceneSafely();
			}
			finally
			{
				_isChangingScene = false;
			}
		}

		private static async UniTask LoadSceneContentAsync(SceneType nextScene)
		{
			string sceneAssetName = UtilityBuiltin.AssetsPath.GetScenePath(nextScene.ToString());
			if (GF.Scene.SceneIsLoaded(sceneAssetName))
			{
				await GF.Scene.UnLoadSceneAwait(sceneAssetName);
			}
			await GF.Scene.LoadSceneAwait(sceneAssetName);
		}

		public static IEnumerator LoadingRoutine(SceneType currentScene, SceneType nextScene)
		{
			nextScene = ResolveAvailableScene(nextScene);
			StopMusicSafely();
			try
			{
				yield return EnsureLoadingCanvasAsync().ToCoroutine();
				if (_loadingCanvas != null)
				{
					_loadingCanvas.SetActive(true);
				}
				yield return new WaitForSecondsRealtime(LoadingDelaySeconds);
				InvokeOnSceneChange(currentScene, nextScene);
				yield return LoadSceneContentAsync(nextScene).ToCoroutine();
				yield return new WaitForSecondsRealtime(LoadingDelaySeconds);
				PlayMusicForCurrentSceneSafely();
			}
			finally
			{
				_isChangingScene = false;
				if (_loadingCanvas != null)
				{
					_loadingCanvas.SetActive(false);
				}
				InvokeOnLoadingDisappear();
			}
		}

		private static async UniTask EnsureLoadingCanvasAsync()
		{
			const string assetName = "Assets/ArrowGame/Prefabs/Flow/LoadingCanvas.prefab";
			if (_loadingCanvas != null || GF.Resource == null || GF.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist)
			{
				return;
			}
			GameObject prefab = await GF.Resource.LoadAssetAwait<GameObject>(assetName);
			if (prefab != null && _loadingCanvas == null)
			{
				_loadingCanvas = UnityEngine.Object.Instantiate(prefab);
				UnityEngine.Object.DontDestroyOnLoad(_loadingCanvas);
				_loadingCanvas.SetActive(false);
			}
		}

		private static bool ShouldShowLoading(SceneType currentScene, SceneType nextScene)
		{
			return nextScene == SceneType.Gameplay && currentScene != SceneType.Gameplay;
		}

		private static SceneType ResolveAvailableScene(SceneType scene)
		{
			return scene == SceneType.Menu ? SceneType.Gameplay : scene;
		}

		public static SceneType GetCurrentScene()
		{
			return _currentScene;
		}

		private static void StopMusicSafely()
		{
			try
			{
				if (_musicSerialId > 0 && GF.Sound != null)
				{
					GF.Sound.StopSound(_musicSerialId);
				}
				_musicSerialId = 0;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static void PlayMusicForCurrentSceneSafely()
		{
			try
			{
				/*if (_musicSerialId == 0 && GF.Sound != null && GF.Sound.HasSoundGroup(Const.SoundGroup.Music.ToString()))
				{
				 	_musicSerialId = GF.Sound.PlayBGM("bgm/bgm.mp3");
				}*/
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static void InvokeOnSceneChange(SceneType currentScene, SceneType nextScene)
		{
			Action<SceneType, SceneType> handlers = OnSceneChange;
			if (handlers == null)
			{
				return;
			}
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				try
				{
					((Action<SceneType, SceneType>)invocationList[i])(currentScene, nextScene);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}

		private static void InvokeOnLoadingDisappear()
		{
			Action handlers = OnLoadingDisappear;
			if (handlers == null)
			{
				return;
			}
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				try
				{
					((Action)invocationList[i])();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
	}
}
