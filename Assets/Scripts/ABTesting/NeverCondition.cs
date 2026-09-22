namespace ABTesting
{
	public class NeverCondition : IABCondition
	{
		public bool IsSatisfied()
		{
			return false;
		}
	}
}
