using ABTesting;
using Geo;
using LocalSave;
using UnityEngine;

namespace Core
{
	public static class CountryHelper
	{
		public static void EnsureInstallCountry()
		{
			if (SaveService.Data == null || !string.IsNullOrEmpty(SaveService.Data.Country))
			{
				return;
			}
			GeoClient.GetInstallCountry(OnCountryResolved, OnCountryError);
		}

		private static void OnCountryResolved(GetCountryResponseDto response)
		{
			if (response == null || string.IsNullOrEmpty(response.country) || SaveService.Data == null || !string.IsNullOrEmpty(SaveService.Data.Country))
			{
				return;
			}
			string country = response.country.Trim().ToUpperInvariant();
			SaveService.Data.Country = country;
			Debug.Log("[Country] Country resolved and set as " + country);
			ABHelper.EvaluateEligibleABs();
		}

		private static void OnCountryError(string error)
		{
			Debug.LogWarning("[Country] Failed to resolve install country: " + error);
		}
	}
}
