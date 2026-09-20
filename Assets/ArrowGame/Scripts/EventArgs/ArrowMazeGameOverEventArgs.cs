using GameFramework.Event;
using GameFramework;

/// <summary>
/// 游戏失败类型
/// </summary>
public enum ArrowMazeFailureType
{
    /// <summary>时间耗尽（倒计时归零）</summary>
    TimeOver,
    /// <summary>生命值归零</summary>
    LivesOver,
    /// <summary>西瓜进洞且场上仍有箭头</summary>
    WatermelonEscaped
}

/// <summary>
/// 游戏失败通知事件
/// </summary>
public class ArrowMazeGameOverEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowMazeGameOverEventArgs).GetHashCode();
    public override int Id => EventId;

    public ArrowMazeFailureType FailureType { get; private set; }

    public static ArrowMazeGameOverEventArgs Create(ArrowMazeFailureType failureType = ArrowMazeFailureType.LivesOver)
    {
        var instance = ReferencePool.Acquire<ArrowMazeGameOverEventArgs>();
        instance.FailureType = failureType;
        return instance;
    }
    public override void Clear()
    {
        FailureType = ArrowMazeFailureType.LivesOver;
    }
}
