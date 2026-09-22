using Service;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.BottomNavigation
{
	public class BottomNavigationController : ServiceMonoBehaviour
	{
		[SerializeField]
		private CanvasScaler attachedScaler;

		[SerializeField]
		private BottomNavigationSmallItem[] smallItems;

		[SerializeField]
		private BottomNavigationBigItem[] bigItems;

		[SerializeField]
		private RectTransform[] separators;

		private int _selectedIndex;

		private const float BigWidthRatio = 0.333f;

		private const float BigHeight = 304f;

		private const float SmallHeight = 227f;

		private const float SeparatorWidth = 9f;

		private const int ItemCount = 3;

		public void SetSelection(int index)
		{
			_selectedIndex = Mathf.Clamp(index, 0, ItemCount - 1);
			Fit();
			if (bigItems != null && _selectedIndex < bigItems.Length)
			{
				bigItems[_selectedIndex]?.PlaySelectAnimation();
			}
		}

		private void Fit()
		{
			ResetAll();
			RectTransform scalerTransform = attachedScaler != null ? attachedScaler.GetComponent<RectTransform>() : null;
			float totalWidth = scalerTransform != null ? scalerTransform.rect.width : 0f;
			if (totalWidth <= 0f)
			{
				totalWidth = attachedScaler != null ? attachedScaler.referenceResolution.x : 1125f;
			}
			float bigWidth = totalWidth * BigWidthRatio;
			int visibleItemCount = ItemCount - 1;
			float smallWidth = (totalWidth - bigWidth - SeparatorWidth * (visibleItemCount + 1)) / (visibleItemCount - 1);
			float cursor = (0f - totalWidth) * 0.5f;
			int separatorIndex = 0;
			for (int i = 0; i < ItemCount; i++)
			{
				if (i == (int)PageType.Shop)
				{
					continue;
				}
				PlaceSeparator(separatorIndex++, cursor);
				cursor += SeparatorWidth;
				bool selected = i == _selectedIndex;
				float itemWidth = selected ? bigWidth : smallWidth;
				BottomNavigationBigItem bigItem = bigItems != null && i < bigItems.Length ? bigItems[i] : null;
				BottomNavigationSmallItem smallItem = smallItems != null && i < smallItems.Length ? smallItems[i] : null;
				RectTransform itemTransform = selected ? bigItem?.rectTransform : smallItem?.rectTransform;
				if (itemTransform != null)
				{
					itemTransform.gameObject.SetActive(true);
					itemTransform.anchoredPosition = new Vector2(cursor + itemWidth * 0.5f, 0f);
					itemTransform.sizeDelta = new Vector2(itemWidth, selected ? BigHeight : SmallHeight);
				}
				cursor += itemWidth;
			}
			PlaceSeparator(separatorIndex, cursor);
		}

		private void ResetAll()
		{
			if (smallItems != null)
			{
				foreach (BottomNavigationSmallItem item in smallItems)
				{
					if (item != null && item.rectTransform != null)
					{
						item.rectTransform.gameObject.SetActive(false);
						item.rectTransform.sizeDelta = new Vector2(item.rectTransform.sizeDelta.x, SmallHeight);
					}
				}
			}
			if (bigItems != null)
			{
				foreach (BottomNavigationBigItem item in bigItems)
				{
					if (item != null && item.rectTransform != null)
					{
						item.rectTransform.gameObject.SetActive(false);
						item.rectTransform.sizeDelta = new Vector2(item.rectTransform.sizeDelta.x, BigHeight);
					}
				}
			}
			if (separators != null)
			{
				foreach (RectTransform separator in separators)
				{
					if (separator != null)
					{
						separator.gameObject.SetActive(false);
						separator.sizeDelta = new Vector2(SeparatorWidth, SmallHeight);
					}
				}
			}
		}

		private void PlaceSeparator(int index, float x)
		{
			if (separators == null || index >= separators.Length || separators[index] == null)
			{
				return;
			}
			RectTransform separator = separators[index];
			separator.gameObject.SetActive(true);
			separator.anchoredPosition = new Vector2(x + SeparatorWidth * 0.5f, 0f);
			separator.sizeDelta = new Vector2(SeparatorWidth, SmallHeight);
		}
	}
}
