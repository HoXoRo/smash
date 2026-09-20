using GameFramework;
using GameFramework.Event;


public class WatchVideoFinishEventArgs : GameEventArgs
{
    /// <summary>
    /// 看视频事件编号。
    /// </summary>
    public static readonly int EventId = typeof(WatchVideoFinishEventArgs).GetHashCode();

    public override int Id
    {
        get
        {
            return EventId;
        }
    }

    public static WatchVideoFinishEventArgs Create()
    {
        WatchVideoFinishEventArgs watchVideoFinishEventArgs = ReferencePool.Acquire<WatchVideoFinishEventArgs>();
        return watchVideoFinishEventArgs;
    }

    public override void Clear()
    {
    }
}
