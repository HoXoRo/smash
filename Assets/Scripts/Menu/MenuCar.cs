using TMPro;
using UnityEngine;

namespace Menu
{
	public class MenuCar : MonoBehaviour
	{
		[SerializeField]
		private TextMeshPro levelNoText;

		public Animator animator;

		public void SetLevelNo(int levelNo)
		{
			if (levelNoText != null)
			{
				levelNoText.text = levelNo.ToString();
			}
		}
	}
}
