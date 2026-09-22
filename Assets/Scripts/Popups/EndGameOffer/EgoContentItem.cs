using Gameplay;
using Service;
using UnityEngine;

namespace Popups.EndGameOffer
{
	public class EgoContentItem : MonoBehaviour
	{
		[SerializeField]
		private EgoContentType contentType;

		public bool ShouldShow()
		{
			if (contentType == EgoContentType.EndGameOffer)
			{
				return true;
			}
			if (contentType != EgoContentType.StreakWarning)
			{
				return false;
			}
			return (ServiceLocator.Get<GameController>()?.GetActiveStreakCount() ?? 0) > 0;
		}
	}
}
