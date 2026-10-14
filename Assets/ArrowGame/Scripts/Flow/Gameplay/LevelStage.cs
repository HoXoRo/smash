using System.Collections.Generic;
using Gameplay.Objects;
using Gameplay.Obstacle;
using UnityEngine;

namespace Gameplay
{
	public class LevelStage
	{
		public Transform RootTransform;

		public List<BaseObject> LiveObjects = new List<BaseObject>();

		public List<Table> Tables = new List<Table>();

		public List<Blocker> Blockers = new List<Blocker>();

		public void Deactivate()
		{
			LiveObjects.ForEach(x =>
			{
				if (x != null)
				{
					x.gameObject.SetActive(false);
				}
			});
		}

		public void Activate()
		{
			LiveObjects.ForEach(x =>
			{
				if (x != null)
				{
					x.gameObject.SetActive(true);
				}
			});
		}
	}
}
