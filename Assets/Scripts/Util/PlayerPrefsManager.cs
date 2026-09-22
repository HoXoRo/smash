using UnityEngine;

namespace Util
{
	public static class PlayerPrefsManager
	{
		public static int GetInt(string key, int defaultValue = 0)
		{
			return PlayerPrefs.GetInt(key, defaultValue);
		}

		public static float GetFloat(string key, float defaultValue = 0f)
		{
			return PlayerPrefs.GetFloat(key, defaultValue);
		}

		public static string GetString(string key, string defaultValue = "")
		{
			return PlayerPrefs.GetString(key, defaultValue);
		}

		public static void SetInt(string key, int value)
		{
			PlayerPrefs.SetInt(key, value);
			PlayerPrefs.Save();
		}

		public static void SetFloat(string key, float value)
		{
			PlayerPrefs.SetFloat(key, value);
			PlayerPrefs.Save();
		}

		public static void SetString(string key, string value)
		{
			PlayerPrefs.SetString(key, value);
			PlayerPrefs.Save();
		}
	}
}
