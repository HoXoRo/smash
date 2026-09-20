using GameFramework;
using GameFramework.Event;

/// <summary>
/// 强制消除道具成功消除箭头时通知。
/// </summary>
public class ArrowMazeForceEliminateUsedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowMazeForceEliminateUsedEventArgs).GetHashCode();
    public override int Id => EventId;

    public static ArrowMazeForceEliminateUsedEventArgs Create()
    {
        return ReferencePool.Acquire<ArrowMazeForceEliminateUsedEventArgs>();
    }

    public override void Clear()
    {
    }
}
