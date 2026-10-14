using UnityEngine;

namespace Util
{
	public static class CommonUtils
	{
		public static bool IsEditor()
		{
			return Application.isEditor;
		}

		public static bool IsIOS()
		{
			return Application.platform == RuntimePlatform.IPhonePlayer;
		}

		public static bool IsAndroid()
		{
			return Application.platform == RuntimePlatform.Android;
		}

		public static void QuitApplication()
		{
			Application.Quit();
		}

		public static string GetPlatformString()
		{
			if (IsIOS())
			{
				return "ios";
			}
			if (IsAndroid())
			{
				return "android";
			}
			return "editor";
		}

		public static bool IsProd()
		{
			return true;
		}

		public static string GetBuildEnvironmentKey()
		{
			return IsProd() ? "prod" : "dev";
		}
	}
}
