using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public enum SpecStatusMode
{
    ForceA = 0,
    Attribution = 1,
    ForceB = 2,
}

public static class CommonHelper
{

    public static bool IsDebug()
    {
        #if UNITY_EDITOR
        return true;
        #endif
        return false;//TODO: 临时测试，后续删除
    }
    //临时用户标记，非持久化数据,归因数据更变时会修改
    public static int UserType = 0;
    public static bool isConstraintBorA = false;
    public static int GetSpecStatus()
    {
        if (GF.Config != null && GF.Config.HasConfig("SpecStatus"))
        {
            return GF.Config.GetInt("SpecStatus", 0);
        }

        return (int)SpecStatusMode.ForceA;
    }

    public static bool ShouldUseAttribution()
    {
        return GetSpecStatus() == (int)SpecStatusMode.Attribution;
    }

    public static bool IsForceUserTypeMode()
    {
        int specStatus = GetSpecStatus();
        return specStatus == (int)SpecStatusMode.ForceA || specStatus == (int)SpecStatusMode.ForceB;
    }

    /// <summary>
    /// 根据 SpecStatus 应用用户类型。强制 A/B 模式每次启动覆盖 UserType；归因模式仅同步已识别结果。
    /// </summary>
    public static void ApplySpecStatusUserType()
    {
        if (GF.Config == null || !GF.Config.HasConfig("SpecStatus"))
        {
            return;
        }

        int specStatus = GetSpecStatus();
        if (specStatus == (int)SpecStatusMode.Attribution)
        {
            var playerData = GF.DataModel?.GetDataModel<PlayerDataModel>();
            if (playerData != null)
            {
                UserType = playerData.UserType;
            }

            return;
        }

        int userType = specStatus == (int)SpecStatusMode.ForceB ? 1 : 0;
        UserType = userType;
        SetUserType(userType, false);
        LogEvent(userType == 1 ? AdjustEventCodeEvent.b_enter : AdjustEventCodeEvent.a_enter);
    }

    /// <summary>
    /// 判断是否为专项渠道/非自然量用户（历史称 B 测试用户）
    /// 从 PlayerDataModel 读取：UserType == 1 时为 true
    /// </summary>
    public static bool IsSpec()
    {
        return true;
        var playerData = GF.DataModel?.GetDataModel<PlayerDataModel>();
        if (playerData != null)
        {
            // UserType: 0=自然量用户, 1=非自然量用户(B测试用户)
            return playerData.UserType == 1;
        }
        // 默认返回false（自然量用户）
        return false;
    }
    
    // 事件上报
    public static void LogEvent(string eventName, Dictionary<string, string> eventValues = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            return;
        }

        Log.Info($"log_event: {eventName}");
        AdjustManager.Instance.LogEvent(eventName, eventValues);
    }

    /// <summary>
    /// debug模式下，切换B测试用户
    /// </summary>
    public static void IsConstraintBorA()
    {
        if (IsForceUserTypeMode())
        {
            Log.Warning("SpecStatus 为强制模式，调试切换 A/B 将在下次启动时被配置覆盖");
        }

        var playerData = GF.DataModel?.GetDataModel<PlayerDataModel>();
        if (playerData != null)
        {
            isConstraintBorA = playerData.UserType==1;
            isConstraintBorA = !isConstraintBorA;
            // UserType: 0=自然量用户, 1=非自然量用户(B测试用户)
            playerData.UserType = isConstraintBorA ? 1 : 0;
            GFBuiltin.Setting.SetBool("UserType", isConstraintBorA);//保存用户类型到基础设置中用于AB测试标记
            playerData.Save();
            GF.Event.Fire(playerData, UserTypeChangeEventArgs.Create());
        }
    }

    public static void ConsumeCoins(int count = -1, Action<bool> onCompleted = null)
    {
        if (count < 0) return;
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData != null && playerData.Coins >= count)
        {
            playerData.Coins -= count;
            playerData.Save();
            onCompleted?.Invoke(true);
        }
        else
        {
            GF.UI.ShowToast("Not enough Coins!");
            onCompleted?.Invoke(false);
        }
    }

    public static bool RecognitionTimeOut = false;//识别超时标记
    public static bool IsRecognition()
    {
        var playerData = GF.DataModel?.GetOrCreate<PlayerDataModel>();
        if (playerData != null)
        {
            return playerData.IsRecognition;
        }
        return false;
    }
    public static string GetUserAdNetwork()
    {
        var playerData = GF.DataModel?.GetOrCreate<PlayerDataModel>();
        if (playerData != null)
        {
            return playerData.AdNetwork;
        }
        return string.Empty;
    }
    public static void SetUserAdNetwork(string adNetwork)
    {
        var playerData = GF.DataModel?.GetOrCreate<PlayerDataModel>();
        if (playerData != null)
        {
            playerData.AdNetwork = adNetwork;
            playerData.Save();
        }
    }

    /// <summary>
    /// 设置用户类型。
    /// markAsRecognized 仅在 Adjust 正常返回归因结果时传 true。
    /// </summary>
    public static void SetUserType(int userType, bool markAsRecognized)
    {
        var playerData = GF.DataModel?.GetDataModel<PlayerDataModel>();
        if (playerData != null)
        {
            UserType = userType;
            playerData.UserType = userType;
            if (markAsRecognized)
            {
                playerData.IsRecognition = true;
            }
            playerData.Save();
            GFBuiltin.Setting.SetBool("UserType", playerData.UserType == 1);//保存用户类型到基础设置中用于AB测试标记
            GFBuiltin.Setting.Save();
            UnityGameFramework.Runtime.Log.Info($"===>SetUserType userType: {userType}, markAsRecognized: {markAsRecognized}");
        }
    }
   
    //是否为审核阶段
    public static bool isForbiden = false;
    public static bool IsForbiden()
    {
        return isForbiden;
    }
    public static void SetForbiden(bool value)
    {
        isForbiden = value;
        // AInitIOSSDK.AInitBootstrap.SetForbidenIncome(value);
        Log.Info("===>SetForbiden value: " + value);
    }


    public static long GetNowTime()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }
    // 根据给定的数组，随意返回一个元素
    public static int RandomInt(this IList<int> array)
    {
        return UnityEngine.Random.Range(array[0], array[1]);
    }

    /// <summary>
    /// 从给定整数列表中随机取一个值，例如 [3, 6, 10] 随机返回 3、6 或 10 其中之一。
    /// </summary>
    public static int RandomPickInList(this IList<int> values)
    {
        if (values == null || values.Count == 0)
            throw new ArgumentException("列表不能为空", nameof(values));
        return values[UnityEngine.Random.Range(0, values.Count)];
    }

    /// <summary>
    /// 从给定整数列表中随机取一个值，例如 RandomPickInList(3, 6, 10)。
    /// </summary>
    public static int RandomPickInList(params int[] values)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("列表不能为空", nameof(values));
        return values[UnityEngine.Random.Range(0, values.Length)];
    }
    public static string GetNowTimeString(long timestamp)
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // 转换为本地时间
        DateTime dateTime = epoch.AddSeconds(timestamp).ToLocalTime();
        // 转换为指定格式的字符串
        string formattedDate = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        return formattedDate;
    }
    // 看广告
    public static void ShowVideoAd(string adId, Action<bool> onCompleted, int coins = -1)
    {
        // 从adId中提取ad_position_type，按"_"分割取前半段
        string adPositionType = adId.Contains("_") ? adId.Split('_')[0] : adId;

        // DataCollectManager.Instance.LogEvent("button_click", new Dictionary<string, object> {
        //     {"ad_type", "reward"},
        //     {"ad_position", adId},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", string.Empty}
        // });

#if UNITY_EDITOR
        ResetInterstitialCancelAccum();
        onCompleted?.Invoke(true);
        GF.Event.Fire(null, WatchVideoFinishEventArgs.Create());
        return;
#endif
        AdsManager.Instance.ShowAd(
            isOk =>
            {
                if (isOk)
                {
                    ResetInterstitialCancelAccum();
                    GF.Event.Fire(null, WatchVideoFinishEventArgs.Create());
                }
                else
                {
                    GF.UI.ShowToast("Ad Load failed !!!");
                }
                onCompleted?.Invoke(isOk);
            },
            adId
        );
    }

    // "button_show"事件上报
    public static void LogAdButtonShowEvent(string adId)
    {
        // 从adId中提取ad_position_type，按"_"分割取前半段
        string adPositionType = adId.Contains("_") ? adId.Split('_')[0] : adId;

        // DataCollectManager.Instance.LogEvent("button_show", new Dictionary<string, object> {
        //     {"ad_type", "reward"},
        //     {"ad_position", adId},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", string.Empty}
        // });
    }

    private static long nextShowInterstitialTime;
    /// <summary> 自上次成功激励/插屏后，连续调用 ShowInterstitial 的次数。 </summary>
    private static int s_InterstitialCancelAccum;

    static void ResetInterstitialCancelAccum()
    {
        s_InterstitialCancelAccum = 0;
    }

    public static void ShowInterstitial(string interstitialId, bool immediately = false)
    {
#if UNITY_EDITOR
        return;
#endif
        // 每次调用累计一次；达到 InterstitialCancelTime 才进入后续 CD/权重等判断
        s_InterstitialCancelAccum++;
        
        // 判断是不是前几次
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData != null)
        {
            playerData.InterstitialTimes++;
            playerData.Save();

            if (playerData.InterstitialTimes <= GF.Config.GetInt("InterstitialPreTimes"))
            {
                return;
            }
        }
        
        int cancelTime = GF.Config != null && GF.Config.HasConfig("InterstitialCancelTime")
            ? GF.Config.GetInt("InterstitialCancelTime")
            : 0;
        if (cancelTime > 0 && s_InterstitialCancelAccum < cancelTime)
            return;

        if (!immediately && GetNowTime() < nextShowInterstitialTime)
        {
            return;
        }

        bool isShow = UnityEngine.Random.Range(0, 101) < GF.Config.GetInt("InterstitialShowWeight");
        if (!isShow)
        {
            return;
        }

        // 从interstitialId中提取ad_position_type，按"_"分割取前半段
        string adPositionType = interstitialId.Contains("_") ? interstitialId.Split('_')[0] : interstitialId;

        // DataCollectManager.Instance.LogEvent("button_click", new Dictionary<string, object> {
        //     {"ad_type", "interstitial"},
        //     {"ad_position", interstitialId},
        //     {"ad_position_type", adPositionType},
        //     {"placement_id", string.Empty}
        // });
        AdsManager.Instance.ShowInter(interstitialId);
        nextShowInterstitialTime = GetNowTime() + GF.Config.GetInt("InterstitialCD");
        ResetInterstitialCancelAccum();
    }

    private static string _gaid;
    private static string _androidId;

    public static string GetAndroidId()
    {
        if (!string.IsNullOrEmpty(_androidId))
        {
            return _androidId;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (var secure = new AndroidJavaClass("android.provider.Settings$Secure"))
            {
                _androidId = secure.CallStatic<string>("getString", contentResolver, "android_id");
                Log.Info("提取 AndroidId  " + _androidId);
                return _androidId;
            }
        }
        catch (Exception e)
        {
            Log.Info("Failed to fetch AndroidId: " + e.Message);
            return null;
        }
#else
        return null;
#endif
    }

    public static void GetGAID(Action<string> callback)
    {
        if (!String.IsNullOrEmpty(_gaid))
        {
            callback?.Invoke(_gaid);
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 获取 AdvertisingIdClient 类
            AndroidJavaClass advertisingClass = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
            
            // 获取当前 Activity 上下文
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            // 调用 getAdvertisingIdInfo 方法
            AndroidJavaObject adInfo = advertisingClass.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
            
            // 提取 GAID
            string gaid = adInfo.Call<string>("getId");
            _gaid = gaid;
            callback?.Invoke(gaid);

            GF.LogInfo("提取 GAID  " + gaid);
        }
        catch (Exception e)
        {
            // //Debug.LogError("Failed to fetch GAID: " + e.Message);
            // callback?.Invoke(null);
        }
#elif UNITY_IOS && !UNITY_EDITOR

        // 请求用户授权
        Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
        {
            if (trackingEnabled)
            {
                //Debug.Log("IDFA: " + advertisingId);
                _gaid = advertisingId;
                callback?.Invoke(advertisingId);
                //Debug.Log("用户拒绝了跟踪请求 Idfa:  " + advertisingId);
            }
            else
            {
                string idfa = UnityEngine.iOS.Device.vendorIdentifier;
                _gaid = idfa;
                callback?.Invoke(null);
                //Debug.Log("用户拒绝了跟踪请求或未授权。返回默认值");
            }
        });

#else 
        //Debug.LogWarning("GAID is only available on Android devices.");
        callback?.Invoke(null);
#endif
    }
    public static string GetGAID()
    {
        if (!String.IsNullOrEmpty(_gaid))
        {
            return _gaid;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 获取 AdvertisingIdClient 类
            AndroidJavaClass advertisingClass = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");

            // 获取当前 Activity 上下文
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // 调用 getAdvertisingIdInfo 方法
            AndroidJavaObject adInfo = advertisingClass.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

            // 提取 GAID
            string gaid = adInfo.Call<string>("getId");
            _gaid = gaid;
            return gaid;

            GF.LogInfo("提取 GAID  " + gaid);
        }
        catch (Exception e)
        {
            return null;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        string idfa = UnityEngine.iOS.Device.advertisingIdentifier;
        _gaid = idfa;
        return idfa;
#else
        return null;
#endif
    }
    public static string GetUUID_IOS(string appcode)
    {
        string uuid = PlayerPrefs.GetString(appcode, "");
    #if UNITY_IOS
        if (string.IsNullOrEmpty(uuid))
        {
            uuid = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = UnityEngine.iOS.Device.vendorIdentifier;
            }
            PlayerPrefs.SetString(appcode, uuid);
        }
    #endif
        return uuid;
    }
    public static void OpenWebview(string url)
    {
        if (String.IsNullOrWhiteSpace(url))
        {
            return;
        }

        GetGAID(gaid =>
        {
            if (String.IsNullOrEmpty(gaid))
            {
                return;
            }

            var appluck_url = url.Replace("{gaid}", gaid);
            // LightWebviewAndroid.instance.open(appluck_url, LightWebviewAndroid.CloseMode.close);
        });
    }
    public static string language = "";
    public static ArrowLocalizationMoney m_LocalizationMoney;
    public static string GetDollarString(float value, bool needPrecision = true, bool needSymbol = true)
    {
        if (language == "")
        {
            PlayerDataModel playerDm = GF.DataModel.GetDataModel<PlayerDataModel>();
            if (!IsSpec())
            {
                language = "English";
                playerDm.Language = "English";
                playerDm.Save();
            }
            else if (playerDm != null && !string.IsNullOrEmpty(playerDm.Language))
            {
                language = playerDm.Language;
                var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
                var langRow = langTb.GetDataRow(row => row.LanguageKey == language);
                if (langRow == null)
                {
                    langRow = langTb.MinIdDataRow;
                    language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey).ToString();//不支持的语言默认用英文
                }
                playerDm.Language = language;
                playerDm.Save();
            }
            else
            {
                language = GFBuiltin.Localization.SystemLanguage.ToString();
                var langTb = GF.DataTable.GetDataTable<ArrowLanguagesTable>();
                var langRow = langTb.GetDataRow(row => row.LanguageKey == language);
                if (langRow == null)
                {
                    langRow = langTb.MinIdDataRow;
                    language = Enum.Parse<GameFramework.Localization.Language>(langRow.LanguageKey).ToString();//不支持的语言默认用英文
                }
                playerDm.Language = language;
                playerDm.Save();
            }
            if (m_LocalizationMoney == null)
            {
                var moneyTb = GF.DataTable.GetDataTable<ArrowLocalizationMoney>();
                m_LocalizationMoney = moneyTb.GetDataRow(row => row.LanguageName == language);
                if (m_LocalizationMoney == null)
                {
                    m_LocalizationMoney = moneyTb.MinIdDataRow;
                }
            }
        }
#if UNITY_EDITOR
        language = GF.Localization.Language.ToString(); //编辑器下使用设置语言方便测试;

        var moneyTb2 = GF.DataTable.GetDataTable<ArrowLocalizationMoney>();
        m_LocalizationMoney = moneyTb2.GetDataRow(row => row.LanguageName == language);
        if (m_LocalizationMoney == null)
        {
            m_LocalizationMoney = moneyTb2.MinIdDataRow;
        }

#endif
        var convertedValue = value * m_LocalizationMoney.CurrencyConverter;
        if (needPrecision)
        {
            var value2 = Math.Round(convertedValue, m_LocalizationMoney.Precision);
            if (needSymbol)
            {
                return m_LocalizationMoney.SymbolStr + value2.ToString();
            }
            return value2.ToString();

        }
        
        if (needSymbol)
        {
            return m_LocalizationMoney.SymbolStr + convertedValue.ToString();
        }

        return convertedValue.ToString();
    }

    /// <summary>
    /// 格式化大额数值显示为国际单位（K/M/B/T）
    /// </summary>
    /// <param name="number">要格式化的数值（double或可转换为double的值）</param>
    /// <param name="decimalPlaces">保留的小数位数（默认1位）</param>
    /// <returns>格式化后的字符串（如1.5K、3.2M等）</returns>
    public static string FormatLargeNumber(double number, int decimalPlaces = 2)
    {
        string[] suffixes = { "", "K", "M", "B", "T" };
        double absNumber = Math.Abs(number);

        // 小于1万不转换
        if (absNumber < 10000)
        {
            return number.ToString("N0");
        }

        // 确定单位和除数
        int suffixIndex = 0;
        double divisor = 1;

        if (absNumber >= 1e12) // 万亿(1e12)
        {
            suffixIndex = 4;
            divisor = 1e12;
        }
        else if (absNumber >= 1e9) // 十亿(1e9)
        {
            suffixIndex = 3;
            divisor = 1e9;
        }
        else if (absNumber >= 1e6) // 百万(1e6)
        {
            suffixIndex = 2;
            divisor = 1e6;
        }
        else if (absNumber >= 1e3) // 千(1e3)
        {
            suffixIndex = 1;
            divisor = 1e3;
        }

        // 计算并格式化
        double formattedNumber = number / divisor;
        string formatString = $"N{decimalPlaces}";
        return formattedNumber.ToString(formatString) + suffixes[suffixIndex];
    }

    /// <summary>
    /// 重载方法，支持字符串输入
    /// </summary>
    public static string FormatLargeNumber(string numberString, int decimalPlaces = 2)
    {
        if (double.TryParse(numberString, out double number))
        {
            return FormatLargeNumber(number, decimalPlaces);
        }
        Log.Error($"输入字符串不是有效的数值 {numberString}");
        return numberString;
    }
    // 安卓设备震动
    public static void Vibrate(long milliseconds)
    {
        if (GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate))
        {
            Log.Info("震动已关闭,不触发震动");
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        vibrator.Call("vibrate", milliseconds);
#elif UNITY_IOS
        Handheld.Vibrate(); // iOS或编辑器中的简单震动
#endif
    }

}
