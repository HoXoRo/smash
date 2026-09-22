using TMPro;
using UnityEngine;
using Util;

namespace Life
{
	public class LifeTimerTracker : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI timeText;

		private void OnEnable()
		{
			LifeTrackerHelper.AddLifeTimerTracker(UpdateText);
		}

		private void OnDisable()
		{
			LifeTrackerHelper.RemoveLifeTimerTracker(UpdateText);
		}

		private void UpdateText(int time)
		{
			timeText.text = time <= 0 ? string.Empty : FormatUtils.FormatSecondsTime(time);
		}
	}
}
