using GameFramework.Event;
using GameFramework;
/// <summary>
/// 玩家数据改变通知事件
/// </summary>
public class ArrowMazeGameWinEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowMazeGameWinEventArgs).GetHashCode();
    public override int Id => EventId;

    public static ArrowMazeGameWinEventArgs Create()
    {
        var instance = ReferencePool.Acquire<ArrowMazeGameWinEventArgs>();

        return instance;
    }
    public override void Clear()
    {
    }
}
