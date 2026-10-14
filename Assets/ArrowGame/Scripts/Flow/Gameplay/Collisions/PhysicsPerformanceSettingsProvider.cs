using Util;

namespace Gameplay.Collisions
{
	public static class PhysicsPerformanceSettingsProvider
	{
		public static PhysicsPerformanceSettings GetSettings(DevicePerformanceTier tier)
		{
			switch (tier)
			{
			case DevicePerformanceTier.Low:
				return new PhysicsPerformanceSettings(10, 3, 50, 10, 2f);
			case DevicePerformanceTier.Mid:
				return new PhysicsPerformanceSettings(20, 6, 100, 25, 2f);
			case DevicePerformanceTier.High:
				return new PhysicsPerformanceSettings(50, 12, 200, 50, 2f);
			default:
				return new PhysicsPerformanceSettings(16, 4, 50, 8, 0.5f);
			}
		}
	}
}
