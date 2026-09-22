using System.Collections.Generic;
using System.Linq;
using RemoteConfig;

namespace ABTesting
{
	public static class ABLibrary
	{
		public static List<ABDefinition> ABDefs => RemoteConfigService.GetExperiments();

		public static IEnumerable<ABDefinition> GetABsForKey(string key)
		{
			return ABDefs.Where((ABDefinition x) => x.Controls(key));
		}

		public static ABDefinition GetABForName(string name)
		{
			return ABDefs.FirstOrDefault((ABDefinition x) => x.Name == name);
		}
	}
}
