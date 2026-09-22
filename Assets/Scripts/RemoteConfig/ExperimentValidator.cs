using System.Collections.Generic;
using System;
using System.Linq;
using ABTesting;
using Segmentation;
using UnityEngine;

namespace RemoteConfig
{
	public static class ExperimentValidator
	{
		public static readonly string[] ConditionTypes;

		private static readonly HashSet<string> KnownConditionTypes;

		public static void Validate(List<ExperimentDto> experiments)
		{
			if (experiments == null)
			{
				return;
			}
			HashSet<string> names = new HashSet<string>();
			foreach (ExperimentDto experiment in experiments)
			{
				if (experiment == null)
				{
					throw new InvalidOperationException("Experiment is null.");
				}
				if (string.IsNullOrWhiteSpace(experiment.Name))
				{
					throw new InvalidOperationException("Experiment name is empty.");
				}
				if (!names.Add(experiment.Name))
				{
					throw new InvalidOperationException("Duplicate experiment name: " + experiment.Name);
				}
				if (experiment.ParticipationPercentage < 0f || experiment.ParticipationPercentage > 100f)
				{
					throw new InvalidOperationException(experiment.Name + ": participation percentage must be between 0 and 100.");
				}
				if (experiment.Variants == null || experiment.Variants.Count == 0)
				{
					throw new InvalidOperationException(experiment.Name + ": variants must not be empty.");
				}
				if (experiment.Variants.Sum((VariantDto variant) => variant?.Ratio ?? 0f) <= 0f)
				{
					throw new InvalidOperationException(experiment.Name + ": variant ratios must be greater than 0.");
				}
				ValidateConditions(experiment);
				ValidateValues(experiment);
			}
		}

		private static void ValidateValues(ExperimentDto experiment)
		{
			foreach (VariantDto variant in experiment.Variants)
			{
				if (variant?.Values == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, string> pair in variant.Values)
				{
					if (!ControlledKeys.IsKnownKey(pair.Key))
					{
						Debug.LogWarning("[RemoteConfig] Unknown experiment value key: " + pair.Key);
						continue;
					}
					if (!ControlledKeys.TryValidateValue(pair.Key, pair.Value, out string error))
					{
						throw new InvalidOperationException(string.Format("{0}/variant {1}/values[{2}]: {3}", experiment.Name, variant.VariantId, pair.Key, error));
					}
				}
			}
		}

		private static void ValidateConditions(ExperimentDto experiment)
		{
			if (experiment.Conditions == null)
			{
				return;
			}
			foreach (ConditionDto condition in experiment.Conditions)
			{
				if (condition == null)
				{
					throw new InvalidOperationException(experiment.Name + ": condition is null.");
				}
				if (string.IsNullOrEmpty(condition.Type) || !KnownConditionTypes.Contains(condition.Type))
				{
					throw new InvalidOperationException(experiment.Name + ": unknown condition type '" + condition.Type + "'.");
				}
				if (condition.Type == "campaign" && (condition.Prefixes == null || condition.Prefixes.Count == 0))
				{
					throw new InvalidOperationException(experiment.Name + ": campaign condition requires prefixes.");
				}
				if (condition.Type == "segment")
				{
					if (condition.Segments == null || condition.Segments.Count == 0)
					{
						throw new InvalidOperationException(experiment.Name + ": segment condition requires segments.");
					}
					foreach (string segment in condition.Segments)
					{
						if (!Enum.TryParse(segment, out UserSegment _))
						{
							throw new InvalidOperationException(experiment.Name + ": unknown segment '" + segment + "'.");
						}
					}
				}
				if ((condition.Type == "country") && (condition.Countries == null || condition.Countries.Count == 0))
				{
					throw new InvalidOperationException(experiment.Name + ": country condition requires countries.");
				}
				if ((condition.Type == "inAB" || condition.Type == "notInAB") && (condition.AbNames == null || condition.AbNames.Count == 0))
				{
					throw new InvalidOperationException(experiment.Name + ": " + condition.Type + " condition requires abNames.");
				}
			}
		}

		static ExperimentValidator()
		{
			ConditionTypes = new[]
			{
				"never",
				"newUser",
				"existingUser",
				"campaign",
				"segment",
				"country",
				"inAB",
				"notInAB"
			};
			KnownConditionTypes = new HashSet<string>(ConditionTypes);
		}
	}
}
