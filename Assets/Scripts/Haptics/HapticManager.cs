using System.Collections.Generic;
using System.ComponentModel;
using Gameplay.Objects;
using LocalSave;
//using Lofelt.NiceVibrations;
using SRDebugger;
using UnityEngine;
using Util;

namespace Haptics
{
	public static class HapticManager
	{
		[Range(0f, 1f)]
		[Category("Haptics")]
		[SROption]
		public static float CustomAmplitude;

		[Range(0f, 1f)]
		[Category("Haptics")]
		[SROption]
		public static float CustomFrequency;

		[Range(0f, 1f)]
		[SROption]
		[Category("Haptics")]
		public static float ObjectMinAmplitude;

		[SROption]
		[Range(0f, 1f)]
		[Category("Haptics")]
		public static float ObjectMaxAmplitude;

		[SROption]
		[Range(0f, 1f)]
		[Category("Haptics")]
		public static float ObjectMinFrequency;

		[Category("Haptics")]
		[Range(0f, 1f)]
		[SROption]
		public static float ObjectMaxFrequency;

		[SROption]
		public static float ThrottleWindowLength;

		[SROption]
		public static int ThrottleCount;

		private const int EmphasisThrottleKey = 0;

		private const int ObjectBreakThrottleKey = 0;

		private static readonly Dictionary<HapticType, (float windowStart, int count)> _presetThrottleDict = new Dictionary<HapticType, (float windowStart, int count)>();

		private static readonly Dictionary<int, (float windowStart, int count)> _emphasisThrottleDict = new Dictionary<int, (float windowStart, int count)>();

		private static readonly Dictionary<int, (float windowStart, int count)> _objectBreakThrottleDict = new Dictionary<int, (float windowStart, int count)>();

		private static bool _enabled;

		public static void Init()
		{
			_enabled = SaveService.Data == null || SaveService.Data.HapticOn;
		}

		public static void Enable()
		{
			_enabled = true;
		}

		public static void Disable()
		{
			_enabled = false;
		}

		public static void Play(HapticType type)
		{
			if (!_enabled || type == HapticType.None || ShouldThrottle(_presetThrottleDict, type))
			{
				return;
			}
			//switch (type)
			//{
			//case HapticType.Selection:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
			//	break;
			//case HapticType.Success:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
			//	break;
			//case HapticType.Warning:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
			//	break;
			//case HapticType.Failure:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
			//	break;
			//case HapticType.Light:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
			//	break;
			//case HapticType.Medium:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
			//	break;
			//case HapticType.Heavy:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
			//	break;
			//case HapticType.Rigid:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.RigidImpact);
			//	break;
			//case HapticType.Soft:
			//	HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
			//	break;
			//}
		}

		public static void PlayEmphasis(float amp, float freq)
		{
			if (!_enabled || ShouldThrottle(_emphasisThrottleDict, EmphasisThrottleKey))
			{
				return;
			}
			//HapticPatterns.PlayEmphasis(Mathf.Clamp01(amp), Mathf.Clamp01(freq));
		}

		public static void PlayCustom()
		{
			PlayEmphasis(CustomAmplitude, CustomFrequency);
		}

		public static void PlayObjectBreak(ObjectType type, float volume)
		{
			if (!_enabled || ShouldThrottle(_objectBreakThrottleDict, ObjectBreakThrottleKey))
			{
				return;
			}
			float normalizedMass = ObjectMassMaps.GetNormalizedMass(type, Mathf.RoundToInt(volume));
			float amp = Mathf.Lerp(ObjectMinAmplitude, ObjectMaxAmplitude, normalizedMass);
			float freq = Mathf.Lerp(ObjectMinFrequency, ObjectMaxFrequency, normalizedMass);
			//HapticPatterns.PlayEmphasis(Mathf.Clamp01(amp), Mathf.Clamp01(freq));
		}

		private static bool ShouldThrottle<T>(Dictionary<T, (float windowStart, int count)> dict, T key)
		{
			if (!CommonUtils.IsAndroid() || ThrottleWindowLength <= 0f || ThrottleCount <= 0)
			{
				return false;
			}
			float now = Time.unscaledTime;
			if (dict.TryGetValue(key, out (float windowStart, int count) entry) && now - entry.windowStart <= ThrottleWindowLength)
			{
				if (entry.count >= ThrottleCount)
				{
					return true;
				}
				dict[key] = (entry.windowStart, entry.count + 1);
				return false;
			}
			dict[key] = (now, 1);
			return false;
		}

		static HapticManager()
		{
			CustomAmplitude = 0.5f;
			CustomFrequency = 0.5f;
			ObjectMinAmplitude = 0.4f;
			ObjectMaxAmplitude = 0.7f;
			ObjectMinFrequency = 0.4f;
			ObjectMaxFrequency = 0.7f;
			ThrottleWindowLength = 0.25f;
			ThrottleCount = 2;
		}
	}
}
