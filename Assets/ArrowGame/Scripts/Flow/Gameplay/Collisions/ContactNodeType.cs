using System;

namespace Gameplay.Collisions
{
	[Flags]
	public enum ContactNodeType
	{
		None = 0,
		Table = 1,
		Object = 2,
		Ball = 4
	}
}
