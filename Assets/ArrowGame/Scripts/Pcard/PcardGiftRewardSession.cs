using System;
using System.Collections.Generic;

/// <summary>
/// 当前进程内的广告 Pcard 奖励。不会写入本地存储，应用重启后自动清空。
/// </summary>
public static class PcardGiftRewardSession
{
    private static readonly List<float> s_AdValues = new List<float>();

    public static event Action Changed;

    public static int Count => s_AdValues.Count;

    public static float TotalValue
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < s_AdValues.Count; i++)
            {
                total += s_AdValues[i];
            }

            return total;
        }
    }

    public static void Add(double adValue)
    {
        s_AdValues.Add((float)Math.Max(0d, adValue));
        Changed?.Invoke();
    }

    public static List<float> GetSnapshot()
    {
        return new List<float>(s_AdValues);
    }

    public static float Consume(int count)
    {
        int consumeCount = Math.Min(Math.Max(0, count), s_AdValues.Count);
        float total = 0f;
        for (int i = 0; i < consumeCount; i++)
        {
            total += s_AdValues[i];
        }

        if (consumeCount > 0)
        {
            s_AdValues.RemoveRange(0, consumeCount);
            Changed?.Invoke();
        }

        return total;
    }

    public static void Clear()
    {
        if (s_AdValues.Count == 0)
        {
            return;
        }

        s_AdValues.Clear();
        Changed?.Invoke();
    }
}
