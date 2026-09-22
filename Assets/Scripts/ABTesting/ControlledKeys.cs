using System.Collections.Generic;
using System.Globalization;
using RemoteConfig;

namespace ABTesting
{
	public static class ControlledKeys
	{
		public static readonly ControlledKey<bool> InterEnabled;

		public static readonly ControlledKey<bool> BundlesEnabled;

		public static readonly ControlledKey<float> InterFrequency;

		public static readonly ControlledKey<bool> InterOnEveryTry;

		public static readonly ControlledKey<bool> RewardedMoreLivesEnabled;

		public static readonly ControlledKey<string> LevelCollection;

		public static readonly ControlledKey<bool> BidfloorEnabled;

		private static readonly HashSet<string> AllKeyNames;

		private static readonly List<string> AllKeyNamesOrdered;

		static ControlledKeys()
		{
			InterEnabled = new ControlledKey<bool>("inter_enabled", true);
			BundlesEnabled = new ControlledKey<bool>("bundles_enabled", false);
			InterFrequency = new ControlledKey<float>("inter_frequency", 120f);
			InterOnEveryTry = new ControlledKey<bool>("inter_on_every_try", false);
			RewardedMoreLivesEnabled = new ControlledKey<bool>("rewarded_more_lives_enabled", true);
			LevelCollection = new ControlledKey<string>("level_collection", "prod-14");
			BidfloorEnabled = new ControlledKey<bool>("bidfloor_enabled", false);
			AllKeyNamesOrdered = new List<string>
			{
				InterEnabled.Name,
				BundlesEnabled.Name,
				InterFrequency.Name,
				InterOnEveryTry.Name,
				RewardedMoreLivesEnabled.Name,
				LevelCollection.Name,
				BidfloorEnabled.Name
			};
			AllKeyNames = new HashSet<string>(AllKeyNamesOrdered);
		}

		public static bool IsKnownKey(string key)
		{
			return !string.IsNullOrEmpty(key) && AllKeyNames.Contains(key);
		}

		public static bool TryValidateValue(string key, string value, out string error)
		{
			error = null;
			if (string.IsNullOrWhiteSpace(value))
			{
				error = "value is empty";
				return false;
			}
			if (!IsKnownKey(key))
			{
				error = "unknown controlled key";
				return false;
			}
			if (key == InterFrequency.Name)
			{
				if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
				{
					return true;
				}
				error = "expected float, got '" + value + "'";
				return false;
			}
			if (key == LevelCollection.Name)
			{
				return true;
			}
			if (bool.TryParse(value, out _))
			{
				return true;
			}
			error = "expected bool, got '" + value + "'";
			return false;
		}

		public static string GetDefaultValue(string controlledKey)
		{
			return RemoteConfigService.GetDefaultValue(controlledKey);
		}

		public static IReadOnlyList<string> GetAllKeyNamesOrdered()
		{
			return AllKeyNamesOrdered;
		}
	}
}
