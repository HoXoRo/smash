using System;
using System.Collections.Generic;
using Gameplay.GameplayTutorial.GenericItemTutorial;

namespace Gameplay.GameplayTutorial
{
	[Serializable]
	public class TutorialProgressData
	{
		public bool tutorialIntroDone;

		public bool tutorialBlockerDone;

		public bool tutorialWormholeDone;

		public List<GenericItemTutorialType> genericItemTutorialsDone;

		public bool tutorialStagedLevelDone;

		public bool tutorialHorizontalTableDone;

		public bool tutorialVerticalTableDone;

		public bool tutorialRotatingTableDone;

		public bool tutorialPrelevelStreakDone;

		public bool tutorialPrelevelBoosterRocketDone;

		public static TutorialProgressData GetDefault()
		{
			return new TutorialProgressData
			{
				genericItemTutorialsDone = new List<GenericItemTutorialType>()
			};
		}
	}
}
