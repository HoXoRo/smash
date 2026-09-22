using System;
using ABTesting;
using Gameplay.GameplayTutorial;
using IAP.Persisted;
using Newtonsoft.Json;

namespace LocalSave
{
	[Serializable]
	public class SaveData
	{
		[JsonIgnore]
		private readonly SaveItem<int> _coins = new SaveItem<int>(1000, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _prelevelRocket = new SaveItem<int>(3, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _level = new SaveItem<int>(1, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<string> _userId = new SaveItem<string>(string.Empty, SaveBehaviour.SaveNow);

		[JsonIgnore]
		private readonly SaveItem<int> _lifeCount = new SaveItem<int>(5, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _lifeTimerStart = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<bool> _musicOn = new SaveItem<bool>(true, SaveBehaviour.Dirty);

		[JsonIgnore]
		private readonly SaveItem<bool> _sfxOn = new SaveItem<bool>(true, SaveBehaviour.Dirty);

		[JsonIgnore]
		private readonly SaveItem<bool> _hapticOn = new SaveItem<bool>(true, SaveBehaviour.Dirty);

		[JsonIgnore]
		private readonly SaveItem<TutorialProgressData> _tutorialProgress = new SaveItem<TutorialProgressData>(TutorialProgressData.GetDefault(), SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _sessionId = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _tryCount = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<bool> _hasNoAds = new SaveItem<bool>(false, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _installDay = new SaveItem<int>(-1, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _lastLoggedDayId = new SaveItem<int>(-1, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _pendingSessionId = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<long> _pendingSessionEndUnixSec = new SaveItem<long>(0L, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<float> _pendingSessionActiveDurationSec = new SaveItem<float>(0f, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<PersistedABEntries> _abEntries = new SaveItem<PersistedABEntries>(PersistedABEntries.GetDefault(), SaveBehaviour.SaveNow);

		[JsonIgnore]
		private readonly SaveItem<PersistedPurchases> _purchases = new SaveItem<PersistedPurchases>(PersistedPurchases.GetDefault(), SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _streakCount = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<string> _buildEnvironment = new SaveItem<string>(string.Empty, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<string> _campaign = new SaveItem<string>(string.Empty, SaveBehaviour.SaveNow);

		[JsonIgnore]
		private readonly SaveItem<string> _country = new SaveItem<string>(string.Empty, SaveBehaviour.SaveNow);

		[JsonIgnore]
		private readonly SaveItem<string> _segment = new SaveItem<string>(string.Empty, SaveBehaviour.SaveNow);

		[JsonIgnore]
		private readonly SaveItem<int> _rateUsLastShownLevel = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _unlimitedLifeEnd = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<int> _unlimitedLifeTime = new SaveItem<int>(0, SaveBehaviour.SaveAtFrameEnd);

		[JsonIgnore]
		private readonly SaveItem<bool> _seenRewardedEgoOnce = new SaveItem<bool>(false, SaveBehaviour.SaveAtFrameEnd);

		[JsonProperty("coins")]
		public int Coins
		{
			get => _coins.Value;
			set => _coins.Value = value;
		}

		[JsonProperty("prelevel_rocket")]
		public int PrelevelRocket
		{
			get => _prelevelRocket.Value;
			set => _prelevelRocket.Value = value;
		}

		[JsonProperty("level")]
		public int Level
		{
			get => _level.Value;
			set => _level.Value = value;
		}

		[JsonProperty("user_id")]
		public string UserId
		{
			get => _userId.Value;
			set => _userId.Value = value;
		}

		[JsonProperty("life_count")]
		public int LifeCount
		{
			get => _lifeCount.Value;
			set => _lifeCount.Value = value;
		}

		[JsonProperty("life_timer_start")]
		public int LifeTimerStart
		{
			get => _lifeTimerStart.Value;
			set => _lifeTimerStart.Value = value;
		}

		[JsonProperty("music_on")]
		public bool MusicOn
		{
			get => _musicOn.Value;
			set => _musicOn.Value = value;
		}

		[JsonProperty("sfx_on")]
		public bool SfxOn
		{
			get => _sfxOn.Value;
			set => _sfxOn.Value = value;
		}

		[JsonProperty("haptic_on")]
		public bool HapticOn
		{
			get => _hapticOn.Value;
			set => _hapticOn.Value = value;
		}

		[JsonProperty("tutorial_progress")]
		public TutorialProgressData TutorialProgress
		{
			get => _tutorialProgress.Value;
			set => _tutorialProgress.Value = value;
		}

		[JsonProperty("session_id")]
		public int SessionId
		{
			get => _sessionId.Value;
			set => _sessionId.Value = value;
		}

		[JsonProperty("try_count")]
		public int TryCount
		{
			get => _tryCount.Value;
			set => _tryCount.Value = value;
		}

		[JsonProperty("has_no_ads")]
		public bool HasNoAds
		{
			get => _hasNoAds.Value;
			set => _hasNoAds.Value = value;
		}

		[JsonProperty("install_day")]
		public int InstallDay
		{
			get => _installDay.Value;
			set => _installDay.Value = value;
		}

		[JsonProperty("last_logged_day_id")]
		public int LastLoggedDayId
		{
			get => _lastLoggedDayId.Value;
			set => _lastLoggedDayId.Value = value;
		}

		[JsonProperty("pending_session_id")]
		public int PendingSessionId
		{
			get => _pendingSessionId.Value;
			set => _pendingSessionId.Value = value;
		}

		[JsonProperty("pending_session_end_unix_sec")]
		public long PendingSessionEndUnixSec
		{
			get => _pendingSessionEndUnixSec.Value;
			set => _pendingSessionEndUnixSec.Value = value;
		}

		[JsonProperty("pending_session_active_duration_sec")]
		public float PendingSessionActiveDurationSec
		{
			get => _pendingSessionActiveDurationSec.Value;
			set => _pendingSessionActiveDurationSec.Value = value;
		}

		[JsonProperty("ab_entries")]
		public PersistedABEntries ABEntries
		{
			get => _abEntries.Value;
			set => _abEntries.Value = value;
		}

		[JsonProperty("purchases")]
		public PersistedPurchases Purchases
		{
			get => _purchases.Value;
			set => _purchases.Value = value;
		}

		[JsonProperty("streak_count")]
		public int StreakCount
		{
			get => _streakCount.Value;
			set => _streakCount.Value = value;
		}

		[JsonProperty("build_environment")]
		public string BuildEnvironment
		{
			get => _buildEnvironment.Value;
			set => _buildEnvironment.Value = value;
		}

		[JsonProperty("campaign")]
		public string Campaign
		{
			get => _campaign.Value;
			set => _campaign.Value = value;
		}

		[JsonProperty("country")]
		public string Country
		{
			get => _country.Value;
			set => _country.Value = value;
		}

		[JsonProperty("segment")]
		public string Segment
		{
			get => _segment.Value;
			set => _segment.Value = value;
		}

		[JsonProperty("rate_us_last_shown_level")]
		public int RateUsLastShownLevel
		{
			get => _rateUsLastShownLevel.Value;
			set => _rateUsLastShownLevel.Value = value;
		}

		[JsonProperty("unlimited_life_end")]
		public int UnlimitedLifeEnd
		{
			get => _unlimitedLifeEnd.Value;
			set => _unlimitedLifeEnd.Value = value;
		}

		[JsonProperty("unlimited_life_time")]
		public int UnlimitedLifeTime
		{
			get => _unlimitedLifeTime.Value;
			set => _unlimitedLifeTime.Value = value;
		}

		[JsonProperty("seen_rewarded_ego_once")]
		public bool SeenRewardedEgoOnce
		{
			get => _seenRewardedEgoOnce.Value;
			set => _seenRewardedEgoOnce.Value = value;
		}
	}
}
