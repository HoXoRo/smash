using GameFramework;
using UnityGameFramework.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励数据
/// </summary>
public class RewardData
{
    public PlayerDataType Type { get; set; }           // 奖励类型
    public float Value { get; set; }                   // 奖励数量
    public string IconPath { get; set; }               // 图标路径
    public Action OnRewardCollected { get; set; }      // 领取奖励回调
    public Vector3? WorldPosition { get; set; }        // 奖励位置（可选）

    public RewardData(PlayerDataType type, float value, string iconPath = null, Action onCollected = null, Vector3? worldPos = null)
    {
        Type = type;
        Value = value;
        IconPath = iconPath;
        OnRewardCollected = onCollected;
        WorldPosition = worldPos;
    }
}

/// <summary>
/// 奖励弹窗参数
/// </summary>
public class RewardDialogParams
{
    public string rewardSource;                               // 奖励来源
    public string Title { get; set; }                          // 弹窗标题
    public string Description { get; set; }                    // 弹窗描述
    public List<RewardData> Rewards { get; set; }             // 奖励列表
    public Action OnDialogClosed { get; set; }                 // 弹窗关闭回调
    public bool AutoCollect { get; set; } = false;            // 是否自动收集奖励
    public float AutoCollectDelay { get; set; } = 0f;         // 自动收集延迟时间
    public bool isADReward { get; set; } = false;             // 是否是广告奖励
    public bool isADDouble { get; set; } = false;             // 是否是广告翻倍奖励

    public RewardDialogParams()
    {
        Rewards = new List<RewardData>();
    }

    public static RewardDialogParams Create()
    {
        return new RewardDialogParams();
    }
}

public class UIParams : RefParams
{
    public bool? AllowEscapeClose { get; set; } = null;
    public int? SortOrder { get; set; } = null;
    public bool IsSubUIForm { get; set; } = false;
    public GameFrameworkAction<UIFormLogic> OpenCallback { get; set; } = null;
    public GameFrameworkAction<UIFormLogic> CloseCallback { get; set; } = null;
    public GameFrameworkAction<object, string> ButtonClickCallback { get; set; } = null;

    // 添加奖励弹窗参数
    public RewardDialogParams RewardParams { get; set; }

    public static UIParams Create(bool? allowEscape = null, int? sortOrder = null)
    {
        var uiParms = ReferencePool.Acquire<UIParams>();
        uiParms.CreateRoot();
        uiParms.AllowEscapeClose = allowEscape;
        uiParms.SortOrder = sortOrder;
        uiParms.IsSubUIForm = false;
        return uiParms;
    }

    // 创建带奖励参数的UI参数
    public static UIParams CreateWithReward(RewardDialogParams rewardParams, bool? allowEscape = null, int? sortOrder = null)
    {
        var uiParams = Create(allowEscape, sortOrder);
        uiParams.RewardParams = rewardParams;
        return uiParams;
    }

    protected override void ResetProperties()
    {
        base.ResetProperties();
        AllowEscapeClose = null;
        SortOrder = null;
        OpenCallback = null;
        CloseCallback = null;
        ButtonClickCallback = null;
        IsSubUIForm = false;
        RewardParams = null;
    }
}
