using UnityEngine;

namespace Menu.UI
{
	public class HomeCoinPanelController : MonoBehaviour
	{
		[SerializeField]
		private FlowButton button;

		private void Start()
		{
			if (button != null)
			{
				button.Interactable = false;
			}
		}
	}
}
