using System.Collections.Generic;
using Newtonsoft.Json;

namespace RemoteConfig
{
	public class OverrideRuleDto
	{
		[JsonProperty("countries")]
		public List<string> Countries;

		[JsonProperty("segments")]
		public List<string> Segments;

		[JsonProperty("values")]
		public Dictionary<string, string> Values;
	}
}
