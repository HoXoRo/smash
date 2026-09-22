using LocalSave;

namespace ABTesting
{
	public class ExistingUserCondition : IABCondition
	{
		public bool IsSatisfied()
		{
			return SaveService.Data != null && SaveService.Data.SessionId > 1;
		}
	}
}
