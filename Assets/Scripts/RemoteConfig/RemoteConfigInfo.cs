using Newtonsoft.Json;

namespace RemoteConfig
{
	public class RemoteConfigInfo
	{
		[JsonProperty("latestVersion")]
		public int LatestVersion;

		[JsonProperty("latestConfigFile")]
		public string LatestConfigFile;
	}
}
