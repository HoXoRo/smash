using System.Collections.Generic;
using Gameplay;
using Gameplay.Level;
using Inventory.TimedInventory;
using Level;
using Life;
using LocalSave;
using Scene;
using Service;
using UnityEngine;

namespace Popups.PrelevelPopup
{
	public class PrelevelPopup : BasePopup
	{
		[SerializeField]
		private PrelevelPopupType type;

		[SerializeField]
		private PrelevelPopupStreakContent[] streakContents;

		[SerializeField]
		private PrelevelPopupNoStreakContent[] noStreakContents;

		private PrelevelPopupContentBase _activeStreakContent;

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			int level = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
			LevelData levelData = LevelLoader.GetLevel(level);
			int contentIndex = levelData != null ? Mathf.Max(0, (int)levelData.difficulty) : 0;
			int currentStreak = StreakHelper.GetStreakCount();
			int maxStreak = StreakHelper.MaxStreak;

			SetContentsActive(streakContents, false);
			SetContentsActive(noStreakContents, false);

			if (StreakHelper.IsUnlocked() && streakContents != null && streakContents.Length > 0)
			{
				PrelevelPopupStreakContent content = streakContents[Mathf.Clamp(contentIndex, 0, streakContents.Length - 1)];
				_activeStreakContent = content;
			}
			else if (noStreakContents != null && noStreakContents.Length > 0)
			{
				PrelevelPopupNoStreakContent content = noStreakContents[Mathf.Clamp(contentIndex, 0, noStreakContents.Length - 1)];
				_activeStreakContent = content;
			}
			else
			{
				_activeStreakContent = null;
			}

			if (_activeStreakContent != null)
			{
				_activeStreakContent.gameObject.SetActive(true);
				_activeStreakContent.Prepare(this, level, type, currentStreak, maxStreak);
			}
		}

		public void PlayClicked()
		{
			PrelevelBoosterHelper.SetSelection(_activeStreakContent != null ? _activeStreakContent.GetSelectedBoosters() : new List<PrelevelBoosterType>());
			if (type == PrelevelPopupType.TryAgain)
			{
				bool hasUnlimitedLife = TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife);
				LifeHelper lifeHelper = ServiceLocator.Get<LifeHelper>();
				if (!hasUnlimitedLife && lifeHelper != null && lifeHelper.GetCurrentLifeCount() < 1)
				{
					ServiceLocator.Get<PopupController>()?.Open(PopupType.MoreLives);
					return;
				}
			}
			SceneHandler.LoadScene(SceneType.Gameplay, false);
		}

		public void CloseClicked()
		{
			if (type == PrelevelPopupType.TryAgain)
			{
				SceneHandler.LoadScene(SceneType.Menu, false);
				return;
			}
			Close();
		}

		private static void SetContentsActive<T>(T[] contents, bool active) where T : PrelevelPopupContentBase
		{
			if (contents == null)
			{
				return;
			}
			foreach (T content in contents)
			{
				if (content != null)
				{
					content.gameObject.SetActive(active);
				}
			}
		}
	}
}
