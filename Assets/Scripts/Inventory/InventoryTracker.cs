using TMPro;
using UnityEngine;

namespace Inventory
{
	public class InventoryTracker : MonoBehaviour
	{
		[SerializeField]
		private InventoryItemType trackedType;

		[SerializeField]
		private TextMeshProUGUI trackText;

		private void OnEnable()
		{
			InventoryTrackerHelper.AddTracker(trackedType, OnAmountUpdated);
		}

		private void OnDisable()
		{
			InventoryTrackerHelper.RemoveTracker(trackedType, OnAmountUpdated);
		}

		private void OnAmountUpdated(int amount)
		{
			trackText.text = amount.ToString();
		}
	}
}
