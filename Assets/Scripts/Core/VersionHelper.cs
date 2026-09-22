using UnityEngine;

namespace Core
{
	public static class VersionHelper
	{
		private const string VersionResourcePath = "version";

		private static string _cachedVersion;

		public static string GetVersion()
		{
			if (string.IsNullOrEmpty(_cachedVersion))
			{
				TextAsset textAsset = Resources.Load<TextAsset>(VersionResourcePath);
				if (textAsset != null)
				{
					_cachedVersion = textAsset.text.Trim();
				}
				else
				{
					Debug.LogError("VersionHelper: version.txt not found in Resources");
					_cachedVersion = string.Empty;
				}
			}
			return _cachedVersion;
		}
	}
}
