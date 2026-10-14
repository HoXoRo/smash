using System.Collections.Generic;
using System.Linq;

namespace Gameplay.Objects
{
	public static class ObjectCategoryMap
	{
		public static Dictionary<ObjectCategory, List<ObjectType>> Map;

		public static ObjectCategory GetCategory(this ObjectType type)
		{
			return Map.FirstOrDefault((KeyValuePair<ObjectCategory, List<ObjectType>> kvp) => kvp.Value.Contains(type)).Key;
		}

		static ObjectCategoryMap()
		{
			Map = new Dictionary<ObjectCategory, List<ObjectType>>
			{
				{
					ObjectCategory.Can,
					new List<ObjectType>
					{
						ObjectType.CanCylinder,
						ObjectType.CanCone,
						ObjectType.CanSquare
					}
				},
				{
					ObjectCategory.JamJar,
					new List<ObjectType>
					{
						ObjectType.JamJarBlue,
						ObjectType.JamJarPink,
						ObjectType.JamJarYellow,
						ObjectType.JamJarRed,
						ObjectType.JamJarOrange,
						ObjectType.JamJarPurple
					}
				},
				{
					ObjectCategory.Stone,
					new List<ObjectType>
					{
						ObjectType.StoneSquare,
						ObjectType.StoneCylinder
					}
				},
				{
					ObjectCategory.Wormhole,
					new List<ObjectType>
					{
						ObjectType.Wormhole
					}
				},
				{
					ObjectCategory.Box,
					new List<ObjectType>
					{
						ObjectType.BoxSquare,
						ObjectType.BoxCylinder
					}
				},
				{
					ObjectCategory.Tnt,
					new List<ObjectType>
					{
						ObjectType.Tnt
					}
				},
				{
					ObjectCategory.Ice,
					new List<ObjectType>
					{
						ObjectType.IceSquare,
						ObjectType.IceCylinder
					}
				},
				{
					ObjectCategory.Disc,
					new List<ObjectType>
					{
						ObjectType.Disc
					}
				}
			};
		}
	}
}
