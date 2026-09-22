using System;
using System.Collections.Generic;
using System.Linq;
using Segmentation;

namespace RemoteConfig
{
	public static class AbSimulator
	{
		public struct Context
		{
			public string Country;

			public UserSegment Segment;

			public string Campaign;

			public bool IsNewUser;

			public HashSet<string> EnrolledAbNames;
		}

		public struct ValueChance
		{
			public string Value;

			public float Probability;
		}

		public static bool IsEligible(ExperimentDto experiment, Context context)
		{
			return experiment != null && (experiment.Conditions == null || experiment.Conditions.Count == 0 || experiment.Conditions.All((ConditionDto condition) => IsConditionSatisfied(condition, context)));
		}

		public static bool IsConditionSatisfied(ConditionDto condition, Context context)
		{
			if (condition == null || string.IsNullOrEmpty(condition.Type))
			{
				return false;
			}
			switch (condition.Type)
			{
				case "never":
					return false;
				case "newUser":
					return context.IsNewUser;
				case "existingUser":
					return !context.IsNewUser;
				case "campaign":
					return !string.IsNullOrEmpty(context.Campaign) && condition.Prefixes != null && condition.Prefixes.Any((string prefix) => !string.IsNullOrEmpty(prefix) && context.Campaign.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
				case "segment":
					return condition.Segments != null && condition.Segments.Any(delegate(string segment)
					{
						return Enum.TryParse(segment, out UserSegment parsed) && parsed == context.Segment;
					});
				case "country":
					return !string.IsNullOrEmpty(context.Country) && condition.Countries != null && condition.Countries.Any((string country) => !string.IsNullOrEmpty(country) && string.Equals(country.Trim(), context.Country.Trim(), StringComparison.OrdinalIgnoreCase));
				case "inAB":
					return condition.AbNames != null && condition.AbNames.All((string name) => context.EnrolledAbNames != null && context.EnrolledAbNames.Contains(name));
				case "notInAB":
					return condition.AbNames == null || condition.AbNames.All((string name) => context.EnrolledAbNames == null || !context.EnrolledAbNames.Contains(name));
				default:
					return false;
			}
		}

		public static List<string> GetEligibleExperimentNames(RemoteConfigData config, Context context)
		{
			List<string> result = new List<string>();
			if (config?.Experiments == null)
			{
				return result;
			}
			foreach (ExperimentDto experiment in config.Experiments)
			{
				if (experiment != null && !string.IsNullOrEmpty(experiment.Name) && IsEligible(experiment, context))
				{
					result.Add(experiment.Name);
				}
			}
			return result;
		}

		public static string GetBaseValue(RemoteConfigData config, string key, Context context)
		{
			if (config == null || string.IsNullOrEmpty(key))
			{
				return null;
			}
			OverrideResolveResult overrideValue = OverrideRuleResolver.Resolve(config.Overrides, context.Country, context.Segment, key);
			if (overrideValue.HasValue)
			{
				return overrideValue.Value;
			}
			return config.Defaults != null && config.Defaults.TryGetValue(key, out string value) ? value : null;
		}

		public static List<ValueChance> GetValueDistribution(RemoteConfigData config, string key, Context context)
		{
			Dictionary<string, float> distribution = new Dictionary<string, float>();
			string baseValue = GetBaseValue(config, key, context);
			float remainingMass = 1f;
			if (config?.Experiments != null)
			{
				foreach (ExperimentDto experiment in config.Experiments)
				{
					if (experiment?.ControlledKeys == null || !experiment.ControlledKeys.Contains(key) || !IsEligible(experiment, context))
					{
						continue;
					}
					remainingMass = ApplyExperiment(experiment, key, remainingMass, distribution);
				}
			}
			Add(distribution, baseValue, remainingMass);
			return distribution.Where((KeyValuePair<string, float> pair) => pair.Value > 0f).OrderByDescending((KeyValuePair<string, float> pair) => pair.Value).ThenBy((KeyValuePair<string, float> pair) => pair.Key, StringComparer.Ordinal).Select(delegate(KeyValuePair<string, float> pair)
			{
				return new ValueChance
				{
					Value = pair.Key,
					Probability = pair.Value
				};
			}).ToList();
		}

		private static float ApplyExperiment(ExperimentDto experiment, string key, float incomingMass, Dictionary<string, float> distribution)
		{
			if (experiment.Variants == null || experiment.Variants.Count == 0 || incomingMass <= 0f)
			{
				return incomingMass;
			}
			float participationMass = incomingMass * Math.Max(0f, Math.Min(100f, experiment.ParticipationPercentage)) / 100f;
			float ratioSum = experiment.Variants.Sum((VariantDto variant) => variant?.Ratio ?? 0f);
			if (participationMass <= 0f || ratioSum <= 0f)
			{
				return incomingMass;
			}
			foreach (VariantDto variant in experiment.Variants)
			{
				if (variant?.Values == null || !variant.Values.TryGetValue(key, out string value))
				{
					continue;
				}
				Add(distribution, value, participationMass * variant.Ratio / ratioSum);
			}
			return incomingMass - participationMass;
		}

		private static void Add(Dictionary<string, float> distribution, string value, float mass)
		{
			if (mass <= 0f)
			{
				return;
			}
			value = value ?? string.Empty;
			distribution.TryGetValue(value, out float existing);
			distribution[value] = existing + mass;
		}
	}
}
