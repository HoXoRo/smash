using Menu.BottomNavigation;
using Menu.Page;
using Service;
using UnityEngine;
using Util;

namespace Menu
{
	public class MenuController : ServiceMonoBehaviour
	{
		public static bool IsAfterWin;

		[SerializeField]
		private Transform homePageTransform;

		[SerializeField]
		private BasePage[] pages;

		private PageType? _currentPageType;

		private void Start()
		{
			SelectPage(PageType.Home);
			RateUsHelper.TryShowRateUs();
		}

		public void SelectPage(PageType type)
		{
			if (_currentPageType == type)
			{
				return;
			}
			_currentPageType = type;
			ServiceLocator.Get<BottomNavigationController>()?.SetSelection((int)type);
			if (pages == null)
			{
				return;
			}
			for (int i = 0; i < pages.Length; i++)
			{
				if (pages[i] == null)
				{
					continue;
				}
				if (i == (int)type)
				{
					pages[i].Prepare();
					pages[i].Show();
				}
				else
				{
					pages[i].Hide();
				}
			}
		}
	}
}
