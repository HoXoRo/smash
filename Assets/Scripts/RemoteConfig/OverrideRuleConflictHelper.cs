using System.Collections.Generic;
using System;
using System.Linq;
using Segmentation;

namespace RemoteConfig
{
	public static class OverrideRuleConflictHelper
	{
		public static List<string> FindWarnings(IReadOnlyList<OverrideRuleDto> rules)
		{
			List<string> warnings = new List<string>();
			if (rules == null)
			{
				return warnings;
			}
			for (int i = 0; i < rules.Count; i++)
			{
				OverrideRuleDto ruleA = rules[i];
				if (ruleA?.Values == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, string> valueA in ruleA.Values)
				{
					for (int j = i + 1; j < rules.Count; j++)
					{
						OverrideRuleDto ruleB = rules[j];
						if (ruleB?.Values == null || !ruleB.Values.TryGetValue(valueA.Key, out string valueB))
						{
							continue;
						}
						if (string.Equals(valueA.Value, valueB, StringComparison.OrdinalIgnoreCase) || !OverrideRuleResolver.HaveSameSpecificity(ruleA, ruleB))
						{
							continue;
						}
						List<string> contexts = GetOverlapContexts(ruleA, ruleB);
						if (contexts.Count == 0)
						{
							continue;
						}
						string preview = string.Join(", ", contexts.Take(3));
						if (contexts.Count >= 4)
						{
							preview += ", ...";
						}
						warnings.Add(string.Format("Rules #{0} and #{1} set different values for '{2}' in overlapping contexts ({3}). Rule #{0} wins by order.", i + 1, j + 1, valueA.Key, preview));
					}
				}
			}
			return warnings;
		}

		private static List<string> GetOverlapContexts(OverrideRuleDto ruleA, OverrideRuleDto ruleB)
		{
			List<string> countries = GetSampleCountries(ruleA, ruleB);
			List<string> segments = GetSampleSegments(ruleA, ruleB);
			List<string> contexts = new List<string>();
			foreach (string country in countries)
			{
				foreach (string segment in segments)
				{
					if (Enum.TryParse(segment, out UserSegment userSegment) && OverrideRuleResolver.Matches(ruleA, country, userSegment) && OverrideRuleResolver.Matches(ruleB, country, userSegment))
					{
						contexts.Add(country + "+" + userSegment);
					}
				}
			}
			return contexts;
		}

		private static List<string> GetSampleCountries(OverrideRuleDto ruleA, OverrideRuleDto ruleB)
		{
			bool aAll = OverrideRuleResolver.IsAll(ruleA.Countries);
			bool bAll = OverrideRuleResolver.IsAll(ruleB.Countries);
			if (aAll && bAll)
			{
				return new List<string> { "US" };
			}
			if (aAll)
			{
				return ruleB.Countries.Where((string c) => !string.IsNullOrWhiteSpace(c)).Select((string c) => c.Trim().ToUpperInvariant()).ToList();
			}
			if (bAll)
			{
				return ruleA.Countries.Where((string c) => !string.IsNullOrWhiteSpace(c)).Select((string c) => c.Trim().ToUpperInvariant()).ToList();
			}
			return ruleA.Countries.Select((string c) => c.Trim().ToUpperInvariant()).Intersect(ruleB.Countries.Select((string c) => c.Trim().ToUpperInvariant())).ToList();
		}

		private static List<string> GetSampleSegments(OverrideRuleDto ruleA, OverrideRuleDto ruleB)
		{
			bool aAll = OverrideRuleResolver.IsAll(ruleA.Segments);
			bool bAll = OverrideRuleResolver.IsAll(ruleB.Segments);
			if (aAll && bAll)
			{
				return new List<string> { UserSegment.Default.ToString() };
			}
			if (aAll)
			{
				return ruleB.Segments.Where((string s) => !string.IsNullOrWhiteSpace(s)).Select((string s) => s.Trim()).ToList();
			}
			if (bAll)
			{
				return ruleA.Segments.Where((string s) => !string.IsNullOrWhiteSpace(s)).Select((string s) => s.Trim()).ToList();
			}
			return ruleA.Segments.Select((string s) => s.Trim()).Intersect(ruleB.Segments.Select((string s) => s.Trim()), StringComparer.OrdinalIgnoreCase).ToList();
		}
	}
}
