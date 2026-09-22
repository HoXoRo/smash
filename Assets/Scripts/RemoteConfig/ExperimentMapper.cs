using System.Collections.Generic;
using ABTesting;

namespace RemoteConfig
{
	public static class ExperimentMapper
	{
		public static List<ABDefinition> ToDefinitions(List<ExperimentDto> experiments)
		{
			List<ABDefinition> definitions = new List<ABDefinition>();
			if (experiments == null)
			{
				return definitions;
			}
			foreach (ExperimentDto experiment in experiments)
			{
				if (experiment == null || string.IsNullOrEmpty(experiment.Name))
				{
					continue;
				}
				List<ABVariantDefinition> variants = new List<ABVariantDefinition>();
				if (experiment.Variants != null)
				{
					foreach (VariantDto variant in experiment.Variants)
					{
						if (variant != null)
						{
							variants.Add(new ABVariantDefinition(variant.VariantId, variant.Ratio, variant.Values ?? new Dictionary<string, string>()));
						}
					}
				}
				definitions.Add(new ABDefinition(experiment.Name, experiment.ControlledKeys ?? new List<string>(), experiment.ParticipationPercentage, variants, ConditionFactory.CreateConditions(experiment.Conditions)));
			}
			return definitions;
		}
	}
}
