using System;
using System.Globalization;
using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class MakeupRecordItem : UIItemBase
{
    private int total, firstMin, firstMax, addMin, addMax;
    private long displayedSecond = -1;
    private long displayedHour = -1;

    private void OnEnable()
    {
        total = Math.Max(0, GF.Config.GetInt("BonusesPoolTotal"));
        BonusesPoolCalculator.ParseRange(GF.Config.GetString("BonusesPoolFirst"), out firstMin, out firstMax);
        BonusesPoolCalculator.ParseRange(GF.Config.GetString("BonusesPoolAdd"), out addMin, out addMax);
        varProgress.interactable = false;
        varProgress.minValue = 0f;
        varProgress.maxValue = 1f;
        varProgress.wholeNumbers = false;
        displayedSecond = displayedHour = -1;
        RefreshDisplay();
    }

    private void Update()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        DateTime now = DateTime.Now;
        long second = now.Ticks / TimeSpan.TicksPerSecond;
        if (displayedSecond == second) return;
        displayedSecond = second;
        varTxtTime.text = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        long hour = now.Ticks / TimeSpan.TicksPerHour;
        if (displayedHour == hour) return;
        displayedHour = hour;
        int amount = BonusesPoolCalculator.Calculate(now, total, firstMin, firstMax, addMin, addMax);
        varProgress.SetValueWithoutNotify(total > 0 ? (float)amount / total : 0f);
        varTxtProgress.text = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", amount, total);
    }
}

// Same config and local date/hour produce identical amounts, without saved state or requests.
internal static class BonusesPoolCalculator
{
    internal static void ParseRange(string value, out int min, out int max)
    {
        string[] parts = value.Split(',');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out min) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out max) ||
            min < 0 || max < min)
            throw new FormatException("BonusesPool range must be non-negative min,max: " + value);
    }

    internal static int Calculate(DateTime localTime, int total, int firstMin, int firstMax, int addMin, int addMax)
    {
        if (total <= 0) return 0;
        uint state = (uint)(localTime.Year * 10000 + localTime.Month * 100 + localTime.Day);
        long amount = NextAmount(ref state, firstMin, firstMax);
        // Midnight contains only the initial amount; 01:00 is the first increment.
        for (int hour = 1; hour <= localTime.Hour; hour++)
            amount += NextAmount(ref state, addMin, addMax);
        return (int)Math.Min(amount, total);
    }

    private static int NextAmount(ref uint state, int min, int max)
    {
        // Explicit integer PRNG is stable across Mono/IL2CPP/.NET versions.
        unchecked
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
        }
        return (int)(min + (long)(state % ((ulong)((long)max - min) + 1UL)));
    }
}
