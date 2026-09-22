using System;
using LocalSave;
using Service;
using UnityEngine;
using Util;

namespace Core
{
	public class SessionHelper : ServiceMonoBehaviour
	{
		private const long SessionTimeoutSec = 300L;

		private static SessionHelper _instance;

		private bool _paused;

		private bool _isActiveTimeCounting;

		private float _activeDurationSec;

		private long _lastFocusOutUnixSec;

		private int _currentDayId;

		public static event Action OnPause;

		public static event Action OnResume;

		public static void Initialize()
		{
			if (_instance != null)
			{
				return;
			}
			_instance = ServiceLocator.Get<SessionHelper>() ?? FindObjectOfType<SessionHelper>();
			if (_instance != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("SessionHelper");
			_instance = gameObject.AddComponent<SessionHelper>();
			DontDestroyOnLoad(gameObject);
			_instance.OnAppOpen();
		}

		protected override void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			base.Awake();
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}

		private void OnAppOpen()
		{
			FlushPendingSessionEndIfAny();
			StartNewSession();
		}

		private void StartNewSessionWithPendingFlush()
		{
			FlushPendingSessionEndIfAny();
			StartNewSession();
		}

		private void StartNewSession()
		{
			_activeDurationSec = 0f;
			_lastFocusOutUnixSec = 0L;
			_isActiveTimeCounting = true;
			_paused = false;
			if (SaveService.Data != null)
			{
				SaveService.Data.SessionId++;
			}
			CheckForRetention();
			if (GetSessionId() == 1)
			{
				SendFirstSessionEvent();
			}
		}

		private void CheckForRetention()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			if (SaveService.Data.InstallDay <= -1)
			{
				SaveService.Data.InstallDay = TimeUtil.GetDayCountSinceEpoch();
			}
			_currentDayId = TimeUtil.GetDayCountSinceEpoch() - SaveService.Data.InstallDay;
			if (SaveService.Data.LastLoggedDayId < _currentDayId)
			{
				SaveService.Data.LastLoggedDayId = _currentDayId;
			}
		}

		public int GetDayId()
		{
			return _currentDayId;
		}

		private void Update()
		{
			if (_isActiveTimeCounting)
			{
				_activeDurationSec += Time.unscaledDeltaTime;
			}
		}

		private static long GetCurrentUnixSec()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		private void EnterBackground()
		{
			_paused = true;
			_isActiveTimeCounting = false;
			_lastFocusOutUnixSec = GetCurrentUnixSec();
			SavePendingSessionEndSnapshot(_lastFocusOutUnixSec);
			OnPause?.Invoke();
		}

		private void ReturnFromBackground()
		{
			_paused = false;
			long now = GetCurrentUnixSec();
			if (_lastFocusOutUnixSec > 0L && now - _lastFocusOutUnixSec > SessionTimeoutSec)
			{
				StartNewSessionWithPendingFlush();
			}
			else
			{
				ClearPendingSessionEndIfCurrentSession();
				_isActiveTimeCounting = true;
			}
			OnResume?.Invoke();
		}

		private void SavePendingSessionEndSnapshot(long sessionEndUnixSec)
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.PendingSessionId = SaveService.Data.SessionId;
			SaveService.Data.PendingSessionEndUnixSec = sessionEndUnixSec;
			SaveService.Data.PendingSessionActiveDurationSec = _activeDurationSec;
		}

		private void ClearPendingSessionEnd()
		{
			if (SaveService.Data == null)
			{
				return;
			}
			SaveService.Data.PendingSessionId = 0;
			SaveService.Data.PendingSessionEndUnixSec = 0L;
			SaveService.Data.PendingSessionActiveDurationSec = 0f;
		}

		private void ClearPendingSessionEndIfCurrentSession()
		{
			if (SaveService.Data != null && SaveService.Data.PendingSessionId == SaveService.Data.SessionId)
			{
				ClearPendingSessionEnd();
			}
		}

		private void FlushPendingSessionEndIfAny()
		{
			if (SaveService.Data == null || SaveService.Data.PendingSessionId <= 0)
			{
				return;
			}
			ClearPendingSessionEnd();
		}

		public static int GetSessionId()
		{
			return SaveService.Data?.SessionId ?? 0;
		}

		private void SendFirstSessionEvent()
		{
			Debug.Log("[Session] First session started");
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus == !_paused)
			{
				return;
			}
			if (hasFocus)
			{
				ReturnFromBackground();
			}
			else
			{
				EnterBackground();
			}
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus == _paused)
			{
				return;
			}
			if (pauseStatus)
			{
				EnterBackground();
			}
			else
			{
				ReturnFromBackground();
			}
		}

		protected override void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
			base.OnDestroy();
		}
	}
}
