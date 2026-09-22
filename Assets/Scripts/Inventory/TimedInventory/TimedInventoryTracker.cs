using TMPro;
using UnityEngine;

namespace Inventory.TimedInventory
{
	public class TimedInventoryTracker : MonoBehaviour
	{
		[SerializeField]
		private TimedInventoryItemType trackedType;

		[SerializeField]
		private TextMeshProUGUI trackText;

		private void OnEnable()
		{
			TimedInventoryTrackerHelper.AddTracker(trackedType, OnAmountUpdated);
		}

		private void OnDisable()
		{
			TimedInventoryTrackerHelper.RemoveTracker(trackedType, OnAmountUpdated);
		}

		private void OnAmountUpdated(int amount)
		{
			trackText.text = amount.ToString();
		}
	}
}
