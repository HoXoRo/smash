using System.Linq;

namespace ABTesting
{
	public class NotInABCondition : IABCondition
	{
		private readonly string[] _abNames;

		public NotInABCondition(params string[] abNames)
		{
			_abNames = abNames;
		}

		public bool IsSatisfied()
		{
			return _abNames == null || _abNames.All((string name) => !ABHelper.IsUserInVariant(name));
		}
	}
}
