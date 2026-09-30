using System;
using System.Collections;
using Audio;
using Cysharp.Threading.Tasks;
using Core;
using Inventory;
using Menu;
using Service;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene
{
	public static class SceneHandler
	{
		private const float LoadingDelaySeconds = 0.2f;

		private static SceneType _currentScene;

		private static GameObject _loadingCanvas;

		private static bool _isChangingScene;

		public static bool IsInLoading => _loadingCanvas != null && _loadingCanvas.activeSelf;

		public static event Action<SceneType, SceneType> OnSceneChange;

		public static event Action OnLoadingDisappear;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Init()
		{
			if (_loadingCanvas == null)
			{
				GameObject prefab = Resources.Load<GameObject>("LoadingCanvas");
				if (prefab != null)
				{
					_loadingCanvas = UnityEngine.Object.Instantiate(prefab);
                    UnityEngine.Object.DontDestroyOnLoad(_loadingCanvas);
					_loadingCanvas.SetActive(false);
				}
			}
			_currentScene = SceneType.Splash;
		}

		public static void LoadScene(SceneType nextScene, bool forceDisableLoading = false)
		{
			nextScene = ResolveAvailableScene(nextScene);
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
			if (GF.Scene != null && GF.Scene.SceneIsLoaded(sceneAssetName))
			{
				if (await GF.Scene.UnLoadSceneAwait(sceneAssetName))
				{
					await GF.Scene.LoadSceneAwait(sceneAssetName);
				}
				return;
			}

			await SceneManager.LoadSceneAsync(nextScene.ToString());
		}

		public static IEnumerator LoadingRoutine(SceneType currentScene, SceneType nextScene)
		{
			nextScene = ResolveAvailableScene(nextScene);
			StopMusicSafely();
			if (_loadingCanvas != null)
			{
				_loadingCanvas.SetActive(true);
			}
			try
			{
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
				ServiceLocator.Get<AudioHelper>()?.StopMusic();
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
				ServiceLocator.Get<AudioHelper>()?.PlayMusicForCurrentScene();
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
