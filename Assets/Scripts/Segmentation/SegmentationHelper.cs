using System;
using LocalSave;

namespace Segmentation
{
	public static class SegmentationHelper
	{
		private const string BlendedToken = "BLDROAS";

		private const string AdToken = "ADROAS";

		private const string IapToken = "IAPROAS";

		public static UserSegment GetSegment()
		{
			return GetPersistedSegment();
		}

		public static void EvaluateSegment()
		{
			Decide();
		}

		private static UserSegment Decide()
		{
			UserSegment persisted = GetPersistedSegment();
			if (persisted != UserSegment.Default)
			{
				return persisted;
			}
			UserSegment resolved = ResolveFromCampaign(SaveService.Data?.Campaign);
			if (resolved != UserSegment.Default && SaveService.Data != null)
			{
				SaveService.Data.Segment = resolved.ToString();
			}
			return resolved;
		}

		private static UserSegment GetPersistedSegment()
		{
			if (SaveService.Data == null || string.IsNullOrEmpty(SaveService.Data.Segment))
			{
				return UserSegment.Default;
			}
			return Enum.TryParse(SaveService.Data.Segment, true, out UserSegment segment) ? segment : UserSegment.Default;
		}

		private static UserSegment ResolveFromCampaign(string campaign)
		{
			if (string.IsNullOrEmpty(campaign))
			{
				return UserSegment.Default;
			}
			if (Contains(campaign, BlendedToken))
			{
				return UserSegment.BlendedUser;
			}
			if (Contains(campaign, IapToken))
			{
				return UserSegment.IAPUser;
			}
			return Contains(campaign, AdToken) ? UserSegment.AdUser : UserSegment.Default;
		}

		private static bool Contains(string campaign, string token)
		{
			return !string.IsNullOrEmpty(campaign) && campaign.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
