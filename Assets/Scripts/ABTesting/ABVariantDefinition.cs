using System.Collections.Generic;

namespace ABTesting
{
	public class ABVariantDefinition
	{
		public int VariantID;

		public float Ratio;

		public Dictionary<string, string> Values;

		public ABVariantDefinition(int variantID, float ratio, Dictionary<string, string> values)
		{
			VariantID = variantID;
			Ratio = ratio;
			Values = values;
		}

		public string GetValue(string key)
		{
			if (Values != null && Values.TryGetValue(key, out string value))
			{
				return value;
			}
			return null;
		}
	}
}
