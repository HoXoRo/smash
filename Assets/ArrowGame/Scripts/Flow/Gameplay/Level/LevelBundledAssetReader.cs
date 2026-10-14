using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using UnityEngine;

namespace Gameplay.Level
{
	internal static class LevelBundledAssetReader
	{
		public static async UniTask<string> ReadTextAsync(string assetName)
		{
			if (GF.Resource == null || GF.Resource.HasAsset(assetName) == HasAssetResult.NotExist)
			{
				return null;
			}

			try
			{
				TextAsset asset = await GF.Resource.LoadAssetAwait<TextAsset>(assetName);
				return asset != null ? asset.text : null;
			}
			catch (System.Exception exception)
			{
				Debug.LogWarning("[LevelCollection] Failed to load level asset: " + assetName + ". " + exception.Message);
				return null;
			}
		}
	}
}
