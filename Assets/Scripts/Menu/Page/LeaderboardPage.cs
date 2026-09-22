using System.Collections.Generic;
using Menu.Leaderboard;
using UnityEngine;

namespace Menu.Page
{
	public class LeaderboardPage : BasePage
	{
		private enum LeaderboardMode
		{
			World = 0,
			Country = 1
		}

		[SerializeField]
		private GameObject itemPrefab;

		[SerializeField]
		private Transform contentRoot;

		[SerializeField]
		private GameObject worldTabHider;

		[SerializeField]
		private GameObject countryTabHider;

		[SerializeField]
		private FlowButton worldTabButton;

		[SerializeField]
		private FlowButton countryTabButton;

		private List<LeaderboardItem> _items;

		private LeaderboardMode? _currentMode;

		private bool _preparedOnce;

		public override void Prepare()
		{
			if (!_preparedOnce)
			{
				if (worldTabButton != null)
				{
					worldTabButton.OnClick.AddListener(delegate
					{
						SelectMode(LeaderboardMode.World);
					});
				}
				if (countryTabButton != null)
				{
					countryTabButton.OnClick.AddListener(delegate
					{
						SelectMode(LeaderboardMode.Country);
					});
				}
				_preparedOnce = true;
			}

			SelectMode(LeaderboardMode.World);
		}

		private void UpdateLeaderboard()
		{
			if (!_currentMode.HasValue)
			{
				return;
			}

			LeaderboardData data = GetMockData(_currentMode.Value);
			if (_items == null)
			{
				_items = new List<LeaderboardItem>();
			}

			for (int i = 0; i < data.data.Count; i++)
			{
				LeaderboardItem item;
				if (i >= _items.Count)
				{
					GameObject instance = Object.Instantiate(itemPrefab, contentRoot);
					item = instance.GetComponent<LeaderboardItem>();
					_items.Add(item);
				}
				else
				{
					item = _items[i];
				}
				if (item != null)
				{
					item.gameObject.SetActive(true);
					item.Prepare(data.data[i]);
				}
			}

			for (int i = data.data.Count; i < _items.Count; i++)
			{
				if (_items[i] != null)
				{
					_items[i].gameObject.SetActive(false);
				}
			}
		}

		private LeaderboardData GetMockData(LeaderboardMode mode)
		{
			string[] names = mode == LeaderboardMode.World
				? new[]
				{
					"Bizdik",
					"Emanettin",
					"Koko",
					"CapCap",
					"Patates",
					"Tofu",
					"Peynir",
					"Chito",
					"Momo",
					"Bihter",
					"Tombi",
					"Uzum",
					"Tosbaga",
					"Bizbiz",
					"Lulu",
					"Pamuk",
					"Kumpir"
				}
				: new[]
				{
					"BizdikTR",
					"EmanettinTR",
					"KokoTR",
					"CapCapTR",
					"PatatesTR",
					"TofuTR",
					"PeynirTR",
					"ChitoTR",
					"MomoTR",
					"BihterTR",
					"TombiTR",
					"UzumTR",
					"TosbagaTR",
					"BizbizTR",
					"LuluTR",
					"PamukTR",
					"KumpirTR"
				};

			LeaderboardData data = new LeaderboardData
			{
				data = new List<LeaderboardDataItem>()
			};
			for (int i = 0; i < names.Length; i++)
			{
				data.data.Add(new LeaderboardDataItem
				{
					rank = i + 1,
					username = names[i],
					level = 256
				});
			}
			return data;
		}

		private void SelectMode(LeaderboardMode mode)
		{
			if (_currentMode.HasValue && _currentMode.Value == mode)
			{
				return;
			}

			_currentMode = mode;
			if (worldTabHider != null)
			{
				worldTabHider.SetActive(mode != LeaderboardMode.World);
			}
			if (countryTabHider != null)
			{
				countryTabHider.SetActive(mode != LeaderboardMode.Country);
			}
			UpdateLeaderboard();
		}
	}
}
