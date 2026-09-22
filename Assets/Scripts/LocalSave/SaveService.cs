using System.Collections;
using System.IO;
using System.Text;
using Core;
using Newtonsoft.Json;
using UnityEngine;

namespace LocalSave
{
	public static class SaveService
	{
		private static readonly string SavePath;

		private static bool _dirty;

		private static bool _suppressDirty;

		private static Coroutine _frameEndCoroutine;

		private static SaveHelper _helper;

		private const float AutoSaveInterval = 1f;

		public static SaveData Data { get; private set; }

		static SaveService()
		{
			SavePath = Path.Combine(Application.persistentDataPath, "save.json");
			LoadOrCreate();
			CreateHelper();
			StartAutoSave();
		}

		private static void CreateHelper()
		{
			if (_helper != null)
			{
				return;
			}
			_helper = Object.FindObjectOfType<SaveHelper>();
			if (_helper == null)
			{
				GameObject gameObject = new GameObject("SaveHelper");
				Object.DontDestroyOnLoad(gameObject);
				_helper = gameObject.AddComponent<SaveHelper>();
			}
		}

		private static void StartAutoSave()
		{
			_helper?.StartAutoSave(AutoSaveInterval);
		}

		public static void HandleChange(SaveBehaviour saveBehaviour)
		{
			if (_suppressDirty)
			{
				return;
			}
			_dirty = true;
			if (saveBehaviour == SaveBehaviour.SaveNow)
			{
				Save();
			}
			else if (saveBehaviour == SaveBehaviour.SaveAtFrameEnd)
			{
				TryScheduleFrameEndSave();
			}
		}

		private static void TryScheduleFrameEndSave()
		{
			if (_helper == null || _frameEndCoroutine != null)
			{
				return;
			}
			_frameEndCoroutine = _helper.StartCoroutine(FrameEndSaveRoutine());
		}

		private static void LoadOrCreate()
		{
			_suppressDirty = true;
			try
			{
				if (File.Exists(SavePath))
				{
					string json = File.ReadAllText(SavePath, Encoding.UTF8);
					Data = JsonConvert.DeserializeObject<SaveData>(json) ?? new SaveData();
					_dirty = false;
					return;
				}
				Data = new SaveData();
				_dirty = true;
				Save();
			}
			finally
			{
				_suppressDirty = false;
			}
		}

		public static void Save()
		{
			if (!_dirty || Data == null)
			{
				return;
			}
			string directory = Path.GetDirectoryName(SavePath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}
			_dirty = false;
			File.WriteAllText(SavePath, JsonConvert.SerializeObject(Data, Formatting.None), Encoding.UTF8);
			ClearRoutines();
		}

		public static void Delete()
		{
			if (File.Exists(SavePath))
			{
				File.Delete(SavePath);
			}
			Data = new SaveData();
			_dirty = true;
			Save();
		}

		private static void ClearRoutines()
		{
			if (_frameEndCoroutine != null && _helper != null)
			{
				_helper.StopCoroutine(_frameEndCoroutine);
				_frameEndCoroutine = null;
			}
		}

		private static IEnumerator FrameEndSaveRoutine()
		{
			yield return new WaitForEndOfFrame();
			_frameEndCoroutine = null;
			Save();
		}
	}
}
