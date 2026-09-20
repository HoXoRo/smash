using GameFramework;
using GameFramework.Event;


public class UserSkinChangeEventArgs : GameEventArgs
{
    /// <summary>
    /// 任务更新事件编号。
    /// </summary>
    public static readonly int EventId = typeof(UserSkinChangeEventArgs).GetHashCode();

    public override int Id
    {
        get
        {
            return EventId;
        }
    }

    public static UserSkinChangeEventArgs Create()
    {
        UserSkinChangeEventArgs UserSkinChangeEventArgs = ReferencePool.Acquire<UserSkinChangeEventArgs>();
        return UserSkinChangeEventArgs;
    }

    public override void Clear()
    {
    }
}
