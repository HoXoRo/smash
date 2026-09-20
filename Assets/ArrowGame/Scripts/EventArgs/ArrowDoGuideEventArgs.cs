using GameFramework.Event;
using GameFramework;
/// <summary>
/// 引导通知事件
/// </summary>
public class ArrowDoGuideEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowDoGuideEventArgs).GetHashCode();
    public override int Id => EventId;
    public int GuideId { get; private set; }
    public static ArrowDoGuideEventArgs Create(int guideID)
    {
        var instance = ReferencePool.Acquire<ArrowDoGuideEventArgs>();
        instance.GuideId = guideID;
        return instance;
    }
    public override void Clear()
    {
        GuideId = 0;
    }
}
