using System;
using UnityEngine;

namespace Gameplay.GameplayTutorial.GenericItemTutorial
{
	[Serializable]
	public class GenericItemTutorialDataItem
	{
		public GenericItemTutorialType type;

		public string title;

		public Sprite itemSprite;

		public string description;
	}
}
