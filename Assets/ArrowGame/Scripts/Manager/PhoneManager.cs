using System;
using UnityEngine;
using UnityGameFramework.Runtime;

public class PhoneManager
{
    private static PhoneManager m_Instance;
    public static PhoneManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new PhoneManager();
            }
            return m_Instance;
        }
    }
    // Resolve the current profile on demand; PlayerDataModel owns persistence.
    private PlayerDataModel Player => GF.DataModel.GetOrCreate<PlayerDataModel>();
    public PhoneTask Task => GF.DataTable.GetDataTable<PhoneTask>()?.GetDataRow(1);

    private static int GetFragmentLimit(PlayerDataModel player, PhoneTask task)
    {
        return player.FirstLoginDate == DateTime.Now.ToString("yyyyMMdd")
            ? Mathf.Min(task.Fragment, task.FragmentFirstDayMax) : task.Fragment;
    }

    public int TryProducePhoneFragments()
    {
        var player = Player;
        var task = Task;
        Log.Info($"手机碎片任务: {task.Fragment}，任务状态：{player.PhoneStage}，当前碎片：{player.PhoneFragments}，当日上限：{GetFragmentLimit(player, task)}");
        if (task == null || player.PhoneStage != 1 || player.PhoneFragments >= GetFragmentLimit(player, task)) return 0;
        int rate = Mathf.Clamp(GF.Config.GetInt("PhoneFragmentRewardRate"), 0, 100);
        Log.Info($"手机碎片产出概率： {rate}");
        if (UnityEngine.Random.Range(0, 100) >= rate) return 0;
        string[] range = GF.Config.GetString("PhoneFragmentReward").Split(',');
        if (range.Length != 2 || !int.TryParse(range[0], out int min)
            || !int.TryParse(range[1], out int max) || min < 0 || max < min || max == int.MaxValue)
        {
            Log.Warning("Invalid PhoneFragmentReward configuration.");
            return 0;
        }
        int limit = GetFragmentLimit(player, task);
        int count = Mathf.Min(UnityEngine.Random.Range(min, max + 1), limit - player.PhoneFragments);
        if (count <= 0) return 0;
        player.PhoneFragments += count;
        player.Save();
        return count;
    }

    public bool IsPhoneFragmentLocked(PhoneTask task)
    {
        var player = Player;
        return task != null && player.FirstLoginDate == DateTime.Now.ToString("yyyyMMdd") && player.PhoneFragments >= task.FragmentFirstDayMax;
    }

    public bool SubmitPhoneAddress(PhoneTask task, PhoneAddressData address)
    {
        var player = Player;
        if (task == null || address == null || player.PhoneStage != 1 || player.PhoneFragments < task.Fragment) return false;
        player.PhoneAddress = address;
        player.PhoneStage = task.AdNum > 0 ? 2 : 3;
        player.PhoneAdCount = 0;
        player.PhoneAdCooldownEnd = 0;
        player.Save();
        return true;
    }

    public void CompletePhoneAd(PlayerDataModel player, PhoneTask task, int stage)
    {
        if (player == null || task == null || player.PhoneStage != stage) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (stage == 1)
        {
            if (player.PhoneFragments >= GetFragmentLimit(player, task) || now < player.PhoneFragmentCooldownEnd) return;
            int limit = GetFragmentLimit(player, task);
            player.PhoneFragments = (int)Math.Min(limit, (long)player.PhoneFragments + task.FragmentAd);
            player.PhoneFragmentCooldownEnd = now + task.FragmentAdCD;
        }
        else if (stage == 2)
        {
            if (now < player.PhoneAdCooldownEnd) return;
            player.PhoneAdCount = Mathf.Min(player.PhoneAdCount + 1, task.AdNum);
            player.PhoneAdCooldownEnd = now + task.AdCD;
            if (player.PhoneAdCount >= task.AdNum) player.PhoneStage = 3;
        }
        else return;
        player.Save();
    }

}
