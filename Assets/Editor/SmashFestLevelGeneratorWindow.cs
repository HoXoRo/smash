using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

using LevelData = SmashFest.Editor.EditorLevelData;
using LevelStageData = SmashFest.Editor.EditorLevelStageData;
using LevelObjectData = SmashFest.Editor.EditorLevelObjectData;
using LevelTableData = SmashFest.Editor.EditorLevelTableData;
using LevelBlockerData = SmashFest.Editor.EditorLevelBlockerData;
using LevelDifficulty = SmashFest.Editor.EditorLevelDifficulty;
using ObjectType = SmashFest.Editor.EditorObjectType;
using FloatTriplet = SmashFest.Editor.EditorFloatTriplet;
using FloatQuartet = SmashFest.Editor.EditorFloatQuartet;
using FloatTuple = SmashFest.Editor.EditorFloatTuple;
using HorizontalDirection = SmashFest.Editor.EditorHorizontalDirection;
using VerticalDirection = SmashFest.Editor.EditorVerticalDirection;

namespace SmashFest.Editor
{
	[Serializable]
	internal sealed class EditorLevelData
	{
		public int levelIndex;
		public int moveCount;
		public EditorLevelDifficulty difficulty;
		public List<EditorLevelStageData> stages;
	}

	[Serializable]
	internal sealed class EditorLevelStageData
	{
		public List<EditorLevelObjectData> objects;
		public List<EditorLevelTableData> tables;
		public List<EditorLevelBlockerData> blockers;
	}

	[Serializable]
	internal sealed class EditorLevelObjectData
	{
		public int tableId;
		public EditorObjectType type;
		public EditorFloatTriplet size;
		public EditorFloatTriplet pos;
		public EditorFloatQuartet rot;

		[JsonExtensionData]
		public IDictionary<string, JToken> extra;
	}

	[Serializable]
	internal sealed class EditorLevelTableData
	{
		public int id;
		public EditorFloatTriplet pos;
		public EditorFloatQuartet rot;
		public EditorFloatTriplet scl;
		public EditorFloatTriplet dim;
		public bool doRot;
		public float rotSpd;
		public bool movH;
		public float movHMin;
		public float movHMax;
		public EditorHorizontalDirection dirH;
		public float movSpdH;
		public bool movV;
		public float movVMin;
		public float movVMax;
		public EditorVerticalDirection dirV;
		public float movSpdV;
	}

	[Serializable]
	internal sealed class EditorLevelBlockerData
	{
		public float width = 1f;
		public float initZ;
		public float startNormalized;
		public EditorFloatTuple minPos;
		public EditorFloatTuple maxPos;
		public float posHalfCycleDuration;
		public float initRotEuler;
		public float rotInSecEuler;
	}

	[Serializable]
	internal struct EditorFloatTriplet
	{
		public float x;
		public float y;
		public float z;

		public EditorFloatTriplet(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	[Serializable]
	internal struct EditorFloatQuartet
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public EditorFloatQuartet(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}

	[Serializable]
	internal struct EditorFloatTuple
	{
		public float x;
		public float y;

		public EditorFloatTuple(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
	}

	internal enum EditorLevelDifficulty
	{
		Normal = 0,
		Hard = 1,
		SuperHard = 2
	}

	internal enum EditorObjectType
	{
		CanCylinder = 1,
		CanCone = 2,
		CanSquare = 3,
		JamJarBlue = 11,
		JamJarPink = 12,
		JamJarYellow = 13,
		JamJarRed = 14,
		JamJarOrange = 15,
		JamJarPurple = 16,
		StoneSquare = 21,
		StoneCylinder = 22,
		Wormhole = 31,
		BoxSquare = 41,
		BoxCylinder = 42,
		Tnt = 51,
		IceSquare = 61,
		IceCylinder = 62,
		Disc = 71
	}

	internal enum EditorHorizontalDirection
	{
		Left = 0,
		Right = 1
	}

	internal enum EditorVerticalDirection
	{
		Up = 0,
		Down = 1
	}

	public sealed class SmashFestLevelGeneratorWindow : EditorWindow
	{
		private const string DefaultCollection = "prod-14";
		private const string MenuPath = "SmashFest/Level Generator";

		private string _collection = DefaultCollection;
		private int _levelIndex = 1;
		private LevelData _levelData;
		private Vector2 _scroll;
		private int _selectedStage;
		private bool _showObjects = true;
		private bool _showTables = true;
		private bool _showBlockers;
		private int _gridColumns = 3;
		private int _gridRows = 3;
		private int _gridLayers = 1;
		private float _gridSpacing = 1f;
		private float _gridStartY = 0.5f;
		private float _gridLayerSpacing = 1f;
		private ObjectType _gridObjectType = ObjectType.CanSquare;
		private bool _gridReplaceObjects;
		private string _status = "Load a level to begin.";
		private MessageType _statusType = MessageType.Info;

		[MenuItem(MenuPath)]
		private static void Open()
		{
			SmashFestLevelGeneratorWindow window = GetWindow<SmashFestLevelGeneratorWindow>();
			window.titleContent = new GUIContent("Level Generator");
			window.minSize = new Vector2(760f, 560f);
			window.Show();
		}

		private void OnEnable()
		{
			if (_levelData == null)
			{
				_levelIndex = Mathf.Max(1, _levelIndex);
				LoadLevel();
			}
		}

		private void OnGUI()
		{
			DrawHeader();
			if (_levelData == null)
			{
				EditorGUILayout.HelpBox(_status, _statusType);
				DrawNewLevelControls();
				return;
			}

			DrawSummary();
			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			DrawLevelSettings();
			DrawStageList();
			DrawGenerator();
			EditorGUILayout.EndScrollView();
		}

		private void DrawHeader()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.BeginHorizontal();
			_collection = EditorGUILayout.TextField("Collection", _collection);
			_levelIndex = EditorGUILayout.IntField("Level", _levelIndex, GUILayout.Width(170f));
			if (GUILayout.Button("Load", GUILayout.Width(70f)))
			{
				LoadLevel();
			}
			if (GUILayout.Button("New", GUILayout.Width(70f)))
			{
				CreateNewLevel();
			}
			if (GUILayout.Button("Save", GUILayout.Width(70f)))
			{
				SaveLevel();
			}
			EditorGUILayout.EndHorizontal();

			string path = GetLevelPath(_collection, _levelData != null ? _levelData.levelIndex : _levelIndex);
			EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
		}

		private void DrawSummary()
		{
			int objectCount = GetObjectCount();
			int tableCount = GetTableCount();
			int blockerCount = GetBlockerCount();
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			GUILayout.Label(string.Format("Stages: {0}", _levelData.stages?.Count ?? 0));
			GUILayout.Label(string.Format("Objects: {0}", objectCount));
			GUILayout.Label(string.Format("Tables: {0}", tableCount));
			GUILayout.Label(string.Format("Blockers: {0}", blockerCount));
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Validate", GUILayout.Width(80f)))
			{
				ValidateLevel(true);
			}
			EditorGUILayout.EndHorizontal();
			if (!string.IsNullOrEmpty(_status))
			{
				EditorGUILayout.HelpBox(_status, _statusType);
			}
		}

		private void DrawNewLevelControls()
		{
			if (GUILayout.Button("Create Blank Level", GUILayout.Height(28f)))
			{
				CreateNewLevel();
			}
		}

		private void DrawLevelSettings()
		{
			EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
			_levelData.levelIndex = Mathf.Max(1, EditorGUILayout.IntField("Level Index", _levelData.levelIndex));
			_levelData.moveCount = Mathf.Max(0, EditorGUILayout.IntField("Move Count", _levelData.moveCount));
			_levelData.difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Difficulty", _levelData.difficulty);
			EditorGUILayout.Space(4f);
		}

		private void DrawStageList()
		{
			EnsureCollections();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Stages", EditorStyles.boldLabel);
			if (GUILayout.Button("Add Stage", GUILayout.Width(90f)))
			{
				_levelData.stages.Add(CreateStage());
				_selectedStage = _levelData.stages.Count - 1;
			}
			EditorGUILayout.EndHorizontal();

			if (_levelData.stages.Count == 0)
			{
				EditorGUILayout.HelpBox("The level has no stages.", MessageType.Warning);
				return;
			}

			_selectedStage = Mathf.Clamp(_selectedStage, 0, _levelData.stages.Count - 1);
			string[] stageNames = Enumerable.Range(0, _levelData.stages.Count)
				.Select(index => string.Format("Stage {0} ({1} objects)", index + 1, _levelData.stages[index]?.objects?.Count ?? 0))
				.ToArray();
			_selectedStage = EditorGUILayout.Popup("Editing", _selectedStage, stageNames);

			LevelStageData stage = _levelData.stages[_selectedStage];
			if (stage == null)
			{
				stage = CreateStage();
				_levelData.stages[_selectedStage] = stage;
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Duplicate Stage"))
			{
				_levelData.stages.Insert(_selectedStage + 1, CloneStage(stage));
				_selectedStage++;
			}
			GUI.enabled = _levelData.stages.Count > 1;
			if (GUILayout.Button("Remove Stage"))
			{
				if (EditorUtility.DisplayDialog("Remove Stage", "Remove the selected stage?", "Remove", "Cancel"))
				{
					_levelData.stages.RemoveAt(_selectedStage);
					_selectedStage = Mathf.Clamp(_selectedStage, 0, _levelData.stages.Count - 1);
				}
			}
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();

			DrawObjects(stage);
			DrawTables(stage);
			DrawBlockers(stage);
		}

		private void DrawObjects(LevelStageData stage)
		{
			_showObjects = EditorGUILayout.Foldout(_showObjects, string.Format("Objects ({0})", stage.objects.Count), true);
			if (!_showObjects)
			{
				return;
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Object", GUILayout.Width(100f)))
			{
				stage.objects.Add(CreateObject());
			}
			if (GUILayout.Button("Sort By Position", GUILayout.Width(120f)))
			{
				stage.objects = stage.objects.OrderBy(item => item.pos.z).ThenBy(item => item.pos.y).ThenBy(item => item.pos.x).ToList();
			}
			EditorGUILayout.EndHorizontal();

			for (int i = 0; i < stage.objects.Count; i++)
			{
				LevelObjectData item = stage.objects[i] ?? CreateObject();
				stage.objects[i] = item;
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(string.Format("Object {0}", i + 1), EditorStyles.boldLabel);
				if (GUILayout.Button("Duplicate", GUILayout.Width(78f)))
				{
					stage.objects.Insert(i + 1, CloneObject(item));
					i++;
				}
			if (GUILayout.Button("Remove", GUILayout.Width(64f)))
				{
					stage.objects.RemoveAt(i);
					i--;
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
					continue;
				}
				EditorGUILayout.EndHorizontal();
				item.type = (ObjectType)EditorGUILayout.EnumPopup("Type", item.type);
				item.tableId = EditorGUILayout.IntField("Table Id", item.tableId);
				item.size = DrawTriplet("Size", item.size);
				item.pos = DrawTriplet("Position", item.pos);
				item.rot = DrawQuartet("Rotation Quaternion", item.rot);
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawTables(LevelStageData stage)
		{
			_showTables = EditorGUILayout.Foldout(_showTables, string.Format("Tables ({0})", stage.tables.Count), true);
			if (!_showTables)
			{
				return;
			}

			if (GUILayout.Button("Add Table", GUILayout.Width(100f)))
			{
				stage.tables.Add(CreateTable(stage.tables.Count));
			}
			for (int i = 0; i < stage.tables.Count; i++)
			{
				LevelTableData table = stage.tables[i] ?? CreateTable(i);
				stage.tables[i] = table;
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(string.Format("Table {0}", i + 1), EditorStyles.boldLabel);
				if (GUILayout.Button("Remove", GUILayout.Width(64f)))
				{
					stage.tables.RemoveAt(i--);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
					continue;
				}
				EditorGUILayout.EndHorizontal();
				table.id = EditorGUILayout.IntField("Id", table.id);
				table.pos = DrawTriplet("Position", table.pos);
				table.rot = DrawQuartet("Rotation Quaternion", table.rot);
				table.scl = DrawTriplet("Scale", table.scl);
				table.dim = DrawTriplet("Dimensions", table.dim);
				table.doRot = EditorGUILayout.Toggle("Rotate", table.doRot);
				if (table.doRot)
				{
					table.rotSpd = EditorGUILayout.FloatField("Rotation Speed", table.rotSpd);
				}
				table.movH = EditorGUILayout.Toggle("Move Horizontally", table.movH);
				if (table.movH)
				{
					table.movHMin = EditorGUILayout.FloatField("Horizontal Min", table.movHMin);
					table.movHMax = EditorGUILayout.FloatField("Horizontal Max", table.movHMax);
					table.dirH = (HorizontalDirection)EditorGUILayout.EnumPopup("Horizontal Direction", table.dirH);
					table.movSpdH = EditorGUILayout.FloatField("Horizontal Speed", table.movSpdH);
				}
				table.movV = EditorGUILayout.Toggle("Move Vertically", table.movV);
				if (table.movV)
				{
					table.movVMin = EditorGUILayout.FloatField("Vertical Min", table.movVMin);
					table.movVMax = EditorGUILayout.FloatField("Vertical Max", table.movVMax);
					table.dirV = (VerticalDirection)EditorGUILayout.EnumPopup("Vertical Direction", table.dirV);
					table.movSpdV = EditorGUILayout.FloatField("Vertical Speed", table.movSpdV);
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawBlockers(LevelStageData stage)
		{
			_showBlockers = EditorGUILayout.Foldout(_showBlockers, string.Format("Blockers ({0})", stage.blockers.Count), true);
			if (!_showBlockers)
			{
				return;
			}

			if (GUILayout.Button("Add Blocker", GUILayout.Width(100f)))
			{
				stage.blockers.Add(new LevelBlockerData());
			}
			for (int i = 0; i < stage.blockers.Count; i++)
			{
				LevelBlockerData blocker = stage.blockers[i] ?? new LevelBlockerData();
				stage.blockers[i] = blocker;
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(string.Format("Blocker {0}", i + 1), EditorStyles.boldLabel);
				if (GUILayout.Button("Remove", GUILayout.Width(64f)))
				{
					stage.blockers.RemoveAt(i--);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
					continue;
				}
				EditorGUILayout.EndHorizontal();
				blocker.width = EditorGUILayout.FloatField("Width", blocker.width);
				blocker.startNormalized = EditorGUILayout.Slider("Start Normalized", blocker.startNormalized, 0f, 1f);
				blocker.minPos = DrawTuple("Min Position", blocker.minPos);
				blocker.maxPos = DrawTuple("Max Position", blocker.maxPos);
				blocker.posHalfCycleDuration = EditorGUILayout.FloatField("Position Half Cycle", blocker.posHalfCycleDuration);
				blocker.initZ = EditorGUILayout.FloatField("Initial Z", blocker.initZ);
				blocker.initRotEuler = EditorGUILayout.FloatField("Initial Rotation", blocker.initRotEuler);
				blocker.rotInSecEuler = EditorGUILayout.FloatField("Rotation Per Second", blocker.rotInSecEuler);
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawGenerator()
		{
			EditorGUILayout.Space(8f);
			EditorGUILayout.LabelField("Grid Generator", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			_gridColumns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _gridColumns));
			_gridRows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _gridRows));
			_gridLayers = Mathf.Max(1, EditorGUILayout.IntField("Layers", _gridLayers));
			_gridSpacing = Mathf.Max(0.01f, EditorGUILayout.FloatField("Cell Spacing", _gridSpacing));
			_gridLayerSpacing = EditorGUILayout.FloatField("Layer Z Spacing", _gridLayerSpacing);
			_gridStartY = EditorGUILayout.FloatField("Start Y", _gridStartY);
			_gridObjectType = (ObjectType)EditorGUILayout.EnumPopup("Object Type", _gridObjectType);
			_gridReplaceObjects = EditorGUILayout.Toggle("Replace Current Objects", _gridReplaceObjects);
			EditorGUILayout.HelpBox("Creates one object per grid cell. X and Y are centered around zero; layers are placed along Z.", MessageType.None);
			if (GUILayout.Button("Generate Into Selected Stage", GUILayout.Height(26f)))
			{
				GenerateGrid();
			}
			EditorGUILayout.EndVertical();
		}

		private void LoadLevel()
		{
			_levelIndex = Mathf.Max(1, _levelIndex);
			string path = GetLevelPath(_collection, _levelIndex);
			if (!File.Exists(path))
			{
				_levelData = null;
				SetStatus("Level file not found: " + path, MessageType.Warning);
				return;
			}

			try
			{
				_levelData = JsonConvert.DeserializeObject<LevelData>(File.ReadAllText(path));
				if (_levelData == null)
				{
					throw new InvalidDataException("The JSON deserialized to null.");
				}
				_levelIndex = _levelData.levelIndex;
				EnsureCollections();
				SetStatus(string.Format("Loaded Level {0}.", _levelData.levelIndex), MessageType.Info);
			}
			catch (Exception exception)
			{
				_levelData = null;
				SetStatus("Load failed: " + exception.Message, MessageType.Error);
			}
		}

		private void CreateNewLevel()
		{
			_levelIndex = Mathf.Max(1, _levelIndex);
			_levelData = new LevelData
			{
				levelIndex = _levelIndex,
				moveCount = 20,
				difficulty = LevelDifficulty.Normal,
				stages = new List<LevelStageData> { CreateStage() }
			};
			_selectedStage = 0;
			SetStatus("Created a blank level. Save to write the JSON file.", MessageType.Info);
		}

		private void SaveLevel()
		{
			if (_levelData == null)
			{
				SetStatus("Nothing to save.", MessageType.Warning);
				return;
			}
			if (!ValidateLevel(false))
			{
				return;
			}

			_levelIndex = _levelData.levelIndex;
			string path = GetLevelPath(_collection, _levelData.levelIndex);
			if (File.Exists(path) && !EditorUtility.DisplayDialog("Overwrite Level", "Overwrite " + path + "?", "Overwrite", "Cancel"))
			{
				return;
			}

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				JsonSerializerSettings settings = new JsonSerializerSettings
				{
					Formatting = Formatting.Indented,
					Culture = System.Globalization.CultureInfo.InvariantCulture,
					NullValueHandling = NullValueHandling.Include
				};
				File.WriteAllText(path, JsonConvert.SerializeObject(_levelData, settings));
				AssetDatabase.Refresh();
				SetStatus("Saved " + path, MessageType.Info);
			}
			catch (Exception exception)
			{
				SetStatus("Save failed: " + exception.Message, MessageType.Error);
			}
		}

		private bool ValidateLevel(bool showSuccess)
		{
			List<string> errors = new List<string>();
			EnsureCollections();
			if (_levelData.levelIndex < 1)
			{
				errors.Add("Level Index must be at least 1.");
			}
			for (int stageIndex = 0; stageIndex < _levelData.stages.Count; stageIndex++)
			{
				LevelStageData stage = _levelData.stages[stageIndex];
				if (stage == null)
				{
					errors.Add(string.Format("Stage {0} is null.", stageIndex + 1));
					continue;
				}
				HashSet<int> tableIds = new HashSet<int>();
				foreach (LevelTableData table in stage.tables)
				{
					if (table == null || !tableIds.Add(table.id))
					{
						errors.Add(string.Format("Stage {0} contains a null or duplicate table id.", stageIndex + 1));
					}
				}
				foreach (LevelObjectData item in stage.objects)
				{
					if (item == null)
					{
						errors.Add(string.Format("Stage {0} contains a null object.", stageIndex + 1));
					}
					else if (!tableIds.Contains(item.tableId))
					{
						errors.Add(string.Format("Stage {0} object references missing table id {1}.", stageIndex + 1, item.tableId));
					}
				}
			}

			if (errors.Count > 0)
			{
				SetStatus(string.Join("\n", errors.Take(8)), MessageType.Error);
				return false;
			}
			if (showSuccess)
			{
				SetStatus("Validation passed.", MessageType.Info);
			}
			return true;
		}

		private void GenerateGrid()
		{
			EnsureCollections();
			LevelStageData stage = _levelData.stages[Mathf.Clamp(_selectedStage, 0, _levelData.stages.Count - 1)];
			if (_gridReplaceObjects)
			{
				stage.objects.Clear();
			}

			int tableId = stage.tables.Count > 0 ? stage.tables[0].id : 0;
			if (stage.tables.Count == 0)
			{
				stage.tables.Add(CreateTable(tableId));
			}

			for (int layer = 0; layer < _gridLayers; layer++)
			{
				for (int row = 0; row < _gridRows; row++)
				{
					for (int column = 0; column < _gridColumns; column++)
					{
						float x = (column - (_gridColumns - 1) * 0.5f) * _gridSpacing;
						float y = _gridStartY + row * _gridSpacing;
						float z = layer * _gridLayerSpacing;
						stage.objects.Add(new LevelObjectData
						{
							tableId = tableId,
							type = _gridObjectType,
							size = new FloatTriplet(1f, 1f, 1f),
							pos = new FloatTriplet(x, y, z),
							rot = new FloatQuartet(0f, 0f, 0f, 1f)
						});
					}
				}
			}
			SetStatus(string.Format("Generated {0} objects in Stage {1}.", _gridColumns * _gridRows * _gridLayers, _selectedStage + 1), MessageType.Info);
		}

		private void EnsureCollections()
		{
			if (_levelData.stages == null)
			{
				_levelData.stages = new List<LevelStageData>();
			}
			for (int i = 0; i < _levelData.stages.Count; i++)
			{
				if (_levelData.stages[i] == null)
				{
					_levelData.stages[i] = CreateStage();
				}
				_levelData.stages[i].objects ??= new List<LevelObjectData>();
				_levelData.stages[i].tables ??= new List<LevelTableData>();
				_levelData.stages[i].blockers ??= new List<LevelBlockerData>();
			}
		}

		private int GetObjectCount() => _levelData.stages?.Where(stage => stage != null).Sum(stage => stage.objects?.Count ?? 0) ?? 0;
		private int GetTableCount() => _levelData.stages?.Where(stage => stage != null).Sum(stage => stage.tables?.Count ?? 0) ?? 0;
		private int GetBlockerCount() => _levelData.stages?.Where(stage => stage != null).Sum(stage => stage.blockers?.Count ?? 0) ?? 0;

		private static LevelStageData CreateStage()
		{
			return new LevelStageData
			{
				objects = new List<LevelObjectData>(),
				tables = new List<LevelTableData> { CreateTable(0) },
				blockers = new List<LevelBlockerData>()
			};
		}

		private static LevelObjectData CreateObject()
		{
			return new LevelObjectData
			{
				tableId = 0,
				type = ObjectType.CanSquare,
				size = new FloatTriplet(1f, 1f, 1f),
				pos = new FloatTriplet(0f, 0.5f, 0f),
				rot = new FloatQuartet(0f, 0f, 0f, 1f)
			};
		}

		private static LevelTableData CreateTable(int id)
		{
			return new LevelTableData
			{
				id = id,
				pos = new FloatTriplet(0f, 2.9f, 0f),
				rot = new FloatQuartet(0f, 0f, 0f, 1f),
				scl = new FloatTriplet(1f, 1f, 1f),
				dim = new FloatTriplet(3f, 2f, -0.5f)
			};
		}

		private static LevelStageData CloneStage(LevelStageData source)
		{
			return new LevelStageData
			{
				objects = source.objects.Select(CloneObject).ToList(),
				tables = source.tables.Select(CloneTable).ToList(),
				blockers = source.blockers.Select(CloneBlocker).ToList()
			};
		}

		private static LevelObjectData CloneObject(LevelObjectData source)
		{
			return new LevelObjectData
			{
				tableId = source.tableId,
				type = source.type,
				size = source.size,
				pos = source.pos,
				rot = source.rot,
				extra = source.extra == null ? null : new Dictionary<string, Newtonsoft.Json.Linq.JToken>(source.extra)
			};
		}

		private static LevelTableData CloneTable(LevelTableData source)
		{
			return new LevelTableData
			{
				id = source.id,
				pos = source.pos,
				rot = source.rot,
				scl = source.scl,
				dim = source.dim,
				doRot = source.doRot,
				rotSpd = source.rotSpd,
				movH = source.movH,
				movHMin = source.movHMin,
				movHMax = source.movHMax,
				dirH = source.dirH,
				movSpdH = source.movSpdH,
				movV = source.movV,
				movVMin = source.movVMin,
				movVMax = source.movVMax,
				dirV = source.dirV,
				movSpdV = source.movSpdV
			};
		}

		private static LevelBlockerData CloneBlocker(LevelBlockerData source)
		{
			return new LevelBlockerData
			{
				width = source.width,
				initZ = source.initZ,
				startNormalized = source.startNormalized,
				minPos = source.minPos,
				maxPos = source.maxPos,
				posHalfCycleDuration = source.posHalfCycleDuration,
				initRotEuler = source.initRotEuler,
				rotInSecEuler = source.rotInSecEuler
			};
		}

		private static FloatTriplet DrawTriplet(string label, FloatTriplet value)
		{
			Vector3 vector = EditorGUILayout.Vector3Field(label, new Vector3(value.x, value.y, value.z));
			return new FloatTriplet(vector.x, vector.y, vector.z);
		}

		private static FloatQuartet DrawQuartet(string label, FloatQuartet value)
		{
			Vector4 vector = EditorGUILayout.Vector4Field(label, new Vector4(value.x, value.y, value.z, value.w));
			return new FloatQuartet(vector.x, vector.y, vector.z, vector.w);
		}

		private static FloatTuple DrawTuple(string label, FloatTuple value)
		{
			Vector2 vector = EditorGUILayout.Vector2Field(label, new Vector2(value.x, value.y));
			return new FloatTuple(vector.x, vector.y);
		}

		private static string GetLevelPath(string collection, int levelIndex)
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			return Path.Combine(projectRoot, "Assets", "ArrowGame", "levels", collection ?? string.Empty, "Level" + levelIndex + ".json");
		}

		private void SetStatus(string message, MessageType type)
		{
			_status = message;
			_statusType = type;
			Repaint();
		}
	}
}
