using System;
using LocalSave;

namespace Core
{
	public static class UserHelper
	{
		public static string GetUserId()
		{
			if (SaveService.Data == null)
			{
				return string.Empty;
			}
			if (string.IsNullOrEmpty(SaveService.Data.UserId))
			{
				SaveService.Data.UserId = Guid.NewGuid().ToString();
			}
			return SaveService.Data.UserId;
		}
	}
}
