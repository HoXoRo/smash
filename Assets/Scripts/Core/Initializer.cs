using System.Collections;
using ABTesting;
using Haptics;
using RemoteConfig;
using Segmentation;
using Service;
using UnityEngine;

namespace Core
{
	public class Initializer : ServiceMonoBehaviour
	{
		public enum ATTStatus
		{
			UnableToRetrieve = -1,
			NotDetermined = 0,
			Restricted = 1,
			Denied = 2,
			Authorized = 3
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Init()
		{
			if (ServiceLocator.Get<Initializer>() != null || Object.FindObjectOfType<Initializer>() != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("Initializer");
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<Initializer>().Initialize();
		}

		protected override void Awake()
		{
			Initializer existing = ServiceLocator.Get<Initializer>();
			if (existing != null && existing != this)
			{
				Destroy(gameObject);
				return;
			}
			Initializer[] initializers = FindObjectsOfType<Initializer>();
			for (int i = 0; i < initializers.Length; i++)
			{
				if (initializers[i] != this)
				{
					Destroy(gameObject);
					return;
				}
			}
			base.Awake();
			Object.DontDestroyOnLoad(gameObject);
		}

		public void Initialize()
		{
			RemoteConfigService.InitializeLocal();
			SessionHelper.Initialize();
			SegmentationHelper.EvaluateSegment();
			ABHelper.EvaluateEligibleABs();
			CountryHelper.EnsureInstallCountry();
			HapticManager.Init();
		}

		public IEnumerator TryShowAttPopup()
		{
			yield break;
		}
	}
}
