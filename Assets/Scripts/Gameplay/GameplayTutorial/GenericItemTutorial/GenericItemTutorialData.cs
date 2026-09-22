using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.GameplayTutorial.GenericItemTutorial
{
	[CreateAssetMenu(fileName = "GenericItemTutorialData", menuName = "GenericItemTutorialData", order = 0)]
	public class GenericItemTutorialData : ScriptableObject
	{
		public List<GenericItemTutorialDataItem> items;
	}
}
