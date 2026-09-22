using System;
using System.Linq;
using LocalSave;

namespace ABTesting
{
	public class CountryCondition : IABCondition
	{
		private readonly string[] _countries;

		public CountryCondition(params string[] countries)
		{
			_countries = countries;
		}

		public bool IsSatisfied()
		{
			string country = SaveService.Data?.Country;
			if (string.IsNullOrEmpty(country) || _countries == null)
			{
				return false;
			}
			string normalizedCountry = country.Trim();
			return _countries.Any((string c) => !string.IsNullOrEmpty(c) && string.Equals(c.Trim(), normalizedCountry, StringComparison.OrdinalIgnoreCase));
		}
	}
}
