using System.ComponentModel;
using Inventory;
using Menu.BottomNavigation;
using Menu.Page;
using SRDebugger;
using Service;
using UI;
using UnityEngine;
using Util;

namespace Menu
{
	public class MenuController : ServiceMonoBehaviour
	{
		[SROption]
		[Category("ZDev")]
		public static bool IsAfterWin;

		public static int CoinToCollect;

		[SerializeField]
		private Transform homePageTransform;

		[SerializeField]
		private BasePage[] pages;

		[SerializeField]
		private CoinCollectTarget coinPanelCollectTarget;

		private PageType _currentPageType;

		private void Start()
		{
			PlayCoinCollectIfNeeded();
			SelectPage(PageType.Home);
			RateUsHelper.TryShowRateUs();
		}

		private void PlayCoinCollectIfNeeded()
		{
			if (CoinToCollect < 1 || coinPanelCollectTarget == null)
			{
				return;
			}
			int remainingAmount = CoinToCollect;
			int remainingHits = 10;
			int shownAmount = CoinToCollect;
			CoinToCollect = 0;
			coinPanelCollectTarget.Play(10, 1f, shownAmount, delegate
			{
				if (remainingAmount <= 0 || remainingHits <= 0)
				{
					return;
				}
				int amount = Mathf.CeilToInt((float)remainingAmount / remainingHits);
				remainingAmount -= amount;
				remainingHits--;
				InventoryHelper.ReduceDelayedAmount(new InventoryPayload(InventoryItemType.Coin, amount));
			}, null);
		}

		public void SelectPage(PageType type)
		{
			if (type == PageType.Shop)
			{
				return;
			}
			if (_currentPageType == type)
			{
				return;
			}
			_currentPageType = type;
			ServiceLocator.Get<BottomNavigationController>()?.SetSelection((int)_currentPageType);
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
				if (i == (int)_currentPageType)
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
