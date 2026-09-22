using System;
using System.Collections.Generic;

namespace Gameplay
{
	public static class ListExtensions
	{
		private static Random rng;

		public static void Shuffle<T>(this IList<T> list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				T value = list[i];
				list[i] = list[j];
				list[j] = value;
			}
		}

		static ListExtensions()
		{
			rng = new Random();
		}
	}
}
