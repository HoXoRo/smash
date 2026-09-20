using GameFramework;
using GameFramework.Event;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;


public enum PlayerDataType
{
    /// <summary>
    /// 玩家金币
    /// </summary>
    Coins,
    /// <summary>
    /// 玩家钻石
    /// </summary>
    Diamond,
    /// <summary>
    /// 关卡Id
    /// </summary>
    LevelId,
    /// <summary>
    /// 玩家钻石
    /// </summary>
    Gems,
    /// <summary>
    /// 道具1
    /// </summary>
    Prop1,
    /// <summary>
    /// 道具2
    /// </summary>
    Prop2,
    /// <summary>
    /// 道具3
    /// </summary>
    Prop3
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
[Serializable]
/// <summary>
/// 玩家数据类, 金币/血量等
/// </summary>
public class PlayerDataModel : DataModelStorageBase
{

    private float m_Coins;

    private float m_Diamond;

    private float m_Dollars;

    private int m_Gems;

    private int m_LevelId;

    private string m_LastSigninTime;

    private int m_SigninIndex;

    private string m_LastBingoCardRecoverTime = "0";  // 使用字符串存储时间戳

    private string m_LastBubbleTime = "0";  // 使用字符串存储时间戳

    private List<MakeupData> m_MakeupDatas = new List<MakeupData>();

    private MakeupPaymentInfo m_MakeupPaymentInfo = new MakeupPaymentInfo();
    private int m_Prop1;
    private int m_Prop2;
    private int m_Prop3;
    private int m_RewardArrowEliminateCount;

    /// <summary>
    /// 收集次数(消除次数)
    /// </summary>
    private int m_CollectCount;
    /// <summary>
    /// 收集美元货物次数
    /// </summary>
    private int m_CollectMoneyGoodsTimes;
    private List<int> m_PassedTasks;
    /// <summary>
    /// 当日通关数
    /// </summary>
    private int m_AchievingCount;
    /// <summary>
    /// 每个类型麻将消除次数
    /// </summary>
    private Dictionary<int, int> m_MatchCountDict = new Dictionary<int, int>();
    private int m_InterstitialTimes;
    private int m_RewardDialogDoubleTimes;
    [SerializeField] public string m_NewDayTime;
    [SerializeField] public bool isFreeSpin;
    [SerializeField] public string Payment;
    [SerializeField] public List<int> CompleteGuideIds = new List<int>();
    [SerializeField] public string Language;
    [SerializeField] public string PlayerName;
    [SerializeField] public int TodayWithdrawalNum;
    private int m_MahjongSkinId;
    private int m_MahjongBgSkinId;
    [SerializeField] public List<int> SkinUnLockedTileIds = new List<int>();
    [SerializeField] public List<int> SkinUnLockedBgIds = new List<int>();
    public Dictionary<int, int> m_LevelStarRecords = new Dictionary<int, int>();

    /// <summary> 刮刮卡消除进度（0 到 ScratchCardCondition 阈值，达标后可刮一次）。 </summary>
    public int m_ScratchCardCondition;

    /// <summary>
    /// 箭头彩色模式：true=普通箭头随机彩色，false=默认黑色。持久化存储。
    /// </summary>
    private bool m_UseColorfulArrows;

    /// <summary>
    /// 背景日/夜间模式：true=夜间(#232633)，false=日间(#EBEBEB)。持久化存储。
    /// </summary>
    private bool m_IsNightMode;

    /// <summary>
    /// 箭头高级皮肤 ID，0 表示默认皮肤。持久化存储。
    /// </summary>
    private int m_ArrowAdvancedSkinId;

    /// <summary>
    /// 兑换记录
    /// </summary>
    public List<MakeupData> makeupDatas
    {
        get => m_MakeupDatas;
        set
        {
            // if (m_MakeupDatas != value)
            // {
            m_MakeupDatas = value;
            // 触发提现数据变更事件
            // GF.Event.Fire(this, MakeupDataChangedEventArgs.Create(MakeupDataChangeType.Updated));
            // }
        }
    }

    public MakeupPaymentInfo MakeupPaymentInfo
    {
        get => m_MakeupPaymentInfo;
        set
        {
            // if (m_MakeupPaymentInfo != value)
            // {
            m_MakeupPaymentInfo = value;
            // 触发支付信息变更事件
            GF.Event.Fire(this, PaymentInfoChangedEventArgs.Create(PaymentInfoChangeType.All));
            // }
        }
    }

    public string LastSigninTime
    {
        get => m_LastSigninTime;
        set => m_LastSigninTime = value;
    }

    public int SigninIndex
    {
        get => m_SigninIndex;
        set => m_SigninIndex = value;
    }

    public float Coins
    {
        get => m_Coins;
        set => SetData(PlayerDataType.Coins, Mathf.Max(0, value));
    }

    public float Dollars
    {
        get => m_Dollars;
        set => SetData(PlayerDataType.Diamond, Mathf.Max(0f, value));
    }

    public int Prop1
    {
        get => m_Prop1;
        set => SetData(PlayerDataType.Prop1, value);
    }

    public int Prop2
    {
        get => m_Prop2;
        set => SetData(PlayerDataType.Prop2, value);
    }


    public int Prop3
    {
        get => m_Prop3;
        set => SetData(PlayerDataType.Prop3, value);
    }

    /// <summary>
    /// 箭头彩色模式（持久化）。true=普通箭头随机彩色，false=默认黑色。
    /// </summary>
    public bool UseColorfulArrows
    {
        get => m_UseColorfulArrows;
        set
        {
            if (m_UseColorfulArrows == value) return;
            GF.Event.Fire(this, UserSkinChangeEventArgs.Create());
            m_UseColorfulArrows = value;
            Save(false);
        }
    }

    /// <summary>
    /// 背景日/夜间模式（持久化）。true=夜间(#232633)，false=日间(#EBEBEB)。
    /// </summary>
    public bool IsNightMode
    {
        get => m_IsNightMode;
        set
        {
            // if (m_IsNightMode == value) return;
            m_IsNightMode = value;
            Save(false);
        }
    }

    /// <summary>
    /// 箭头高级皮肤 ID（持久化）。0=默认皮肤，非 0 时从 ArrowAdvancedSkinDatabase 取配置应用。
    /// </summary>
    public int ArrowAdvancedSkinId
    {
        get => m_ArrowAdvancedSkinId;
        set
        {
            // if (m_ArrowAdvancedSkinId == value) return;
            m_ArrowAdvancedSkinId = value;
            Save(false);
        }
    }

    public int CollectCount
    {
        get => m_CollectCount;
        set
        {
            m_CollectCount = value;
            GF.Event.Fire(null, MakeupTaskChangeEventArgs.Create());
        }
    }
    public int RewardArrowEliminateCount
    {
        get => m_RewardArrowEliminateCount;
        set => m_RewardArrowEliminateCount = value;
    }
    public int CollectMoneyGoodsTimes
    {
        get => m_CollectMoneyGoodsTimes;
        set => m_CollectMoneyGoodsTimes = value;
    }

    /// <summary>
    /// 上次触发泡泡时间戳
    /// </summary>
    public string LastBubbleTime
    {
        get => m_LastBubbleTime;
        set => m_LastBubbleTime = value;
    }

    /// <summary>
    /// 关卡
    /// </summary>
    public int LevelId
    {
        get => m_LevelId;
        set
        {
            var lvTb = GF.DataTable.GetDataTable<ArrowLevelTable>();
            int nextLvId = Const.RepeatLevel ? value : Mathf.Clamp(value, lvTb.MinIdDataRow.Id, lvTb.MaxIdDataRow.Id);
            SetData(PlayerDataType.LevelId, nextLvId);
            // SetData(PlayerDataType.LevelId, value);
        }
    }
    public int InterstitialTimes
    {
        get => m_InterstitialTimes;
        set
        {
            m_InterstitialTimes = value;
        }
    }

    /// <summary>
    /// 奖励弹窗已领取翻倍奖励次数（前 N 次免广告）。
    /// </summary>
    public int RewardDialogDoubleTimes
    {
        get => m_RewardDialogDoubleTimes;
        set => m_RewardDialogDoubleTimes = value;
    }

    public int AchievingCount
    {
        get => m_AchievingCount;
        set
        {
            m_AchievingCount = value;
            GF.Event.Fire(this, TaskUpdateEventArgs.Create());
        }
    }
    public Dictionary<int, int> MatchCountDict
    {
        get => m_MatchCountDict;
        set
        {
            m_MatchCountDict = value;
            GF.Event.Fire(this, TaskUpdateEventArgs.Create());
        }
    }
    public List<int> PassedTasks
    {
        get => m_PassedTasks;
        set
        {
            m_PassedTasks = value;
            GF.Event.Fire(this, TaskUpdateEventArgs.Create());
        }
    }

    private int m_UserType = 0;
    private bool m_IsRecognition = false;
    /// <summary>
    /// 用户类型：0=自然量用户(organic)，1=非自然量用户(付费广告用户)
    /// </summary>
    public int UserType
    {
        get => m_UserType;
        set
        {
            m_UserType = value;
            GF.Event.Fire(this, UserTypeChangeEventArgs.Create());
        }
    }

    /// <summary>
    /// 是否已识别用户类型（防止重复更改）
    /// </summary>
    public bool IsRecognition { get => m_IsRecognition; set => m_IsRecognition = value; }
    private string m_AdNetwork = "";
    /// <summary>
    /// 广告网络(归因数据)
    /// </summary>
    public string AdNetwork { get => m_AdNetwork; set => m_AdNetwork = value; }
    public int ScratchCardCondition { get => m_ScratchCardCondition; set => m_ScratchCardCondition = value; }
    public PlayerDataModel()
    {
    }

    protected override void OnInitialDataModel()
    {
        m_Coins = GF.Config.GetInt("DefaultCoins");
        m_Dollars = GF.Config.GetInt("DefaultDiamonds");
        m_Gems = 0;
        m_LevelId = 1;
        LastSigninTime = string.Empty;
        SigninIndex = 0;
        m_LastBingoCardRecoverTime = UtilityBuiltin.GetTimeStamp();
        makeupDatas = new List<MakeupData>();
        Payment = "0";
        m_MakeupPaymentInfo = new MakeupPaymentInfo();
        m_Prop1 = GF.Config.GetInt("DefaultProp");
        m_Prop2 = GF.Config.GetInt("DefaultProp");
        m_Prop3 = GF.Config.GetInt("DefaultProp");
        m_CollectCount = 0;
        m_CollectMoneyGoodsTimes = 0;
        m_PassedTasks = new List<int>();
        m_MatchCountDict = new Dictionary<int, int>();
        m_AchievingCount = 0;
        m_NewDayTime = UtilityBuiltin.GetTimeStamp();
        m_InterstitialTimes = 0;
        m_RewardDialogDoubleTimes = 0;
        isFreeSpin = false;
        m_LevelStarRecords = new Dictionary<int, int>();
        m_MahjongSkinId = 0;
        m_MahjongBgSkinId = 0;
        PlayerName = string.Empty;
        TodayWithdrawalNum = UnityEngine.Random.Range(12000, 26000);
        SkinUnLockedTileIds = new List<int>();
        SkinUnLockedBgIds = new List<int>();
        if (SkinUnLockedTileIds.Count == 0)
        {
            SkinUnLockedTileIds.Add(0);
        }
        if (SkinUnLockedBgIds.Count == 0)
        {
            SkinUnLockedBgIds.Add(0);
        }
        m_UserType = 0;
        m_IsRecognition = false;
        m_RewardArrowEliminateCount = 0;
        m_ScratchCardCondition = 0;
        m_UseColorfulArrows = false;
        m_IsNightMode = false;
        m_ArrowAdvancedSkinId = 0;
    }

    public int GetLevelStarRecord(int levelId)
    {
        if (levelId <= 0 || m_LevelStarRecords == null)
        {
            return 0;
        }

        return m_LevelStarRecords.TryGetValue(levelId, out var record) ? record : 0;
    }

    public void SetLevelStarRecord(int levelId, int record)
    {
        if (levelId <= 0)
        {
            return;
        }

        if (m_LevelStarRecords == null)
        {
            m_LevelStarRecords = new Dictionary<int, int>();
        }

        record = Mathf.Clamp(record, 0, 111);

        if (m_LevelStarRecords.TryGetValue(levelId, out var existing))
        {
            record = MergeStarRecord(existing, record);
        }

        m_LevelStarRecords[levelId] = record;
        Save(false);
    }

    private int MergeStarRecord(int oldRecord, int newRecord)
    {
        int oldComplete = (oldRecord / 100) % 10;
        int oldClean = (oldRecord / 10) % 10;
        int oldFast = oldRecord % 10;

        int newComplete = (newRecord / 100) % 10;
        int newClean = (newRecord / 10) % 10;
        int newFast = newRecord % 10;

        int mergedComplete = Mathf.Max(oldComplete, newComplete);
        int mergedClean = Mathf.Max(oldClean, newClean);
        int mergedFast = Mathf.Max(oldFast, newFast);

        return mergedComplete * 100 + mergedClean * 10 + mergedFast;
    }
    private float FormatValue(PlayerDataType type, float value)
    {
        switch (type)
        {
            case PlayerDataType.Diamond:
                // 钻石精确到小数点后3位
                return (float)Math.Round(value, 3);
            case PlayerDataType.Coins:
                // 金币取整
                return Mathf.Floor(value);
            default:
                return math.max(0, value);
        }
    }

    public void SetData(PlayerDataType tp, float value, bool triggerEvent = true)
    {
        float oldValue = 0;
        // 根据类型进行精度转换
        float formattedValue = FormatValue(tp, value);
        switch (tp)
        {
            case PlayerDataType.Diamond:
                oldValue = m_Dollars;
                m_Dollars = formattedValue;
                CommonHelper.LogStep2CompleteEvents(oldValue, formattedValue);
                break;
            case PlayerDataType.Coins:
                oldValue = m_Coins;
                m_Coins = formattedValue;
                break;
            case PlayerDataType.LevelId:
                oldValue = m_LevelId;
                m_LevelId = (int)formattedValue;
                GF.Event.Fire(null, MakeupTaskChangeEventArgs.Create());
                break;
            case PlayerDataType.Gems:
                oldValue = m_Gems;
                m_Gems = (int)formattedValue;
                GF.Event.Fire(null, MakeupTaskChangeEventArgs.Create());
                break;
            case PlayerDataType.Prop1:
                oldValue = m_Prop1;
                m_Prop1 = (int)formattedValue;
                break;
            case PlayerDataType.Prop2:
                oldValue = m_Prop2;
                m_Prop2 = (int)formattedValue;
                break;
            case PlayerDataType.Prop3:
                oldValue = m_Prop3;
                m_Prop3 = (int)formattedValue;
                break;
        }
        if (triggerEvent)
            GF.Event.Fire(this, PlayerDataChangedEventArgs.Create(tp, oldValue, formattedValue));
    }

    public void SetData(PlayerDataType tp, int value, bool triggerEvent = true)
    {
        // 整数类型直接调用float版本的SetData，会自动进行精度转换
        SetData(tp, (float)value, triggerEvent);
    }

    public bool IsTodaySigned()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (string.IsNullOrEmpty(m_LastSigninTime)) return false;
        DateTime last = DateTimeOffset.FromUnixTimeSeconds(long.Parse(m_LastSigninTime)).UtcDateTime;
        return last.ToString("yyyyMMdd") == today;
    }

    public void DoSignin()
    {
        m_LastSigninTime = UtilityBuiltin.GetTimeStamp();
        m_SigninIndex++;
        Save();
    }

    public void RefreshSignin()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (string.IsNullOrEmpty(m_LastSigninTime)) return;
        DateTime last = DateTimeOffset.FromUnixTimeSeconds(long.Parse(m_LastSigninTime)).UtcDateTime;
        if (last.ToString("yyyyMMdd") != today)
        {

        }
    }
    public void RefreshNewDay()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (string.IsNullOrEmpty(m_NewDayTime))
        {
            m_PassedTasks = new List<int>();
            m_MatchCountDict = new Dictionary<int, int>();
            m_AchievingCount = 0;
            m_NewDayTime = UtilityBuiltin.GetTimeStamp();
            TodayWithdrawalNum = UnityEngine.Random.Range(12000, 26000);
            isFreeSpin = false;
            return;
        }
        DateTime last = DateTimeOffset.FromUnixTimeSeconds(long.Parse(m_NewDayTime)).UtcDateTime;
        if (last.ToString("yyyyMMdd") != today)
        {
            m_PassedTasks = new List<int>();
            m_MatchCountDict = new Dictionary<int, int>();
            m_AchievingCount = 0;
            m_NewDayTime = UtilityBuiltin.GetTimeStamp();
            TodayWithdrawalNum = UnityEngine.Random.Range(12000, 26000);
            isFreeSpin = false;
        }
        if(SkinUnLockedTileIds.Count == 0)
        {
            SkinUnLockedTileIds.Add(0);
        }
        if(SkinUnLockedBgIds.Count == 0)
        {
            SkinUnLockedBgIds.Add(0);
        }
    }

    /// <summary>
    /// 添加提现数据
    /// </summary>
    public void AddMakeupData(MakeupData makeupData)
    {
        if (makeupData == null) return;

        makeupDatas.Add(makeupData);

        // 触发提现数据添加事件
        GF.Event.Fire(this, MakeupDataChangedEventArgs.Create(MakeupDataChangeType.Added, makeupData.makeupType, makeupData.id, makeupData.makeupStep));

        // 保存数据
        Save(false);
    }

    /// <summary>
    /// 更新提现数据
    /// </summary>
    public void UpdateMakeupData(MakeupData makeupData)
    {
        if (makeupData == null) return;

        var existingData = makeupDatas.Find(x => x.makeupType == makeupData.makeupType && x.id == makeupData.id);
        if (existingData != null)
        {
            var oldStep = existingData.makeupStep;
            // 更新数据
            existingData.makeupStep = makeupData.makeupStep;
            existingData.step1TaskNum = makeupData.step1TaskNum;
            existingData.step2TaskNum = makeupData.step2TaskNum;

            // 如果步骤发生变化，触发步骤变更事件
            if (oldStep != makeupData.makeupStep)
            {
                GF.Event.Fire(this, MakeupDataChangedEventArgs.Create(MakeupDataChangeType.StepChanged, makeupData.makeupType, makeupData.id, makeupData.makeupStep));
            }
            else
            {
                // 触发数据更新事件
                // GF.Event.Fire(this, MakeupDataChangedEventArgs.Create(MakeupDataChangeType.Updated, makeupData.makeupType, makeupData.id, makeupData.makeupStep));
            }

            // 保存数据
            Save(false);
        }
    }

    /// <summary>
    /// 移除提现数据
    /// </summary>
    public void RemoveMakeupData(MakeupType makeupType, int makeupId)
    {
        var existingData = makeupDatas.Find(x => x.makeupType == makeupType && x.id == makeupId);
        if (existingData != null)
        {
            makeupDatas.Remove(existingData);

            // 触发提现数据移除事件
            GF.Event.Fire(this, MakeupDataChangedEventArgs.Create(MakeupDataChangeType.Removed, makeupType, makeupId, existingData.makeupStep));

            // 保存数据
            Save(false);
        }
    }

    /// <summary>
    /// 更新支付信息
    /// </summary>
    public void UpdatePaymentInfo(string payment, string accountId, string email, string cpf = "")
    {
        if (MakeupPaymentInfo == null)
        {
            MakeupPaymentInfo = new MakeupPaymentInfo();
        }

        bool hasChanged = false;

        if (MakeupPaymentInfo.payment != payment)
        {
            MakeupPaymentInfo.payment = payment;
            hasChanged = true;
        }

        if (MakeupPaymentInfo.accountId != accountId)
        {
            MakeupPaymentInfo.accountId = accountId;
            hasChanged = true;
        }

        if (MakeupPaymentInfo.email != email)
        {
            MakeupPaymentInfo.email = email;
            hasChanged = true;
        }
        if (MakeupPaymentInfo.cpf != cpf)
        {
            MakeupPaymentInfo.cpf = cpf;
            hasChanged = true;
        }
        if (hasChanged)
        {
            // 触发支付信息变更事件
            GF.Event.Fire(this, PaymentInfoChangedEventArgs.Create(PaymentInfoChangeType.All, payment, accountId, email));

            // 同步支付信息到所有提现数据（暂定 提现信息已绑定有支付账号数据 就不会修改）
            // SyncPaymentInfoToAllMakeupData();

            // 保存数据
            Save(false);
        }
    }
}
