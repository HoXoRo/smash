using TMPro;
using UnityEngine;

namespace Life
{
	public class LifeCountTracker : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI countText;

		private void OnEnable()
		{
			LifeTrackerHelper.AddLifeCountTracker(UpdateText);
		}

		private void OnDisable()
		{
			LifeTrackerHelper.RemoveLifeCountTracker(UpdateText);
		}

		private void UpdateText(int count)
		{
			countText.text = count.ToString();
		}
	}
}
