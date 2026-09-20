using GameFramework;
using GameFramework.Event;

public enum GFEventType
{
    ApplicationQuit,//游戏退出
    AppOpenMenu//打开主菜单（配置存档初始化完成）
}
public class GFEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(GFEventArgs).GetHashCode();
    public override int Id => EventId;
    public GFEventType EventType { get; private set; }
    public object UserData { get; private set; }
    public override void Clear()
    {
        UserData = null;
    }
    public static GFEventArgs Create(GFEventType eventType, object userDt = null)
    {
        var instance = ReferencePool.Acquire<GFEventArgs>();
        instance.EventType = eventType;
        instance.UserData = userDt;
        return instance;
    }
}
