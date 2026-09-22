using Service;
using UnityEngine;
using Inventory.TimedInventory;
using LocalSave;
using Util;

namespace Life
{
	public class LifeHelper : ServiceMonoBehaviour
	{
		private const int LifeGainInterval = 1800;

		public const int MaxLifeCount = 5;

		private TimeHelper _subscribedTimeHelper;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Init()
		{
			if (ServiceLocator.Get<LifeHelper>() != null || Object.FindObjectOfType<LifeHelper>() != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("LifeHelper");
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<LifeHelper>();
		}

		protected override void Awake()
		{
			LifeHelper existing = ServiceLocator.Get<LifeHelper>();
			if (existing != null && existing != this)
			{
				Destroy(gameObject);
				return;
			}
			LifeHelper[] helpers = FindObjectsOfType<LifeHelper>();
			for (int i = 0; i < helpers.Length; i++)
			{
				if (helpers[i] != this)
				{
					Destroy(gameObject);
					return;
				}
			}
			base.Awake();
			Object.DontDestroyOnLoad(gameObject);
		}

		private void Start()
		{
			Setup();
		}

		private void OnEnable()
		{
			EnsureTimeTickSubscription();
		}

		private void Setup()
		{
			EnsureTimeTickSubscription();
			Check();
		}

		private void EnsureTimeTickSubscription()
		{
			TimeHelper timeHelper = ServiceLocator.Get<TimeHelper>();
			if (_subscribedTimeHelper == timeHelper)
			{
				return;
			}
			RemoveTimeTickSubscription();
			if (timeHelper != null)
			{
				timeHelper.OnTick += Check;
				_subscribedTimeHelper = timeHelper;
			}
		}

		private void RemoveTimeTickSubscription()
		{
			if (_subscribedTimeHelper != null)
			{
				_subscribedTimeHelper.OnTick -= Check;
				_subscribedTimeHelper = null;
			}
		}

		private void Check()
		{
			int currentLifeCount = GetCurrentLifeCount();
			if (currentLifeCount >= MaxLifeCount)
			{
				SaveService.Data.LifeTimerStart = 0;
				TriggerTrackers();
				return;
			}
			int timerStart = GetLifeTimerStart();
			if (timerStart <= 0)
			{
				StartLifeTimer();
				return;
			}
			int elapsed = TimeUtil.GetNowSecondsUtc() - timerStart;
			if (elapsed >= LifeGainInterval)
			{
				int gainedLives = elapsed / LifeGainInterval;
				int newLifeCount = currentLifeCount + gainedLives;
				if (newLifeCount >= MaxLifeCount)
				{
					SetCurrentLifeCount(MaxLifeCount, false);
					SaveService.Data.LifeTimerStart = 0;
				}
				else
				{
					SetCurrentLifeCount(newLifeCount, false);
					SaveService.Data.LifeTimerStart = timerStart + gainedLives * LifeGainInterval;
				}
			}
			TriggerTrackers();
		}

		public void LoseLife()
		{
			if (TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife))
			{
				return;
			}
			int currentLifeCount = GetCurrentLifeCount();
			int newLifeCount = currentLifeCount > 0 ? currentLifeCount - 1 : 0;
			bool shouldStartTimer = newLifeCount < MaxLifeCount && GetLifeTimerStart() <= 0;
			SetCurrentLifeCount(newLifeCount, !shouldStartTimer);
			if (shouldStartTimer)
			{
				StartLifeTimer();
			}
		}

		public void AddLife(int count)
		{
			int newLifeCount = Mathf.Min(MaxLifeCount, GetCurrentLifeCount() + count);
			bool reachedMaxLife = newLifeCount >= MaxLifeCount;
			bool shouldStartTimer = !reachedMaxLife && GetLifeTimerStart() <= 0;
			SetCurrentLifeCount(newLifeCount, !reachedMaxLife && !shouldStartTimer);
			if (reachedMaxLife)
			{
				SaveService.Data.LifeTimerStart = 0;
				TriggerTrackers();
			}
			else if (shouldStartTimer)
			{
				StartLifeTimer();
			}
		}

		public int GetCurrentLifeCount()
		{
			return SaveService.Data.LifeCount;
		}

		public bool HasLife()
		{
			return TimedInventoryHelper.HasTime(TimedInventoryItemType.UnlimitedLife) || GetCurrentLifeCount() > 0;
		}

		public void FullLife()
		{
			SetCurrentLifeCount(MaxLifeCount, false);
			SaveService.Data.LifeTimerStart = 0;
			TriggerTrackers();
		}

		public void SetCurrentLifeCount(int val, bool triggerTrackers = true)
		{
			SaveService.Data.LifeCount = Mathf.Clamp(val, 0, MaxLifeCount);
			if (triggerTrackers)
			{
				TriggerTrackers();
			}
		}

		public int GetLifeTimerStart()
		{
			return SaveService.Data.LifeTimerStart;
		}

		public void StartLifeTimer()
		{
			SaveService.Data.LifeTimerStart = TimeUtil.GetNowSecondsUtc();
			TriggerTrackers();
		}

		public int GetSecondsUntilNextLife()
		{
			if (GetCurrentLifeCount() >= MaxLifeCount)
			{
				return -1;
			}
			int timerStart = GetLifeTimerStart();
			if (timerStart <= 0)
			{
				return LifeGainInterval;
			}
			int elapsed = TimeUtil.GetNowSecondsUtc() - timerStart;
			int remaining = LifeGainInterval - elapsed;
			return remaining > 0 ? remaining : 0;
		}

		private void TriggerTrackers()
		{
			LifeTrackerHelper.TriggerLifeCountTrackers();
			LifeTrackerHelper.TriggerLifeTimerTrackers();
		}

		private void OnDisable()
		{
			RemoveTimeTickSubscription();
		}

		protected override void OnDestroy()
		{
			RemoveTimeTickSubscription();
			base.OnDestroy();
		}
	}
}
