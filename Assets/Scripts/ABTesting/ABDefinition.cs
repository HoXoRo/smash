using System.Collections.Generic;
using System.Linq;

namespace ABTesting
{
	public class ABDefinition
	{
		public string Name;

		public List<string> ControlledKeyNames;

		public List<ABVariantDefinition> Variants;

		public List<IABCondition> Conditions;

		public float ParticipationPercentage;

		public ABDefinition(string name, List<string> controlledKeys, float participationPercentage, List<ABVariantDefinition> variants, List<IABCondition> conditions = null)
		{
			Name = name;
			ControlledKeyNames = controlledKeys;
			ParticipationPercentage = participationPercentage;
			Variants = variants;
			Conditions = conditions;
		}

		public bool Controls(string key)
		{
			return ControlledKeyNames != null && ControlledKeyNames.Contains(key);
		}

		public bool AreConditionsMet()
		{
			return Conditions == null || Conditions.All((IABCondition condition) => condition.IsSatisfied());
		}
	}
}
