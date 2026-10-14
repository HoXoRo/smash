using System.Collections.Generic;
using Gameplay.Objects;

namespace Gameplay.GameplayTutorial.GenericItemTutorial
{
	public static class GenericItemTutorialMap
	{
		private static Dictionary<ObjectType, GenericItemTutorialType> _map;

		public static GenericItemTutorialType GetTutorialTypeToShow(ObjectType objectType)
		{
			return _map.TryGetValue(objectType, out GenericItemTutorialType tutorialType) ? tutorialType : GenericItemTutorialType.None;
		}

		static GenericItemTutorialMap()
		{
			_map = new Dictionary<ObjectType, GenericItemTutorialType>
			{
				[ObjectType.JamJarBlue] = GenericItemTutorialType.Jar,
				[ObjectType.JamJarPink] = GenericItemTutorialType.Jar,
				[ObjectType.JamJarYellow] = GenericItemTutorialType.Jar,
				[ObjectType.JamJarRed] = GenericItemTutorialType.Jar,
				[ObjectType.JamJarOrange] = GenericItemTutorialType.Jar,
				[ObjectType.JamJarPurple] = GenericItemTutorialType.Jar,
				[ObjectType.StoneSquare] = GenericItemTutorialType.Stone,
				[ObjectType.StoneCylinder] = GenericItemTutorialType.Stone,
				[ObjectType.BoxSquare] = GenericItemTutorialType.BoxSquare,
				[ObjectType.BoxCylinder] = GenericItemTutorialType.BoxCylinder,
				[ObjectType.Tnt] = GenericItemTutorialType.Tnt,
				[ObjectType.IceSquare] = GenericItemTutorialType.Ice,
				[ObjectType.IceCylinder] = GenericItemTutorialType.Ice
			};
		}
	}
}
