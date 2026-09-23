using System;
using System.IO;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Gameplay.Level
{
	internal static class LevelBundledAssetReader
	{
		public static string ReadText(string assetName)
		{
			string projectFileText = TryReadProjectAssetFile(assetName);
			if (!string.IsNullOrEmpty(projectFileText))
			{
				return projectFileText;
			}

			ResourceComponent resource = GFBuiltin.Resource;
			if (resource == null || resource.HasAsset(assetName) == HasAssetResult.NotExist)
			{
				return null;
			}

			TextAsset textAsset = LoadTextAssetSync(resource, assetName);
			return textAsset != null ? textAsset.text : null;
		}

		private static TextAsset LoadTextAssetSync(ResourceComponent resource, string assetName)
		{
			UniTaskCompletionSource<TextAsset> loadTask = new UniTaskCompletionSource<TextAsset>();
			resource.LoadAsset(assetName, typeof(TextAsset), new LoadAssetCallbacks(
				(name, asset, duration, userData) =>
				{
					TextAsset textAsset = asset as TextAsset;
					if (textAsset != null)
					{
						loadTask.TrySetResult(textAsset);
						return;
					}

					loadTask.TrySetException(new GameFrameworkException(
						string.Format("Load asset failure load type is {0} but asset type is {1}.", asset.GetType(), typeof(TextAsset))));
				},
				(name, status, errorMessage, userData) => loadTask.TrySetException(new GameFrameworkException(errorMessage))));

			try
			{
				return loadTask.Task.GetAwaiter().GetResult();
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[LevelCollection] Failed to load level asset: " + assetName + ". " + exception.Message);
				return null;
			}
		}

		private static string TryReadProjectAssetFile(string assetName)
		{
			if (string.IsNullOrEmpty(assetName) || !assetName.StartsWith("Assets/"))
			{
				return null;
			}

			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string filePath = Path.Combine(projectRoot, assetName.Replace('/', Path.DirectorySeparatorChar));
			return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
		}
	}
}
