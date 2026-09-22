using System.Collections.Generic;
using Newtonsoft.Json;

namespace RemoteConfig
{
	public class ExperimentDto
	{
		[JsonProperty("name")]
		public string Name;

		[JsonProperty("controlledKeys")]
		public List<string> ControlledKeys;

		[JsonProperty("participationPercentage")]
		public float ParticipationPercentage;

		[JsonProperty("variants")]
		public List<VariantDto> Variants;

		[JsonProperty("conditions")]
		public List<ConditionDto> Conditions;
	}
}
