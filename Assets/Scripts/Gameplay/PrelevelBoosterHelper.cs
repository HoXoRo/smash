using System.Collections.Generic;
using Inventory;
using LocalSave;

namespace Gameplay
{
	public static class PrelevelBoosterHelper
	{
		private static List<PrelevelBoosterType> _selectionList;

		private static Dictionary<PrelevelBoosterType, int> _unlockLevelMap;

		public static void SetSelection(List<PrelevelBoosterType> selectedTypes)
		{
			_selectionList = selectedTypes;
		}

		public static List<PrelevelBoosterType> GetSelection()
		{
			return _selectionList;
		}

		public static void ClearSelection()
		{
			_selectionList?.Clear();
		}

		public static int GetUnlockLevelForType(PrelevelBoosterType type)
		{
			return _unlockLevelMap.TryGetValue(type, out int unlockLevel) ? unlockLevel : 0;
		}

		public static bool ShouldShowPrelevelTutorial(PrelevelBoosterType type, int level)
		{
			return type == PrelevelBoosterType.Rocket && GetUnlockLevelForType(type) <= level && !SaveService.Data.TutorialProgress.tutorialPrelevelBoosterRocketDone;
		}

		public static void MarkPrelevelTutorialDone(PrelevelBoosterType type)
		{
			if (type == PrelevelBoosterType.Rocket)
			{
				SaveService.Data.TutorialProgress.tutorialPrelevelBoosterRocketDone = true;
			}
			SaveService.Save();
		}

		public static InventoryItemType ToInventoryItemType(this PrelevelBoosterType type)
		{
			return type switch
			{
				PrelevelBoosterType.Rocket => InventoryItemType.PrelevelRocket,
				_ => InventoryItemType.Coin
			};
		}

		static PrelevelBoosterHelper()
		{
			_selectionList = new List<PrelevelBoosterType>();
			_unlockLevelMap = new Dictionary<PrelevelBoosterType, int>
			{
				[PrelevelBoosterType.Rocket] = 23
			};
		}
	}
}
