using LocalSave;

namespace ABTesting
{
	public class NewUserCondition : IABCondition
	{
		public bool IsSatisfied()
		{
			return SaveService.Data == null || SaveService.Data.SessionId <= 1;
		}
	}
}
