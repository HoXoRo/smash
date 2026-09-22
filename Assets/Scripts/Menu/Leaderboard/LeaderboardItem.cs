using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Leaderboard
{
	public class LeaderboardItem : MonoBehaviour
	{
		[SerializeField]
		private Sprite[] badgeSprites;

		[SerializeField]
		private GameObject badgeGO;

		[SerializeField]
		private Image badgeImage;

		[SerializeField]
		private TextMeshProUGUI rankText;

		[SerializeField]
		private TextMeshProUGUI usernameText;

		[SerializeField]
		private TextMeshProUGUI levelText;

		public void Prepare(LeaderboardDataItem dataItem)
		{
			if (dataItem == null)
			{
				return;
			}
			if (rankText != null)
			{
				rankText.text = dataItem.rank.ToString();
			}
			if (usernameText != null)
			{
				usernameText.text = dataItem.username;
			}
			if (levelText != null)
			{
				levelText.text = dataItem.level.ToString();
			}
			bool hasBadge = badgeSprites != null && dataItem.rank > 0 && dataItem.rank <= badgeSprites.Length;
			if (badgeGO != null)
			{
				badgeGO.SetActive(hasBadge);
			}
			if (badgeImage != null && hasBadge)
			{
				badgeImage.sprite = badgeSprites[dataItem.rank - 1];
			}
		}
	}
}
