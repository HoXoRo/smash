using System;
using System.Collections.Generic;
using System.Linq;
using Level;
using LocalSave;
using UnityEngine;

namespace Gameplay
{
	public class GameplayAnalyticsHelper
	{
		private int _levelIndex;

		private LevelData _levelData;

		private float _levelStartTime;

		private string _tryId;

		private int _egoBoughtCount;

		private int _extraMovesBought;

		private int _egoShowCount;

		private string _collectionName;

		private int _collectionVersion;

		private int _stageCount;

		private List<int> _prelevelBoostersUsed;

		public void LevelStart(int levelIndex, LevelData levelData, List<bool> prelevelBoostersUsed, int remainingObjectCount, float remainingMass, string collectionName, int collectionVersion)
		{
			_levelIndex = levelIndex;
			_levelData = levelData;
			_levelStartTime = Time.time;
			_tryId = Guid.NewGuid().ToString();
			_collectionName = collectionName;
			_collectionVersion = collectionVersion;
			_stageCount = levelData != null ? levelData.GetStageCount() : 0;
			_prelevelBoostersUsed = prelevelBoostersUsed != null ? prelevelBoostersUsed.Select(x => x ? 1 : 0).ToList() : new List<int>();
			_egoBoughtCount = 0;
			_extraMovesBought = 0;
			_egoShowCount = 0;
			IncrementTryCount();

			_ = remainingObjectCount;
			_ = remainingMass;
		}

		public void LevelEgoShow(int remainingObjectCount, float remainingMass, int stageIndex)
		{
			_egoShowCount++;
			_ = remainingObjectCount;
			_ = remainingMass;
			_ = stageIndex;
		}

		public void LevelFail(int remainingObjectCount, float remainingMass, int remainingMoveCount, FailReason reason, int stageIndex)
		{
			_ = remainingObjectCount;
			_ = remainingMass;
			_ = remainingMoveCount;
			_ = GetFailReasonString(reason);
			_ = stageIndex;
		}

		public void LevelWin(int remainingMoveCount, int stageIndex)
		{
			_ = remainingMoveCount;
			_ = stageIndex;
			ResetTryCount();
		}

		public void OnEgoBought(int moves)
		{
			_egoBoughtCount++;
			_extraMovesBought += moves;
		}

		private void IncrementTryCount()
		{
			if (SaveService.Data != null)
			{
				SaveService.Data.TryCount++;
				SaveService.Save();
			}
		}

		private void ResetTryCount()
		{
			if (SaveService.Data != null)
			{
				SaveService.Data.TryCount = 0;
				SaveService.Save();
			}
		}

		private string GetFailReasonString(FailReason reason)
		{
			return reason switch
			{
				FailReason.OutOfMoves => "out_of_moves",
				FailReason.Quit => "quit",
				_ => string.Empty
			};
		}
	}
}
