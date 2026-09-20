using GameFramework;
using GameFramework.Event;


public class MakeupTaskChangeEventArgs : GameEventArgs
{

    /// <summary>
    /// 兑换任务修改事件编号。
    /// </summary>
    public static readonly int EventId = typeof(MakeupTaskChangeEventArgs).GetHashCode();

    public override int Id
    {
        get
        {
            return EventId;
        }
    }
    public float Value { get; private set; }
    public static MakeupTaskChangeEventArgs Create(float value = 0f)
    {
        MakeupTaskChangeEventArgs makeupTaskChangeEventArgs = ReferencePool.Acquire<MakeupTaskChangeEventArgs>();
        makeupTaskChangeEventArgs.Value = value;
        return makeupTaskChangeEventArgs;
    }

    public override void Clear()
    {
    }
}
