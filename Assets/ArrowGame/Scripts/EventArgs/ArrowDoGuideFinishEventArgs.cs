using GameFramework.Event;
using GameFramework;
/// <summary>
/// 引导完成通知事件
/// </summary>
public class ArrowDoGuideFinishEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowDoGuideFinishEventArgs).GetHashCode();
    public override int Id => EventId;
    public int GuideId { get; private set; }
    public static ArrowDoGuideFinishEventArgs Create(int guideID)
    {
        var instance = ReferencePool.Acquire<ArrowDoGuideFinishEventArgs>();
        instance.GuideId = guideID;
        return instance;
    }
    public override void Clear()
    {
        GuideId = 0;
    }
}
