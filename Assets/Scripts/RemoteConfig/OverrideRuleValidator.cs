using System.Collections.Generic;
using System;
using ABTesting;
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
					if (!ControlledKeys.IsKnownKey(pair.Key))
					{
						Debug.LogWarning("[RemoteConfig] Unknown override value key: " + pair.Key);
						continue;
					}
					if (!ControlledKeys.TryValidateValue(pair.Key, pair.Value, out string error))
					{
						Report(string.Format("overrides[{0}].values[{1}]: {2}", i, pair.Key, error), throwOnError);
					}
				}
			}
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
