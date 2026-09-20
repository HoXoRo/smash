using GameFramework.Event;
using GameFramework;
/// <summary>
/// 玩家数据改变通知事件
/// </summary>
public class ArrowMazeArrowHitEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowMazeArrowHitEventArgs).GetHashCode();
    public override int Id => EventId;
    public int Lives { get; private set; }
    public static ArrowMazeArrowHitEventArgs Create(int lives)
    {
        var instance = ReferencePool.Acquire<ArrowMazeArrowHitEventArgs>();
        instance.Lives = lives;
        return instance;
    }
    public override void Clear()
    {
        Lives = 0;
    }
}
