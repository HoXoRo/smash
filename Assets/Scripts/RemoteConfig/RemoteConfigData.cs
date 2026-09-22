using System.Collections.Generic;
using Newtonsoft.Json;

namespace RemoteConfig
{
	public class RemoteConfigData
	{
		[JsonProperty("version")]
		public int Version;

		[JsonProperty("defaults")]
		public Dictionary<string, string> Defaults;

		[JsonProperty("overrides")]
		public List<OverrideRuleDto> Overrides;

		[JsonProperty("experiments")]
		public List<ExperimentDto> Experiments;
	}
}
