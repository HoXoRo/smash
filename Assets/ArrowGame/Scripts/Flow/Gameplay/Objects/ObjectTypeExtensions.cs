namespace Gameplay.Objects
{
	public static class ObjectTypeExtensions
	{
		public static string GetUnderCategoryName(this ObjectType type)
		{
			return type.GetCategory().ToString();
		}
	}
}
