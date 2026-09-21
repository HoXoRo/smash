using System;
using System.Collections.Generic;
using GameFramework.DataTable;
using UnityEngine;
using UnityGameFramework.Runtime;

public enum ArrowRewardType
{
    Normal = 1,
    Win = 2,
    Bubble = 3,
    Combo = 5,
}

/// <summary>
/// 统一奖励计算：配表梯度 + 超出梯度固定产出。
/// </summary>
public static class ArrowRewardCalculator
{
    const float DefaultNonSpecReward = 10f;
    const float FixedGradientOverflowReward = 0.0000001f;
    const int FirstRewardRowId = 1;

    public static float CalculateReward(ArrowRewardType rewardType)
    {
        if (!CommonHelper.IsSpec())
            return DefaultNonSpecReward;

        if (!CommonHelper.TryGetStep2MakeupContext(out var makeupData, out var config, out float target, out float currentDollars))
            return CalculatePreStep2Reward(rewardType);

        if (makeupData.makeupStep != MakeupStep.Step2)
            return CalculatePreStep2Reward(rewardType);

        float gradient = CalculateStep2GradientPreRatio(currentDollars, makeupData.step2OriginVlue);
        return CalculateStep2Reward(rewardType, gradient, CommonHelper.GetStep2MonStored(config), target, currentDollars, config);
    }

    static float CalculatePreStep2Reward(ArrowRewardType rewardType)
    {
        if (!TryGetFirstRowRange(rewardType, out float[] range))
            return 0f;

        return RandomInRange(range);
    }

    static float CalculateStep2Reward(
        ArrowRewardType rewardType,
        float gradientPreRatio,
        float step2MonStored,
        float targetDollars,
        float currentDollars,
        ArrowMakeupBanknotes config)
    {
        if (step2MonStored <= 0f)
            return 0f;

        if (!TryFindRowByGradient(rewardType, gradientPreRatio, out _, out float[] range))
        {
            float remainingStored = targetDollars - currentDollars;
            if (remainingStored <= 0f)
                return 0f;

            float minGapStored = CommonHelper.GetStep2RemainingMinGap(config);
            if (remainingStored - minGapStored < FixedGradientOverflowReward)
                return 0f;

            return FixedGradientOverflowReward;
        }

        return RandomInRange(range);
    }

    /// <summary>
    /// 玩家存储美元与 Step2 目标均为存储值；配表 Step2_Mon、Currency 梯度为乘以提现比例后的展示值。
    /// 展示梯度 = 存储进度 * MakeupRatio。
    /// </summary>
    static float CalculateStep2GradientPreRatio(float currentDollars, float step2OriginValue)
    {
        return Mathf.Max(0f, currentDollars - step2OriginValue);
    }

    static bool TryFindRowByGradient(
        ArrowRewardType rewardType,
        float gradient,
        out float matchedCurrency,
        out float[] range)
    {
        matchedCurrency = 0f;
        range = null;

        var rows = GetSortedRows(rewardType);
        if (rows == null || rows.Count == 0)
            return false;

        foreach (var row in rows)
        {
            if (gradient <= row.Currency)
            {
                matchedCurrency = row.Currency;
                range = row.RewardRange;
                return range != null && range.Length > 0;
            }
        }

        return false;
    }

    static bool TryGetFirstRowRange(ArrowRewardType rewardType, out float[] range)
    {
        range = null;
        switch (rewardType)
        {
            case ArrowRewardType.Normal:
                return TryGetRowRange(GF.DataTable.GetDataTable<ArrowNormalRewardTable>()?.GetDataRow(FirstRewardRowId)?.RewardRange, out range);
            case ArrowRewardType.Combo:
                return TryGetRowRange(GF.DataTable.GetDataTable<ArrowComboRewardTable>()?.GetDataRow(FirstRewardRowId)?.RewardRange, out range);
            case ArrowRewardType.Bubble:
                return TryGetRowRange(GF.DataTable.GetDataTable<ArrowBubbleRewardTable>()?.GetDataRow(FirstRewardRowId)?.RewardRange, out range);
            case ArrowRewardType.Win:
                return TryGetRowRange(GF.DataTable.GetDataTable<ArrowWinRewardTable>()?.GetDataRow(FirstRewardRowId)?.RewardRange, out range);
            default:
                Log.Warning($"ArrowRewardCalculator: unsupported reward type {rewardType}");
                return false;
        }
    }

    static List<RewardTableRowSnapshot> GetSortedRows(ArrowRewardType rewardType)
    {
        switch (rewardType)
        {
            case ArrowRewardType.Normal:
                return BuildSortedRows(GF.DataTable.GetDataTable<ArrowNormalRewardTable>(), row => row.Currency, row => row.RewardRange);
            case ArrowRewardType.Combo:
                return BuildSortedRows(GF.DataTable.GetDataTable<ArrowComboRewardTable>(), row => row.Currency, row => row.RewardRange);
            case ArrowRewardType.Bubble:
                return BuildSortedRows(GF.DataTable.GetDataTable<ArrowBubbleRewardTable>(), row => row.Currency, row => row.RewardRange);
            case ArrowRewardType.Win:
                return BuildSortedRows(GF.DataTable.GetDataTable<ArrowWinRewardTable>(), row => row.Currency, row => row.RewardRange);
            default:
                Log.Warning($"ArrowRewardCalculator: unsupported reward type {rewardType}");
                return null;
        }
    }

    static List<RewardTableRowSnapshot> BuildSortedRows<T>(
        IDataTable<T> table,
        Func<T, float> getCurrency,
        Func<T, float[]> getRange) where T : DataRowBase
    {
        if (table == null)
        {
            Log.Warning($"ArrowRewardCalculator: data table {typeof(T).Name} is missing.");
            return null;
        }

        var rows = new List<RewardTableRowSnapshot>();
        foreach (var row in table.GetAllDataRows())
        {
            rows.Add(new RewardTableRowSnapshot
            {
                Currency = getCurrency(row),
                RewardRange = getRange(row),
            });
        }

        rows.Sort((a, b) => a.Currency.CompareTo(b.Currency));
        return rows;
    }

    static bool TryGetRowRange(float[] sourceRange, out float[] range)
    {
        range = sourceRange;
        return range != null && range.Length > 0;
    }

    static float RandomInRange(float[] range)
    {
        if (range == null || range.Length == 0)
            return 0f;

        float min = range[0];
        float max = range.Length > 1 ? range[1] : range[0];
        if (max < min)
            (min, max) = (max, min);

        if (Mathf.Approximately(min, max))
            return RoundReward(min);

        return RoundReward(UnityEngine.Random.Range(min, max));
    }

    static float RoundReward(float value)
    {
        if (value <= 0f)
            return 0f;

        int precision = GetRewardPrecision(value);
        return (float)Math.Round(value, precision, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 按数值量级决定保留小数位：>0.001 取 3 位，>0.0001 取 4 位，以此类推，最高 8 位。
    /// </summary>
    static int GetRewardPrecision(float value)
    {
        const int minPrecision = 3;
        const int maxPrecision = 8;
        const float baseThreshold = 0.001f;

        if (value > baseThreshold)
            return minPrecision;

        int precision = minPrecision;
        float threshold = baseThreshold;
        while (precision < maxPrecision)
        {
            threshold /= 10f;
            if (value > threshold)
                return precision + 1;
            precision++;
        }

        return maxPrecision;
    }

    struct RewardTableRowSnapshot
    {
        public float Currency;
        public float[] RewardRange;
    }
}
