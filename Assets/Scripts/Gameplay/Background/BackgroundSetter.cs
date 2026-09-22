using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Background
{
	public class BackgroundSetter : MonoBehaviour
	{
		[SerializeField]
		private SpriteRenderer bgRenderer;

		[SerializeField]
		private List<Sprite> sprites;

		public void Set(int stageIndex, bool isFiller)
		{
			if (bgRenderer == null || sprites == null || sprites.Count == 0)
			{
				return;
			}
			int index = Mod(stageIndex * 2 + (isFiller ? 1 : 0), sprites.Count);
			bgRenderer.sprite = sprites[index];
		}

		public static int Mod(int value, int modulo)
		{
			if (modulo == 0)
			{
				return 0;
			}
			int result = value % modulo;
			return result < 0 ? result + modulo : result;
		}
	}
}
