using UnityEngine;

namespace Util
{
	public static class ScreenUtils
	{
		private const float TopSafeYNormalized = 0.93f;

		public static bool IsInTopUnsafeArea(Vector3 mousePos)
		{
			return mousePos.y >= Screen.height * TopSafeYNormalized;
		}
	}
}
