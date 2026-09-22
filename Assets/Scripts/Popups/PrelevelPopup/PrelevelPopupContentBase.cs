using System.Collections.Generic;
using Gameplay;
using UnityEngine;

namespace Popups.PrelevelPopup
{
	public abstract class PrelevelPopupContentBase : MonoBehaviour
	{
		public abstract void Prepare(PrelevelPopup popup, int level, PrelevelPopupType type, int currentStreak, int maxStreak);

		public abstract List<PrelevelBoosterType> GetSelectedBoosters();
	}
}
