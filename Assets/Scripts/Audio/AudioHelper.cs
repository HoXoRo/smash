using System.Collections.Generic;
using LocalSave;
using Scene;
using Service;
using UnityEngine;

namespace Audio
{
	public class AudioHelper : ServiceMonoBehaviour
	{
		private const int InitialPoolSize = 10;

		private readonly List<AudioSource> _pool = new List<AudioSource>();

		private AudioSource _musicSource;

		private bool _sfxEnabled;

		private bool _musicEnabled;

		private const float ThrottleWindowLength = 0.1f;

		private const int ThrottleCount = 3;

		private readonly Dictionary<AudioType, (float windowStart, int count)> _sfxThrottleDict = new Dictionary<AudioType, (float windowStart, int count)>();

		private Dictionary<AudioType, List<AudioClip>> _map = new Dictionary<AudioType, List<AudioClip>>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void CreateInstance()
		{
			if (ServiceLocator.Get<AudioHelper>() != null || Object.FindObjectOfType<AudioHelper>() != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("AudioHelper");
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<AudioHelper>();
		}

		protected override void Awake()
		{
			AudioHelper existing = ServiceLocator.Get<AudioHelper>();
			if (existing != null && existing != this)
			{
				Destroy(gameObject);
				return;
			}
			AudioHelper[] helpers = FindObjectsOfType<AudioHelper>();
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
			_sfxEnabled = SaveService.Data == null || SaveService.Data.SfxOn;
			_musicEnabled = SaveService.Data == null || SaveService.Data.MusicOn;
			for (int i = 0; i < InitialPoolSize; i++)
			{
				CreateSource();
			}
			CreateMusicSource();
			CacheMap();
			PlayMusicForCurrentScene();
		}

		private void CreateMusicSource()
		{
			_musicSource = gameObject.AddComponent<AudioSource>();
			_musicSource.playOnAwake = false;
			_musicSource.loop = true;
		}

		private void CacheMap()
		{
			_map = new Dictionary<AudioType, List<AudioClip>>();
			AudioData data = Resources.Load<AudioData>("AudioData");
			if (data == null || data.entries == null)
			{
				Debug.LogWarning("[Audio] AudioData resource not found");
				return;
			}
			foreach (AudioData.Entry entry in data.entries)
			{
				_map[entry.type] = entry.clips ?? new List<AudioClip>();
			}
		}

		private AudioSource CreateSource()
		{
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;
			_pool.Add(source);
			return source;
		}

		public void PlayMusicForCurrentScene()
		{
			if (!_musicEnabled || _musicSource == null)
			{
				return;
			}
			AudioType musicType = SceneHandler.GetCurrentScene() == SceneType.Gameplay ? AudioType.GameplayMusic : AudioType.MenuMusic;
			AudioClip clip = GetRandomClipForType(musicType);
			if (clip == null)
			{
				return;
			}
			if (_musicSource.clip == clip && _musicSource.isPlaying)
			{
				return;
			}
			_musicSource.clip = clip;
			_musicSource.Play();
		}

		private AudioSource GetFreeSource()
		{
			foreach (AudioSource source in _pool)
			{
				if (!source.isPlaying)
				{
					return source;
				}
			}
			return CreateSource();
		}

		private AudioClip GetRandomClipForType(AudioType type)
		{
			if (!_map.TryGetValue(type, out List<AudioClip> clips) || clips == null || clips.Count == 0)
			{
				return null;
			}
			return clips[Random.Range(0, clips.Count)];
		}

		public void PlaySfx(AudioType type)
		{
			if (type == AudioType.None || ShouldThrottle(type))
			{
				return;
			}
			Play(GetRandomClipForType(type));
		}

		private void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
		{
			if (!_sfxEnabled || clip == null)
			{
				return;
			}
			AudioSource source = GetFreeSource();
			source.volume = volume;
			source.pitch = pitch;
			source.clip = clip;
			source.Play();
		}

		private bool ShouldThrottle(AudioType type)
		{
			float now = Time.unscaledTime;
			if (!_sfxThrottleDict.TryGetValue(type, out (float windowStart, int count) state) || now - state.windowStart > ThrottleWindowLength)
			{
				_sfxThrottleDict[type] = (now, 1);
				return false;
			}
			state.count++;
			_sfxThrottleDict[type] = state;
			return state.count > ThrottleCount;
		}

		public void StopMusic()
		{
			_musicSource?.Stop();
		}

		public void StopAll()
		{
			foreach (AudioSource source in _pool)
			{
				if (source.isPlaying)
				{
					source.Stop();
				}
			}
		}

		public void EnableSfx()
		{
			_sfxEnabled = true;
			if (SaveService.Data != null)
			{
				SaveService.Data.SfxOn = true;
			}
		}

		public void DisableSfx()
		{
			_sfxEnabled = false;
			if (SaveService.Data != null)
			{
				SaveService.Data.SfxOn = false;
			}
			StopAll();
		}
	}
}
