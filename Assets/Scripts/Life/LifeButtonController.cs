using UnityEngine;
using Inventory.TimedInventory;
using Popups;
using Service;

namespace Life
{
	public class LifeButtonController : MonoBehaviour
	{
		[SerializeField]
		private FlowButton button;

		[SerializeField]
		private GameObject plusGO;

		[SerializeField]
		private GameObject normalLifeIcon;

		[SerializeField]
		private GameObject unlimitedLifeIcon;

		[SerializeField]
		private GameObject normalLifeCounter;

		[SerializeField]
		private GameObject unlimitedLifeCounter;

		private bool _hasUnlimitedLife;

		private void OnEnable()
		{
			RefreshUnlimitedLifeState(true);
			LifeTrackerHelper.AddLifeCountTracker(UpdateButton);
			button.OnClick.RemoveListener(OnClick);
			button.OnClick.AddListener(OnClick);
		}

		private void OnDisable()
		{
			LifeTrackerHelper.RemoveLifeCountTracker(UpdateButton);
			button.OnClick.RemoveListener(OnClick);
		}

		private void Update()
		{
			RefreshUnlimitedLifeState();
		}

		private void UpdateButton(int count)
		{
			button.Interactable = true;
			plusGO.SetActive(!_hasUnlimitedLife && count < LifeHelper.MaxLifeCount);
		}

		private void RefreshUnlimitedLifeState(bool force = false)
		{
			bool hasUnlimitedLife = TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife);
			if (!force && _hasUnlimitedLife == hasUnlimitedLife)
			{
				return;
			}
			_hasUnlimitedLife = hasUnlimitedLife;
			normalLifeIcon.SetActive(!hasUnlimitedLife);
			normalLifeCounter.SetActive(!hasUnlimitedLife);
			unlimitedLifeIcon.SetActive(hasUnlimitedLife);
			unlimitedLifeCounter.SetActive(hasUnlimitedLife);
			if (!force)
			{
				LifeTrackerHelper.TriggerLifeCountTrackers();
			}
		}

		private void OnClick()
		{
			ServiceLocator.Get<PopupController>()?.Open(PopupType.MoreLives);
		}
	}
}
