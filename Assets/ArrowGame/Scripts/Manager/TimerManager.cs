using GameFramework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 计时器数据类
/// </summary>
public class TimerData
{
    public string TimerId { get; private set; }
    public float Interval { get; private set; }  // 间隔时间（秒）
    public long LastTriggerTime { get; set; }   // 上次触发时间戳
    public Action OnTrigger { get; private set; } // 触发回调
    public bool IsActive { get; set; }           // 是否激活
    public Action<float> OnTriggerWithDeltaTime { get; private set; } // 间隔Interval秒触发回调

    public TimerData(string timerId, float interval, Action onTrigger)
    {
        TimerId = timerId;
        Interval = interval;
        OnTrigger = onTrigger;
        IsActive = true;
    }
    public TimerData(string timerId, float interval, Action<float> onTrigger)
    {
        TimerId = timerId;
        Interval = interval;
        OnTriggerWithDeltaTime = onTrigger;
        IsActive = true;
    }
}

/// <summary>
/// 计时管理器
/// </summary>
public class TimerManager
{
    private static TimerManager s_Instance;
    public static TimerManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new TimerManager();
            }
            return s_Instance;
        }
    }

    private Dictionary<string, TimerData> m_Timers = new Dictionary<string, TimerData>();
    private List<string> m_TimersToRemove = new List<string>(); // 待移除的计时器列表
    private List<TimerData> m_TimersSnapshot = new List<TimerData>(); // 复用，避免每帧 new 造成 GC
    private float m_GameStartTime;  // 游戏启动时间

    private TimerManager()
    {
        m_GameStartTime = Time.realtimeSinceStartup;
    }

    public void Update()
    {
        long currentTime = GetCurrentTime();
        
        // 先处理待移除的计时器
        if (m_TimersToRemove.Count > 0)
        {
            foreach (var timerId in m_TimersToRemove)
            {
                m_Timers.Remove(timerId);
            }
            m_TimersToRemove.Clear();
        }

        // 复用列表做快照，避免回调中 AddTimer/RemoveTimer 导致 "Collection was modified"，且不每帧 new
        m_TimersSnapshot.Clear();
        m_TimersSnapshot.AddRange(m_Timers.Values);
        foreach (var timer in m_TimersSnapshot)
        {
            if (!timer.IsActive) continue;

            if (currentTime - timer.LastTriggerTime >= (long)timer.Interval)
            {
                timer.OnTrigger?.Invoke();
                timer.LastTriggerTime = currentTime;
                timer.OnTriggerWithDeltaTime?.Invoke(timer.Interval);
            }
        }
    }

    /// <summary>
    /// 获取当前时间戳（秒）
    /// </summary>
    public long GetCurrentTime()
    {
        return GetTimeStamp();
    }

    /// <summary>
    /// 获取游戏运行时间（秒）
    /// </summary>
    public float GetGameRunningTime()
    {
        return Time.realtimeSinceStartup - m_GameStartTime;
    }


    /// <summary>
    /// 添加计时器
    /// </summary>
    public void AddTimer(string timerId, float interval, Action onTrigger)
    {
        if (m_Timers.ContainsKey(timerId))
        {
            Log.Warning($"Timer {timerId} already exists!");
            return;
        }

        var timer = new TimerData(timerId, interval, onTrigger);
        timer.LastTriggerTime = GetCurrentTime();
        m_Timers.Add(timerId, timer);
    }
    /// <summary>
    /// 添加计时器
    /// </summary>
    public void AddTimer(string timerId, float interval, Action<float> onTrigger)
    {
        if (m_Timers.ContainsKey(timerId))
        {
            Log.Warning($"Timer {timerId} already exists!");
            return;
        }

        var timer = new TimerData(timerId, interval, onTrigger);
        timer.LastTriggerTime = GetCurrentTime();
        m_Timers.Add(timerId, timer);
    }

    /// <summary>
    /// 移除计时器（延迟到下一帧Update开始时执行，避免在遍历时修改集合）
    /// </summary>
    public void RemoveTimer(string timerId)
    {
        if (!m_TimersToRemove.Contains(timerId))
        {
            m_TimersToRemove.Add(timerId);
        }
    }

    /// <summary>
    /// 暂停计时器
    /// </summary>
    public void PauseTimer(string timerId,bool isClean = false)
    {
        if (m_Timers.TryGetValue(timerId, out var timer))
        {
            timer.IsActive = false;
        }
    }

    /// <summary>
    /// 恢复计时器
    /// </summary>
    public void ResumeTimer(string timerId,bool isClean = true)
    {
        if (m_Timers.TryGetValue(timerId, out var timer))
        {
            timer.IsActive = true;
            if(isClean)
            timer.LastTriggerTime = GetCurrentTime();
        }
    }

    /// <summary>
    /// 获取计时器剩余时间（秒）
    /// </summary>
    public float GetTimerRemainingTime(string timerId)
    {
        if (m_Timers.TryGetValue(timerId, out var timer) && timer.IsActive)
        {
            float elapsed = GetCurrentTime() - timer.LastTriggerTime;
            return Mathf.Max(0, timer.Interval - elapsed);
        }
        return 0;
    }

    public static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }
} 