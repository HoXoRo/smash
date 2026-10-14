using System;
using LocalSave;
using UnityEngine;

namespace Util
{
	public static class RateUsHelper
	{
		private class OnCompleteListenerProxy : AndroidJavaProxy
		{
			private readonly Action<AndroidJavaObject> _onComplete;

			public OnCompleteListenerProxy(Action<AndroidJavaObject> onComplete)
				: base("com.google.android.gms.tasks.OnCompleteListener")
			{
				_onComplete = onComplete;
			}

			public void onComplete(AndroidJavaObject task)
			{
				_onComplete?.Invoke(task);
			}
		}

		private const int FirstRateUsLevel = 22;

		private const int RateUsLevelInterval = 50;

		public static void TryShowRateUs()
		{
			int level = GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId;
			if (!ShouldShowRateUs(level))
			{
				return;
			}
			MarkRateUsShown(level);
			RequestReview();
		}

		public static bool ShouldShowRateUs(int level)
		{
			return IsRateUsLevel(level) && !HasSeenRateUsForLevel(level);
		}

		public static bool IsRateUsLevel(int level)
		{
			return level >= FirstRateUsLevel && (level - FirstRateUsLevel) % RateUsLevelInterval == 0;
		}

		private static bool HasSeenRateUsForLevel(int level)
		{
			return SaveService.Data.RateUsLastShownLevel >= level;
		}

		public static void MarkRateUsShown(int level)
		{
			SaveService.Data.RateUsLastShownLevel = level;
			SaveService.Save();
		}

		public static void RequestReview()
		{
			RequestAndroidReview();
		}

		private static void RequestAndroidReview()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			using (AndroidJavaClass reviewManagerFactory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory"))
			using (AndroidJavaObject manager = reviewManagerFactory.CallStatic<AndroidJavaObject>("create", activity))
			{
				AndroidJavaObject request = manager.Call<AndroidJavaObject>("requestReviewFlow");
				request.Call<AndroidJavaObject>("addOnCompleteListener", new OnCompleteListenerProxy(delegate(AndroidJavaObject task)
				{
					if (!task.Call<bool>("isSuccessful"))
					{
						return;
					}
					AndroidJavaObject reviewInfo = task.Call<AndroidJavaObject>("getResult");
					activity.Call("runOnUiThread", new AndroidJavaRunnable(delegate
					{
						manager.Call<AndroidJavaObject>("launchReviewFlow", activity, reviewInfo);
					}));
				}));
			}
#endif
		}
	}
}
