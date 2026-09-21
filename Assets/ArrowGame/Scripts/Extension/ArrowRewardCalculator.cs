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
    const int FirstRewardRowId = 1;

    public static float CalculateReward(ArrowRewardType rewardType)
    {
        if (!CommonHelper.IsSpec())
            return DefaultNonSpecReward;

        return CalculateSpecReward(rewardType);
    }

    static float CalculateSpecReward(ArrowRewardType rewardType)
    {
        if (!TryGetFirstRowRange(rewardType, out float[] range))
            return 0f;

        return RandomInRange(range);
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
}
