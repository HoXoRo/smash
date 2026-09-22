using System.Linq;

namespace ABTesting
{
	public class InABCondition : IABCondition
	{
		private readonly string[] _abNames;

		public InABCondition(params string[] abNames)
		{
			_abNames = abNames;
		}

		public bool IsSatisfied()
		{
			return _abNames != null && _abNames.Length > 0 && _abNames.All(ABHelper.IsUserInVariant);
		}
	}
}
