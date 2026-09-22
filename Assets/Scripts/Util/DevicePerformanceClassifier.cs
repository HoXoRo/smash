using System;
using System.Text;
using UnityEngine;

namespace Util
{
	public static class DevicePerformanceClassifier
	{
		private readonly struct DeviceContext
		{
			public readonly string DeviceModel;

			public readonly string DeviceName;

			public readonly string ProcessorType;

			public readonly string GpuName;

			public readonly string OperatingSystem;

			public readonly string DeviceFingerprint;

			public DeviceContext(string deviceModel, string deviceName, string processorType, string gpuName, string operatingSystem, string deviceFingerprint)
			{
				DeviceModel = deviceModel;
				DeviceName = deviceName;
				ProcessorType = processorType;
				GpuName = gpuName;
				OperatingSystem = operatingSystem;
				DeviceFingerprint = deviceFingerprint;
			}
		}

		private const int LowRamThresholdMb = 3072;

		private const int HighRamThresholdMb = 8192;

		private const int LowCpuCoreThreshold = 4;

		private const int HighCpuCoreThreshold = 8;

		private static bool hasCachedTier;

		private static DevicePerformanceTier cachedTier;

		public static DevicePerformanceTier GetTier()
		{
			if (!hasCachedTier)
			{
				cachedTier = GetTierInternal();
				hasCachedTier = true;
				Debug.Log("[DevicePerformance] Performance tier: " + cachedTier);
			}

			return cachedTier;
		}

		public static string GetDebugInfo()
		{
			DeviceContext deviceContext = GetDeviceContext();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("Tier: {0}\n", GetTier());
			stringBuilder.AppendFormat("RAM: {0} MB\n", SystemInfo.systemMemorySize);
			stringBuilder.AppendFormat("CPU Cores: {0}\n", SystemInfo.processorCount);
			stringBuilder.Append("CPU (normalized): ").Append(deviceContext.ProcessorType).Append('\n');
			stringBuilder.Append("GPU (normalized): ").Append(deviceContext.GpuName).Append('\n');
			stringBuilder.AppendFormat("GPU Memory: {0} MB\n", SystemInfo.graphicsMemorySize);
			stringBuilder.Append("Device Model (normalized): ").Append(deviceContext.DeviceModel).Append('\n');
			stringBuilder.Append("Device Name (normalized): ").Append(deviceContext.DeviceName).Append('\n');
			stringBuilder.Append("Device Fingerprint: ").Append(deviceContext.DeviceFingerprint).Append('\n');
			stringBuilder.Append("OS (normalized): ").Append(deviceContext.OperatingSystem);
			return stringBuilder.ToString();
		}

		public static void ClearCache()
		{
			hasCachedTier = false;
			cachedTier = DevicePerformanceTier.Low;
		}

		private static DeviceContext GetDeviceContext()
		{
			string text = Normalize(SystemInfo.deviceModel);
			string text2 = Normalize(SystemInfo.deviceName);
			string text3 = Normalize(SystemInfo.processorType);
			string text4 = Normalize(SystemInfo.graphicsDeviceName);
			string text5 = Normalize(SystemInfo.operatingSystem);
			string deviceFingerprint = BuildDeviceFingerprint(text, text2, text3);
			return new DeviceContext(text, text2, text3, text4, text5, deviceFingerprint);
		}

		private static DevicePerformanceTier GetTierInternal()
		{
			if (IsEditor())
			{
				return DevicePerformanceTier.High;
			}

			DeviceContext deviceContext = GetDeviceContext();
			int systemMemorySize = SystemInfo.systemMemorySize;
			int num = Mathf.Max(SystemInfo.processorCount, 1);
			if (IsKnownLowEndDevice(deviceContext.DeviceFingerprint) || IsKnownLowEndGpu(deviceContext.GpuName))
			{
				return DevicePerformanceTier.Low;
			}

			if (IsKnownHighEndDevice(deviceContext.DeviceFingerprint))
			{
				return DevicePerformanceTier.High;
			}

			if (systemMemorySize < LowRamThresholdMb || num < LowCpuCoreThreshold)
			{
				return DevicePerformanceTier.Low;
			}

			if (systemMemorySize >= HighRamThresholdMb && num >= HighCpuCoreThreshold && !IsSuspiciousGpuForHighTier(deviceContext.GpuName))
			{
				return DevicePerformanceTier.High;
			}

			return DevicePerformanceTier.Mid;
		}

		private static bool IsEditor()
		{
#if UNITY_EDITOR
			return true;
#else
			return false;
#endif
		}

		private static string Normalize(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			return value.Trim().ToLowerInvariant();
		}

		private static string BuildDeviceFingerprint(string deviceModel, string deviceName, string processorType)
		{
			StringBuilder stringBuilder = new StringBuilder();
			AppendFingerprintPart(stringBuilder, deviceModel);
			AppendFingerprintPart(stringBuilder, deviceName);
			AppendFingerprintPart(stringBuilder, processorType);
			return stringBuilder.ToString().Trim();
		}

		private static bool IsKnownLowEndDevice(string device)
		{
			if (string.IsNullOrEmpty(device))
			{
				return false;
			}

			return ContainsAny(device, "sm-t220", "sm-t225", "sm-a02", "sm-a03", "sm-a04", "sm-a05", "redmi 9", "redmi 9a", "redmi 9c", "redmi 10a", "redmi 10c", "redmi a1", "redmi a2", "redmi a3", "tecno", "infinix smart", "realme c11", "realme c21", "realme c30", "realme c31") || IsMotoE(device);
		}

		private static bool IsMotoE(string device)
		{
			if (string.IsNullOrEmpty(device))
			{
				return false;
			}

			return device.Contains("moto e") || device.Contains("motoe") || (device.Contains("motorola e") && !device.Contains("edge"));
		}

		private static bool IsKnownHighEndDevice(string device)
		{
			if (string.IsNullOrEmpty(device))
			{
				return false;
			}

			return ContainsAny(device, "sm-s", "sm-g98", "sm-g99", "pixel 7", "pixel 8", "pixel 9", "oneplus 11", "oneplus 12", "oneplus 13", "xiaomi 13", "xiaomi 14", "xiaomi 15");
		}

		private static bool IsKnownLowEndGpu(string gpuName)
		{
			if (string.IsNullOrEmpty(gpuName))
			{
				return false;
			}

			return ContainsAny(gpuName, "mali-g31", "mali-g51", "mali-g52", "mali-t", "adreno 308", "adreno 505", "adreno 506", "adreno 508", "adreno 509", "adreno 610", "adreno 612", "powervr ge", "powervr rogue ge");
		}

		private static bool IsSuspiciousGpuForHighTier(string gpuName)
		{
			if (IsKnownLowEndGpu(gpuName))
			{
				return true;
			}

			return ContainsAny(gpuName, "mali-g57", "adreno 615", "adreno 618", "adreno 619");
		}

		private static void AppendFingerprintPart(StringBuilder stringBuilder, string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return;
			}

			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}

			stringBuilder.Append(value);
		}

		private static bool ContainsAny(string value, params string[] needles)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			for (int i = 0; i < needles.Length; i++)
			{
				if (value.Contains(needles[i]))
				{
					return true;
				}
			}

			return false;
		}
	}
}
