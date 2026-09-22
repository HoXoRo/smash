namespace RemoteConfig
{
	public struct OverrideResolveResult
	{
		public string Value;

		public int? RuleIndex;

		public OverrideRuleDto Rule;

		public static OverrideResolveResult None => default(OverrideResolveResult);

		public bool HasValue => Value != null;
	}
}
