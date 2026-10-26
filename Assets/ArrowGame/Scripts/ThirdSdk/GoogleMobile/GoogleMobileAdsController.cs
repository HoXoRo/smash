using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityGameFramework.Runtime;
public class GoogleMobileAdsController
{
    public bool CanRequestAds => ConsentInformation.CanRequestAds();
    
    // Always use test ads.
    // https://developers.google.com/admob/unity/test-ads
    internal static List<string> TestDeviceIds = new List<string>()
    {
        AdRequest.TestDeviceSimulator,
#if UNITY_IPHONE
        "96e23e80653bb28980d3f40beb58915c",
#elif UNITY_ANDROID
        "19c2aa2b92d845e88c75ddd737618357"
#endif
    };

    // The Google Mobile Ads Unity plugin needs to be run only once.
    private static bool? _isInitialized;

    /// <summary>
    /// Demonstrates how to configure Google Mobile Ads Unity plugin.
    /// </summary>
    public GoogleMobileAdsController()
    {
        // On Android, Unity is paused when displaying interstitial or rewarded video.
        // This setting makes iOS behave consistently with Android.
        // MobileAds.SetiOSAppPauseOnBackground(true);

        // Configure your RequestConfiguration with Child Directed Treatment
        // and the Test Device Ids.
        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            TestDeviceIds = TestDeviceIds
        });

        // If we can request ads, we should initialize the Google Mobile Ads Unity plugin.
        if (CanRequestAds)
        {
            InitializeGoogleMobileAds();
        }

        // Ensures that privacy and consent information is up to date.
        InitializeGoogleMobileAdsConsent();
    }

    /// <summary>
    /// Ensures that privacy and consent information is up to date.
    /// </summary>
    private void InitializeGoogleMobileAdsConsent()
    {
        Log.Info("Google Mobile Ads gathering consent.");

        GatherConsent((string error) =>
        {
            if (error != null)
            {
                Debug.LogError("Failed to gather consent with error: " +
                    error);
            }
            else
            {
                Log.Info("Google Mobile Ads consent updated: "
                         + ConsentInformation.ConsentStatus);
            }

            if (CanRequestAds)
            {
                InitializeGoogleMobileAds();
            }
        });
    }
    
     /// <summary>
    /// Startup method for the Google User Messaging Platform (UMP) SDK
    /// which will run all startup logic including loading any required
    /// updates and displaying any required forms.
    /// </summary>
    public void GatherConsent(Action<string> onComplete)
    {

        var requestParameters = new ConsentRequestParameters
        {
            // False means users are not under age.
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = new ConsentDebugSettings
            {
                // For debugging consent settings by geography.
                DebugGeography = DebugGeography.Disabled,
                // https://developers.google.com/admob/unity/test-ads
                TestDeviceHashedIds = GoogleMobileAdsController.TestDeviceIds,
            }
        };

        // The Google Mobile Ads SDK provides the User Messaging Platform (Google's
        // IAB Certified consent management platform) as one solution to capture
        // consent for users in GDPR impacted countries. This is an example and
        // you can choose another consent management platform to capture consent.
        ConsentInformation.Update(requestParameters, (FormError updateError) =>
        {

            if (updateError != null)
            {
                onComplete(updateError.Message);
                return;
            }

            // Determine the consent-related action to take based on the ConsentStatus.
            if (CanRequestAds)
            {
                // Consent has already been gathered or not required.
                // Return control back to the user.
                onComplete(null);
                return;
            }

            // Consent not obtained and is required.
            // Load the initial consent request form for the user.
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
            {
                if (showError != null)
                {
                    // Form showing failed.
                    if (onComplete != null)
                    {
                        onComplete(showError.Message);
                    }
                }
                // Form showing succeeded.
                else if (onComplete != null)
                {
                    onComplete(null);
                }
            });
        });
    }

    /// <summary>
    /// Initializes the Google Mobile Ads Unity plugin.
    /// </summary>
    private void InitializeGoogleMobileAds()
    {
        // The Google Mobile Ads Unity plugin needs to be run only once and before loading any ads.
        if (_isInitialized.HasValue)
        {
            return;
        }

        _isInitialized = false;

        // Initialize the Google Mobile Ads Unity plugin.
        Log.Info("Google Mobile Ads Initializing.");

        // [START initialize_sdk]
        MobileAds.Initialize((InitializationStatus initstatus) =>
        {
            if (initstatus == null)
            {
                Debug.LogError("Google Mobile Ads initialization failed.");
                _isInitialized = null;
                return;
            }
            // [START_EXCLUDE silent]
            // If you use mediation, you can check the status of each adapter.
            var adapterStatusMap = initstatus.getAdapterStatusMap();
            if (adapterStatusMap != null)
            {
                foreach (var item in adapterStatusMap)
                {
                    Log.Info(string.Format("Adapter {0} is {1}",
                        item.Key,
                        item.Value.InitializationState));
                }
            }
            // [END_EXCLUDE]

            Debug.Log("Google Mobile Ads initialization complete.");
            _isInitialized = true;

            // Google Mobile Ads events are raised off the Unity Main thread. If you need to
            // access UnityEngine objects after initialization,
            // use MobileAdsEventExecutor.ExecuteInUpdate(). For more information, see:
            // https://developers.google.com/admob/unity/global-settings#raise_ad_events_on_the_unity_main_thread
        });
        // [END initialize_sdk]
    }

    /// <summary>
    /// Opens the AdInspector.
    /// </summary>
    public void OpenAdInspector()
    {
        Debug.Log("Opening ad Inspector.");
        MobileAds.OpenAdInspector((AdInspectorError error) =>
        {
            // If the operation failed, an error is returned.
            if (error != null)
            {
                Log.Info("Ad Inspector failed to open with error: " + error);
                return;
            }

            Log.Info("Ad Inspector opened successfully.");
        });
    }
}
