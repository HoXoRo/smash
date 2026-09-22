using System.Collections.Generic;
using System.Linq;
using Segmentation;

namespace RemoteConfig
{
	public static class OverrideRuleResolver
	{
		public const string AllToken = "all";

		public static bool IsAll(IReadOnlyList<string> values)
		{
			return values == null || values.Count == 0 || values.Any((string value) => string.Equals(value?.Trim(), AllToken, System.StringComparison.OrdinalIgnoreCase));
		}

		public static bool MatchesCountry(IReadOnlyList<string> ruleCountries, string userCountry)
		{
			if (IsAll(ruleCountries))
			{
				return true;
			}
			if (string.IsNullOrEmpty(userCountry))
			{
				return false;
			}
			string normalizedCountry = userCountry.Trim();
			return ruleCountries.Any((string country) => !string.IsNullOrEmpty(country) && string.Equals(country.Trim(), normalizedCountry, System.StringComparison.OrdinalIgnoreCase));
		}

		public static bool MatchesSegment(IReadOnlyList<string> ruleSegments, UserSegment userSegment)
		{
			if (IsAll(ruleSegments))
			{
				return true;
			}
			string segment = userSegment.ToString();
			return ruleSegments != null && ruleSegments.Any((string value) => !string.IsNullOrEmpty(value) && string.Equals(value.Trim(), segment, System.StringComparison.OrdinalIgnoreCase));
		}

		public static bool Matches(OverrideRuleDto rule, string country, UserSegment segment)
		{
			return rule != null && MatchesCountry(rule.Countries, country) && MatchesSegment(rule.Segments, segment);
		}

		public static int GetCoarseScore(OverrideRuleDto rule)
		{
			if (rule == null)
			{
				return 0;
			}
			int score = 0;
			if (!IsAll(rule.Countries))
			{
				score++;
			}
			if (!IsAll(rule.Segments))
			{
				score++;
			}
			return score;
		}

		public static bool HaveSameSpecificity(OverrideRuleDto ruleA, OverrideRuleDto ruleB)
		{
			return ruleA != null && ruleB != null && GetCoarseScore(ruleA) == GetCoarseScore(ruleB);
		}

		public static OverrideResolveResult Resolve(IReadOnlyList<OverrideRuleDto> rules, string country, UserSegment segment, string controlledKey)
		{
			if (rules == null || string.IsNullOrEmpty(controlledKey))
			{
				return OverrideResolveResult.None;
			}
			OverrideResolveResult best = OverrideResolveResult.None;
			int bestScore = -1;
			for (int i = 0; i < rules.Count; i++)
			{
				OverrideRuleDto rule = rules[i];
				if (rule?.Values == null || !rule.Values.TryGetValue(controlledKey, out string value) || !Matches(rule, country, segment))
				{
					continue;
				}
				int score = GetCoarseScore(rule);
				if (!best.HasValue || score >= bestScore)
				{
					best = new OverrideResolveResult
					{
						Value = value,
						RuleIndex = i,
						Rule = rule
					};
					bestScore = score;
				}
			}
			return best;
		}

		public static List<string> ExpandPreview(OverrideRuleDto rule)
		{
			if (rule == null)
			{
				return new List<string>();
			}
			List<string> countries = IsAll(rule.Countries) ? new List<string> { AllToken } : rule.Countries.Where((string c) => !string.IsNullOrWhiteSpace(c)).Select((string c) => c.Trim().ToUpperInvariant()).ToList();
			List<string> segments = IsAll(rule.Segments) ? new List<string> { AllToken } : rule.Segments.Where((string s) => !string.IsNullOrWhiteSpace(s)).Select((string s) => s.Trim()).ToList();
			List<string> preview = new List<string>();
			foreach (string country in countries)
			{
				foreach (string segment in segments)
				{
					preview.Add(country + "+" + segment);
				}
			}
			return preview;
		}
	}
}
