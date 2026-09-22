using UnityEngine;

namespace Util
{
	public static class FrameRateInitializer
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void SetTargetFrameRate()
		{
		}
	}
}
