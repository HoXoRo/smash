using System;
using Level;
using UnityEngine;

namespace Gameplay.GameplayTutorial
{
	public abstract class TutorialBase : MonoBehaviour
	{
		public abstract void Play(TutorialManager manager, Action<int, int> onStepComplete);

		public abstract bool ShouldPlay(LevelData levelData);
	}
}
