using System.Collections.Generic;
using Newtonsoft.Json;

namespace RemoteConfig
{
	public class VariantDto
	{
		[JsonProperty("variantId")]
		public int VariantId;

		[JsonProperty("ratio")]
		public float Ratio;

		[JsonProperty("values")]
		public Dictionary<string, string> Values;
	}
}
