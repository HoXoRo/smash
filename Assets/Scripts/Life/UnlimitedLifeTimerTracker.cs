using TMPro;
using UnityEngine;
using Inventory.TimedInventory;
using Util;

namespace Life
{
	public class UnlimitedLifeTimerTracker : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI timeText;

		private void OnEnable()
		{
			TimedInventoryTrackerHelper.AddTracker(TimedInventoryItemType.UnlimitedLife, UpdateText);
		}

		private void OnDisable()
		{
			TimedInventoryTrackerHelper.RemoveTracker(TimedInventoryItemType.UnlimitedLife, UpdateText);
		}

		private void UpdateText(int time)
		{
			timeText.text = FormatUtils.FormatSecondsTime(time < 0 ? 0 : time);
		}
	}
}
