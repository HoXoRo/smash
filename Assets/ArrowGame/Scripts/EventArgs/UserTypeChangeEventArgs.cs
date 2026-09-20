using GameFramework;
using GameFramework.Event;


public class UserTypeChangeEventArgs : GameEventArgs
{
    /// <summary>
    /// 任务更新事件编号。
    /// </summary>
    public static readonly int EventId = typeof(UserTypeChangeEventArgs).GetHashCode();

    public override int Id
    {
        get
        {
            return EventId;
        }
    }

    public static UserTypeChangeEventArgs Create()
    {
        UserTypeChangeEventArgs UserTypeChangeEventArgs = ReferencePool.Acquire<UserTypeChangeEventArgs>();
        return UserTypeChangeEventArgs;
    }

    public override void Clear()
    {
    }
}
