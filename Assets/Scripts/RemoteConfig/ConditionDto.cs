using System.Collections.Generic;
using Newtonsoft.Json;

namespace RemoteConfig
{
	public class ConditionDto
	{
		[JsonProperty("type")]
		public string Type;

		[JsonProperty("prefixes")]
		public List<string> Prefixes;

		[JsonProperty("segments")]
		public List<string> Segments;

		[JsonProperty("abNames")]
		public List<string> AbNames;

		[JsonProperty("countries")]
		public List<string> Countries;
	}
}
