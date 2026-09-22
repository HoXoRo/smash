using System.Globalization;
using LocalSave;
using RemoteConfig;
using Segmentation;
using UnityEngine;

namespace ABTesting
{
	public static class ControlledKeyHelper
	{
		public static bool GetBool(ControlledKey<bool> key)
		{
			return TryParseBool(GetValue(key.Name), key.DefaultValue, key.Name);
		}

		public static float GetFloat(ControlledKey<float> key)
		{
			return TryParseFloat(GetValue(key.Name), key.DefaultValue, key.Name);
		}

		public static int GetInt(ControlledKey<int> key)
		{
			return TryParseInt(GetValue(key.Name), key.DefaultValue, key.Name);
		}

		public static string GetString(ControlledKey<string> key)
		{
			string value = GetValue(key.Name);
			return string.IsNullOrWhiteSpace(value) ? key.DefaultValue : value;
		}

		public static string GetValue(string controlledKey)
		{
			string value = ABHelper.GetAbValueForKey(controlledKey);
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
			value = RemoteConfigService.GetOverrideValue(SaveService.Data?.Country, SegmentationHelper.GetSegment(), controlledKey);
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
			return ControlledKeys.GetDefaultValue(controlledKey);
		}

		private static bool TryParseBool(string raw, bool fallback, string keyName)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return fallback;
			}
			if (bool.TryParse(raw, out bool value))
			{
				return value;
			}
			Debug.LogWarning(string.Format("[ControlledKeys] Failed to parse bool for {0}: '{1}', using {2}", keyName, raw, fallback));
			return fallback;
		}

		private static float TryParseFloat(string raw, float fallback, string keyName)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return fallback;
			}
			if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
			{
				return value;
			}
			Debug.LogWarning(string.Format("[ControlledKeys] Failed to parse float for {0}: '{1}', using {2}", keyName, raw, fallback));
			return fallback;
		}

		private static int TryParseInt(string raw, int fallback, string keyName)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return fallback;
			}
			if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
			{
				return value;
			}
			Debug.LogWarning(string.Format("[ControlledKeys] Failed to parse int for {0}: '{1}', using {2}", keyName, raw, fallback));
			return fallback;
		}
	}
}
