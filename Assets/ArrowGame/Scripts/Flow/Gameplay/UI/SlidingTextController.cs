using System;
using Service;
using UnityEngine;

namespace Gameplay.UI
{
	public class SlidingTextController : ServiceMonoBehaviour
	{
		[SerializeField]
		private GameObject slidingTextPrefab;

		[SerializeField]
		private Transform root;

		private int _currentActiveCount;

		private const int MaxActiveCount = 1;

		public void Play(float viewportY, SlidingTextContentType type)
		{
			if (_currentActiveCount >= MaxActiveCount || slidingTextPrefab == null)
			{
				return;
			}

			GameObject instance = UnityEngine.Object.Instantiate(slidingTextPrefab, root);
			SlidingText slidingText = instance.GetComponent<SlidingText>();
			if (slidingText == null)
			{
				UnityEngine.Object.Destroy(instance);
				return;
			}

			_currentActiveCount++;
			slidingText.Play(viewportY, GetContentText(type), OnSlidingTextComplete);
		}

		private string GetContentText(SlidingTextContentType type)
		{
			if (type == SlidingTextContentType.TapOnTable)
			{
				return "Tap on the items on the table";
			}
			return string.Empty;
		}

		private void OnSlidingTextComplete()
		{
			_currentActiveCount = Math.Max(0, _currentActiveCount - 1);
		}
	}
}
