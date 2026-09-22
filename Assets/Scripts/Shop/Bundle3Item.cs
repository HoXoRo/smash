using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Shop
{
	public class Bundle3Item : ShopItemBase
	{
		[SerializeField]
		private TextMeshProUGUI bundleNameText;

		[SerializeField]
		private RectTransform coinSlotTransform;

		[SerializeField]
		private RectTransform item1Transform;

		[SerializeField]
		private RectTransform item2Transform;

		[SerializeField]
		private List<BundleContentMapItem> contentPrefabMap;

		private readonly List<GameObject> _createdContentItems = new List<GameObject>();

		protected override void Prepare()
		{
			base.Prepare();
			PrepareContent();
		}

		private void PrepareContent()
		{
			if (bundleNameText != null)
			{
				bundleNameText.text = string.Empty;
			}

			for (int i = 0; i < _createdContentItems.Count; i++)
			{
				if (_createdContentItems[i] != null)
				{
					Object.Destroy(_createdContentItems[i]);
				}
			}
			_createdContentItems.Clear();

		}

		private RectTransform GetItemSlotTransform(int index)
		{
			return index switch
			{
				0 => item1Transform,
				1 => item2Transform,
				_ => null
			};
		}

		private void CreateContentItem(BundleContentItemType contentType, RectTransform parent, int amount)
		{
			if (parent == null || contentPrefabMap == null)
			{
				return;
			}

			BundleContentMapItem mapItem = contentPrefabMap.Find(x => x != null && x.type == contentType);
			if (mapItem?.prefab == null)
			{
				return;
			}

			GameObject contentItem = Object.Instantiate(mapItem.prefab, parent);
			_createdContentItems.Add(contentItem);

			BundleContentItem bundleContentItem = contentItem.GetComponent<BundleContentItem>();
			if (bundleContentItem != null)
			{
				bundleContentItem.SetAmount(amount);
			}
		}
	}
}
