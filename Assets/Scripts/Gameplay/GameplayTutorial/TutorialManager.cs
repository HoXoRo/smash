using System;
using System.Collections.Generic;
using Level;
using Service;
using UnityEngine;

namespace Gameplay.GameplayTutorial
{
	public class TutorialManager : ServiceMonoBehaviour
	{
		[SerializeField]
		private List<TutorialBase> tutorials;

		[SerializeField]
		private TutorialStagedLevel stagedLevelTutorial;

		private TutorialBase _activeTutorial;

		protected override void Awake()
		{
			base.Awake();
			tutorials?.ForEach(x =>
			{
				if (x != null)
				{
					x.gameObject.SetActive(false);
				}
			});
			if (stagedLevelTutorial != null)
			{
				stagedLevelTutorial.gameObject.SetActive(false);
			}
		}

		public void CheckTutorial()
		{
			SyncActiveTutorialState();
			if (_activeTutorial != null)
			{
				return;
			}

			LevelData levelData = ServiceLocator.Get<GameController>()?.GetLevelData();
			int index = GetTutorialIndexToShow(levelData);
			if (index < 0)
			{
				return;
			}

			_activeTutorial = tutorials[index];
			_activeTutorial.gameObject.SetActive(true);
			_activeTutorial.Play(this, null);
		}

		public bool CheckStagedLevelTutorial(Action<int, int> onStageComplete)
		{
			SyncActiveTutorialState();
			if (_activeTutorial != null || stagedLevelTutorial == null)
			{
				return false;
			}

			LevelData levelData = ServiceLocator.Get<GameController>()?.GetLevelData();
			if (!stagedLevelTutorial.ShouldPlay(levelData))
			{
				return false;
			}

			_activeTutorial = stagedLevelTutorial;
			_activeTutorial.gameObject.SetActive(true);
			_activeTutorial.Play(this, onStageComplete);
			return true;
		}

		private int GetTutorialIndexToShow(LevelData levelData)
		{
			if (levelData == null || tutorials == null)
			{
				return -1;
			}
			for (int i = 0; i < tutorials.Count; i++)
			{
				if (tutorials[i] != null && tutorials[i].ShouldPlay(levelData))
				{
					return i;
				}
			}
			return -1;
		}

		public void OnTutorialEnd()
		{
			SyncActiveTutorialState();
			if (_activeTutorial != null)
			{
				_activeTutorial.gameObject.SetActive(false);
				_activeTutorial = null;
			}
		}

		public bool AnyActiveTutorial()
		{
			SyncActiveTutorialState();
			return _activeTutorial != null;
		}

		private void SyncActiveTutorialState()
		{
			if (_activeTutorial != null && !_activeTutorial.gameObject.activeInHierarchy)
			{
				_activeTutorial = null;
			}
		}
	}
}
