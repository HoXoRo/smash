using System;
using System.ComponentModel;
using System.IO;
using Gameplay;
using Gameplay.Level;
using Inventory.TimedInventory;
using Life;
using LocalSave;
using Scene;
using Segmentation;
using SRDebugger.Internal;
using SRF.Service;
using Service;
using UnityEngine;
using Util;

public partial class SROptions : INotifyPropertyChanged
{
	private static readonly SROptions _current = new SROptions();

	public static SROptions Current => _current;

	public event SROptionsPropertyChanged PropertyChanged;

	private event PropertyChangedEventHandler InterfacePropertyChangedEventHandler;

	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add
		{
			InterfacePropertyChangedEventHandler += value;
		}
		remove
		{
			InterfacePropertyChangedEventHandler -= value;
		}
	}

	[Sort(3)]
	[Category("General")]
	public string Campaign
	{
		get => SaveService.Data?.Campaign ?? string.Empty;
		set
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.Campaign = value ?? string.Empty;
			OnPropertyChanged(nameof(Campaign));
		}
	}

	[Sort(4)]
	[Category("General")]
	public UserSegment Segment
	{
		get
		{
			if (SaveService.Data == null || string.IsNullOrEmpty(SaveService.Data.Segment))
			{
				return UserSegment.Default;
			}
			return Enum.TryParse(SaveService.Data.Segment, true, out UserSegment segment) ? segment : UserSegment.Default;
		}
		set
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.Segment = value == UserSegment.Default ? string.Empty : value.ToString();
			OnPropertyChanged(nameof(Segment));
		}
	}

	[Sort(1)]
	[Category("Level")]
	public int LevelIndex
	{
		get => SaveService.Data?.Level ?? 1;
		set
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.Level = Mathf.Max(1, value);
			OnPropertyChanged(nameof(LevelIndex));
		}
	}

	[Sort(6)]
	[Category("Level")]
	public bool LoopActive
	{
		get => LevelLoader.LoopActive;
		set
		{
			LevelLoader.LoopActive = value;
			OnPropertyChanged(nameof(LoopActive));
		}
	}

	[Sort(7)]
	[Category("Level")]
	public string ForcedCollection
	{
		get => !string.IsNullOrEmpty(LevelCollectionHelper.ExternalForcedCollectionName) ? LevelCollectionHelper.ExternalForcedCollectionName : LevelCollectionHelper.ForcedCollectionName;
		set
		{
			string normalized = value ?? string.Empty;
			LevelCollectionHelper.ExternalForcedCollectionName = normalized;
			if (string.IsNullOrEmpty(normalized))
			{
				PlayerPrefs.DeleteKey("ForcedCollectionName");
			}
			else
			{
				PlayerPrefs.SetString("ForcedCollectionName", normalized);
			}
			OnPropertyChanged(nameof(ForcedCollection));
		}
	}

	[Category("Test")]
	[Sort(1)]
	public int StreakCount
	{
		get => SaveService.Data?.StreakCount ?? 0;
		set
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.StreakCount = Mathf.Max(0, value);
			OnPropertyChanged(nameof(StreakCount));
		}
	}

	[Category("Inventory")]
	public int Coin
	{
		get => SaveService.Data?.Coins ?? 0;
		set
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.Coins = Mathf.Max(0, value);
			OnPropertyChanged(nameof(Coin));
		}
	}

	[Category("Inventory")]
	public int PrelevelRocket
	{
		get => SaveService.Data?.PrelevelRocket ?? 0;
		set
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.PrelevelRocket = Mathf.Max(0, value);
			OnPropertyChanged(nameof(PrelevelRocket));
		}
	}

	[Category("General")]
	[Sort(1)]
	public void RemoveAllData()
	{
		SaveService.Delete();
		ClearPersistentData();
		CommonUtils.QuitApplication();
	}

	public static void ClearPersistentData()
	{
		string path = Application.persistentDataPath;
		if (!Directory.Exists(path))
		{
			return;
		}
		DirectoryInfo root = new DirectoryInfo(path);
		FileInfo[] files = root.GetFiles();
		for (int i = 0; i < files.Length; i++)
		{
			files[i].Delete();
		}
		DirectoryInfo[] directories = root.GetDirectories();
		for (int i = 0; i < directories.Length; i++)
		{
			directories[i].Delete(true);
		}
	}

	[Category("Level")]
	[Sort(2)]
	public void LoadLevel()
	{
		SceneHandler.LoadScene(SceneType.Gameplay, true);
	}

	[Sort(3)]
	[Category("Level")]
	public void WinStage()
	{
		ServiceLocator.Get<GameController>()?.CompleteStage();
	}

	[Category("Level")]
	[Sort(4)]
	public void Fail()
	{
		ServiceLocator.Get<GameController>()?.Fail();
	}

	[Sort(5)]
	[Category("Level")]
	public void LoadMenu()
	{
		SceneHandler.LoadScene(SceneType.Menu, true);
	}

	[Category("Level")]
	[Sort(8)]
	public void DownloadCollection()
	{
		string collectionName = !string.IsNullOrEmpty(ForcedCollection) ? ForcedCollection : LevelCollectionHelper.GetConfiguredCollection();
		LevelTestHelper.SyncLevels(collectionName);
	}

	[Category("Inventory")]
	public void RemoveLife()
	{
		ServiceLocator.Get<LifeHelper>()?.LoseLife();
	}

	[Category("Inventory")]
	public void FullLife()
	{
		ServiceLocator.Get<LifeHelper>()?.FullLife();
	}

	[Category("Inventory")]
	public void Add30mUnlimitedLife()
	{
		TimedInventoryHelper.AddTimeForTest(new TimedInventoryPayload(TimedInventoryItemType.UnlimitedLife, 30 * 60));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void OnStartup()
	{
		SRServiceManager.GetService<InternalOptionsRegistry>().AddOptionContainer(Current);
	}

	public void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, propertyName);
		InterfacePropertyChangedEventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
