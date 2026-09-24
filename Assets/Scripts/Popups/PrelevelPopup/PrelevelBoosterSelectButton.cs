using Gameplay;
using Inventory;
using LocalSave;
using Service;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Popups.PrelevelPopup
{
	public class PrelevelBoosterSelectButton : MonoBehaviour
	{
		private enum State
		{
			Locked = 0,
			NoneRemaining = 1,
			Unselected = 2,
			Selected = 3,
			FreeUnselected = 4
		}

		public PrelevelBoosterType type;

		[SerializeField]
		private GameObject unselectedBG;

		[SerializeField]
		private GameObject selectedBG;

		[SerializeField]
		private GameObject lockedBG;

		[SerializeField]
		private GameObject lockedFrame;

		[SerializeField]
		private TextMeshProUGUI lockedUnlockLevelText;

		public FlowButton button;

		[SerializeField]
		private TextMeshProUGUI countText;

		[SerializeField]
		private GameObject countSign;

		[SerializeField]
		private GameObject selectedSign;

		[SerializeField]
		private GameObject plusSign;

		[SerializeField]
		private GameObject freeSign;

		[SerializeField]
		private Image[] boosterIconImages;

		[SerializeField]
		private Image[] boosterIconLockedImages;

		[SerializeField]
		private Sprite[] boosterIconSprites;

		[SerializeField]
		private Sprite[] boosterIconLockedSprites;

		private State? _currentState;

		private bool _isFree;

		public void Prepare()
		{
			int unlockLevel = PrelevelBoosterHelper.GetUnlockLevelForType(type);
			int currentLevel = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
			bool unlocked = currentLevel >= unlockLevel;
			_isFree = currentLevel == unlockLevel;
			InventoryItemType itemType = PrelevelBoosterHelper.ToInventoryItemType(type);
			InventoryTrackerHelper.RemoveTracker(itemType, OnCountUpdated);

			UpdateIcons(unlocked);

			if (lockedUnlockLevelText != null)
			{
				lockedUnlockLevelText.text = string.Format("Level {0}", unlockLevel);
			}
			if (button != null)
			{
				button.OnClick.RemoveListener(OnClick);
				button.OnClick.AddListener(OnClick);
				button.Interactable = unlocked;
			}

			if (unlocked)
			{
				UpdateState(State.Unselected);
				InventoryTrackerHelper.AddTracker(itemType, OnCountUpdated);
			}
			else
			{
				UpdateState(State.Locked);
			}
		}

		private void OnClick()
		{
			if (!_currentState.HasValue)
			{
				return;
			}

			switch (_currentState.Value)
			{
			case State.Unselected:
			case State.FreeUnselected:
				UpdateState(State.Selected);
				break;
			case State.Selected:
				UpdateState(_isFree ? State.FreeUnselected : State.Unselected);
				break;
			case State.NoneRemaining:
				ServiceLocator.Get<PopupController>()?.Open(PopupType.PrelevelBoosterBuy, new Dictionary<string, object>
				{
					["boosterType"] = type
				});
				break;
			}
		}

		public bool GetSelected()
		{
			return _currentState == State.Selected;
		}

		private void UpdateState(State state)
		{
			if (_currentState == state)
			{
				return;
			}
			_currentState = state;

			SetActive(unselectedBG, state == State.Unselected || state == State.NoneRemaining || state == State.FreeUnselected);
			SetActive(selectedBG, state == State.Selected);
			SetActive(lockedBG, state == State.Locked);
			SetActive(lockedFrame, state == State.Locked);
			SetActive(countSign, state == State.Unselected);
			SetActive(selectedSign, state == State.Selected);
			SetActive(plusSign, state == State.NoneRemaining);
			SetActive(freeSign, state == State.FreeUnselected);
		}

		private void OnCountUpdated(int amount)
		{
			if (countText != null)
			{
				countText.text = amount.ToString();
			}
			if (!_currentState.HasValue || _currentState == State.Locked || _currentState == State.Selected)
			{
				return;
			}
			if (amount > 0)
			{
				UpdateState(State.Unselected);
			}
			else
			{
				UpdateState(_isFree ? State.FreeUnselected : State.NoneRemaining);
			}
		}

		private void OnDestroy()
		{
			InventoryTrackerHelper.RemoveTracker(PrelevelBoosterHelper.ToInventoryItemType(type), OnCountUpdated);
			if (button != null)
			{
				button.OnClick.RemoveListener(OnClick);
			}
		}

		private void UpdateIcons(bool unlocked)
		{
			int index = (int)type;
			Sprite normalSprite = GetSprite(boosterIconSprites, index);
			Sprite lockedSprite = GetSprite(boosterIconLockedSprites, index);

			foreach (Image image in boosterIconImages)
			{
				if (image == null)
				{
					continue;
				}
				if (normalSprite != null)
				{
					image.sprite = normalSprite;
				}
				image.gameObject.SetActive(unlocked);
			}
			foreach (Image image in boosterIconLockedImages)
			{
				if (image == null)
				{
					continue;
				}
				if (lockedSprite != null)
				{
					image.sprite = lockedSprite;
				}
				image.gameObject.SetActive(!unlocked);
			}
		}

		private static Sprite GetSprite(Sprite[] sprites, int index)
		{
			return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
		}

		private static void SetActive(GameObject target, bool active)
		{
			if (target != null)
			{
				target.SetActive(active);
			}
		}
	}
}
