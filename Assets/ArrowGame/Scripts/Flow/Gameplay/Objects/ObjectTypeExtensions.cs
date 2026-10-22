namespace Gameplay.Objects
{
	public static class ObjectTypeExtensions
	{
		public static string GetUnderCategoryName(this ObjectType type)
		{
			// These labels must not depend on enum names renamed by Obfuz.
            ObjectCategory category = type.GetCategory();
            switch (category)
            {
                case ObjectCategory.Can: return "Can";
                case ObjectCategory.JamJar: return "JamJar";
                case ObjectCategory.Stone: return "Stone";
                case ObjectCategory.Wormhole: return "Wormhole";
                case ObjectCategory.Box: return "Box";
                case ObjectCategory.Tnt: return "Tnt";
                case ObjectCategory.Ice: return "Ice";
                case ObjectCategory.Disc: return "Disc";
                default: return ((int)category).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
		}
	}
}
