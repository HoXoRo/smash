using System;
using System.Linq;
using LocalSave;

namespace ABTesting
{
	public class CampaignCondition : IABCondition
	{
		private readonly string[] _campaigns;

		public CampaignCondition(params string[] campaigns)
		{
			_campaigns = campaigns;
		}

		public bool IsSatisfied()
		{
			string campaign = SaveService.Data?.Campaign;
			return !string.IsNullOrEmpty(campaign) && _campaigns != null && _campaigns.Any((string c) => !string.IsNullOrEmpty(c) && campaign.StartsWith(c, StringComparison.OrdinalIgnoreCase));
		}
	}
}
