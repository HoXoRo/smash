using GameFramework;
using GameFramework.Event;


public class MahJongLanguageChangeEventArgs : GameEventArgs
{
    /// <summary>
    /// 语言更新事件编号。
    /// </summary>
    public static readonly int EventId = typeof(MahJongLanguageChangeEventArgs).GetHashCode();

    public override int Id
    {
        get
        {
            return EventId;
        }
    }

    public static MahJongLanguageChangeEventArgs Create()
    {
        MahJongLanguageChangeEventArgs MahJongLanguageChangeEventArgs = ReferencePool.Acquire<MahJongLanguageChangeEventArgs>();
        return MahJongLanguageChangeEventArgs;
    }

    public override void Clear()
    {
    }
}
