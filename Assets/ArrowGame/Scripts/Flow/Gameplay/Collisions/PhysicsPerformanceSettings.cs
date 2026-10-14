namespace Gameplay.Collisions
{
	public readonly struct PhysicsPerformanceSettings
	{
		public readonly int NormalSolverIterations;

		public readonly int NormalSolverVelocityIterations;

		public readonly int WarmupSolverIterations;

		public readonly int WarmupSolverVelocityIterations;

		public readonly float WarmupSeconds;

		public PhysicsPerformanceSettings(int normalSolverIterations, int normalSolverVelocityIterations, int warmupSolverIterations, int warmupSolverVelocityIterations, float warmupSeconds)
		{
			NormalSolverIterations = normalSolverIterations;
			NormalSolverVelocityIterations = normalSolverVelocityIterations;
			WarmupSolverIterations = warmupSolverIterations;
			WarmupSolverVelocityIterations = warmupSolverVelocityIterations;
			WarmupSeconds = warmupSeconds;
		}
	}
}
