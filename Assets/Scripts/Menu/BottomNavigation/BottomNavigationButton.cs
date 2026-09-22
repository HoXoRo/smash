using Service;
using UnityEngine;

namespace Menu.BottomNavigation
{
	public class BottomNavigationButton : MonoBehaviour
	{
		[SerializeField]
		private PageType pageType;

		[SerializeField]
		private FlowButton button;

		private void Start()
		{
			if (button != null)
			{
				button.OnClick.RemoveListener(OnClick);
				button.OnClick.AddListener(OnClick);
			}
		}

		private void OnClick()
		{
			ServiceLocator.Get<MenuController>()?.SelectPage(pageType);
		}
	}
}
