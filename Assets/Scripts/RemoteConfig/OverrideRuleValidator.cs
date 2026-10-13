using System.Collections.Generic;
using System;
using System.Globalization;
using Segmentation;
using UnityEngine;

namespace RemoteConfig
{
	public static class OverrideRuleValidator
	{
		public static void ValidateOverrides(IReadOnlyList<OverrideRuleDto> overrides, bool throwOnError = true)
		{
			if (overrides == null)
			{
				return;
			}
			for (int i = 0; i < overrides.Count; i++)
			{
				OverrideRuleDto rule = overrides[i];
				if (rule == null)
				{
					Report(string.Format("overrides[{0}]: rule is null.", i), throwOnError);
					continue;
				}
				ValidateDimensionList(rule.Countries, string.Format("overrides[{0}].countries", i), throwOnError);
				ValidateDimensionList(rule.Segments, string.Format("overrides[{0}].segments", i), throwOnError, true);
				if (rule.Values == null || rule.Values.Count == 0)
				{
					Report(string.Format("overrides[{0}]: values must not be empty.", i), throwOnError);
					continue;
				}
				foreach (KeyValuePair<string, string> pair in rule.Values)
				{
					if (!IsKnownKey(pair.Key))
					{
						Debug.LogWarning("[RemoteConfig] Unknown override value key: " + pair.Key);
						continue;
					}
					if (!TryValidateValue(pair.Key, pair.Value, out string error))
					{
						Report(string.Format("overrides[{0}].values[{1}]: {2}", i, pair.Key, error), throwOnError);
					}
				}
			}
		}

		private static bool IsKnownKey(string key)
		{
			return key == "inter_enabled" || key == "bundles_enabled" || key == "inter_frequency"
				|| key == "inter_on_every_try" || key == "rewarded_more_lives_enabled"
				|| key == "level_collection" || key == "bidfloor_enabled";
		}

		private static bool TryValidateValue(string key, string value, out string error)
		{
			error = null;
			if (string.IsNullOrWhiteSpace(value))
			{
				error = "value is empty";
				return false;
			}
			if (key == "level_collection")
			{
				return true;
			}
			if (key == "inter_frequency")
			{
				if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
				{
					return true;
				}
				error = "expected float, got '" + value + "'";
				return false;
			}
			if (bool.TryParse(value, out _))
			{
				return true;
			}
			error = "expected bool, got '" + value + "'";
			return false;
		}

		private static void ValidateDimensionList(IReadOnlyList<string> values, string path, bool throwOnError, bool validateSegment = false)
		{
			if (values == null || values.Count == 0)
			{
				Report(path + ": must contain at least one entry or 'all'.", throwOnError);
				return;
			}
			if (OverrideRuleResolver.IsAll(values))
			{
				return;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string value in values)
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					Report(path + ": contains an empty entry.", throwOnError);
					continue;
				}
				string trimmed = value.Trim();
				if (!seen.Add(trimmed))
				{
					Report(path + ": duplicate entry '" + trimmed + "'.", throwOnError);
				}
				if (validateSegment && !Enum.TryParse(trimmed, out UserSegment _))
				{
					Report(path + ": unknown segment '" + trimmed + "'.", throwOnError);
				}
			}
		}

		private static void Report(string message, bool throwOnError)
		{
			if (throwOnError)
			{
				throw new InvalidOperationException(message);
			}
			Debug.LogWarning("[RemoteConfig] " + message);
		}
	}
}
