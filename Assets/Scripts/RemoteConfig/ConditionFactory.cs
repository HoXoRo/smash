using System.Collections.Generic;
using System;
using System.Linq;
using ABTesting;
using Segmentation;
using UnityEngine;

namespace RemoteConfig
{
	public static class ConditionFactory
	{
		public static List<IABCondition> CreateConditions(List<ConditionDto> conditions)
		{
			List<IABCondition> result = new List<IABCondition>();
			if (conditions == null)
			{
				return result;
			}
			foreach (ConditionDto condition in conditions)
			{
				if (condition == null || string.IsNullOrEmpty(condition.Type))
				{
					continue;
				}
				IABCondition abCondition = CreateCondition(condition);
				if (abCondition != null)
				{
					result.Add(abCondition);
				}
			}
			return result;
		}

		private static IABCondition CreateCondition(ConditionDto condition)
		{
			switch (condition.Type)
			{
			case "campaign":
				return CreateCampaignCondition(condition);
			case "segment":
				return CreateSegmentCondition(condition);
			case "country":
				return CreateCountryCondition(condition);
			case "inAB":
				return CreateInAbCondition(condition);
			case "notInAB":
				return CreateNotInAbCondition(condition);
			case "newUser":
				return new NewUserCondition();
			case "existingUser":
				return new ExistingUserCondition();
			case "never":
				return new NeverCondition();
			default:
				Debug.LogWarning("[RemoteConfig] Unknown condition type: " + condition.Type);
				return null;
			}
		}

		private static IABCondition CreateCampaignCondition(ConditionDto condition)
		{
			if (condition.Prefixes != null && condition.Prefixes.Count > 0)
			{
				return new CampaignCondition(condition.Prefixes.ToArray());
			}
			Debug.LogWarning("[RemoteConfig] campaign condition requires prefixes");
			return null;
		}

		private static IABCondition CreateSegmentCondition(ConditionDto condition)
		{
			if (condition.Segments == null || condition.Segments.Count == 0)
			{
				Debug.LogWarning("[RemoteConfig] segment condition requires segments");
				return null;
			}
			List<UserSegment> segments = new List<UserSegment>();
			foreach (string segment in condition.Segments)
			{
				if (Enum.TryParse(segment, true, out UserSegment parsed))
				{
					segments.Add(parsed);
				}
				else
				{
					Debug.LogWarning("[RemoteConfig] Unknown segment: " + segment);
				}
			}
			return segments.Count > 0 ? new SegmentCondition(segments.ToArray()) : null;
		}

		private static IABCondition CreateCountryCondition(ConditionDto condition)
		{
			if (condition.Countries != null && condition.Countries.Count > 0)
			{
				return new CountryCondition(condition.Countries.ToArray());
			}
			Debug.LogWarning("[RemoteConfig] country condition requires countries");
			return null;
		}

		private static IABCondition CreateInAbCondition(ConditionDto condition)
		{
			if (condition.AbNames != null && condition.AbNames.Count > 0)
			{
				return new InABCondition(condition.AbNames.ToArray());
			}
			Debug.LogWarning("[RemoteConfig] inAB condition requires abNames");
			return null;
		}

		private static IABCondition CreateNotInAbCondition(ConditionDto condition)
		{
			if (condition.AbNames != null && condition.AbNames.Count > 0)
			{
				return new NotInABCondition(condition.AbNames.ToArray());
			}
			Debug.LogWarning("[RemoteConfig] notInAB condition requires abNames");
			return null;
		}
	}
}
