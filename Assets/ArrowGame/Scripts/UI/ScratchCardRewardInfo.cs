using System;

/// <summary>
/// 刮刮卡结算信息。
/// </summary>
public class ScratchCardRewardInfo
{
    public bool IsWin;
    /// <summary> 中奖目标图标编号（1~8，对应 winIcon / UI/ScratchCard/{n}.png）。 </summary>
    public int WinIconIndex;
    /// <summary> 与 winIcon 相同的格子数量（1~3）。 </summary>
    public int MatchCount;
    /// <summary> 传入界面的总奖励值。 </summary>
    public float TotalRewardValue;
    /// <summary> 8 个刮开格 txt 展示数值；中奖格为分配奖励，未中奖格为 50~500 随机展示值。 </summary>
    public float[] SlotRewardValues;

    public static ScratchCardRewardInfo Create(
        bool isWin,
        int winIconIndex,
        int matchCount,
        float totalRewardValue,
        float[] slotRewardValues)
    {
        return new ScratchCardRewardInfo
        {
            IsWin = isWin,
            WinIconIndex = winIconIndex,
            MatchCount = matchCount,
            TotalRewardValue = totalRewardValue,
            SlotRewardValues = slotRewardValues
        };
    }
}
