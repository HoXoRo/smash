using GameFramework.Event;
using GameFramework;

/// <summary>
/// 背景日/夜间模式切换通知事件
/// </summary>
public class BgSkinChangeEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(BgSkinChangeEventArgs).GetHashCode();
    public override int Id => EventId;

    /// <summary>true=夜间(#232633)，false=日间(#EBEBEB)</summary>
    public bool IsNightMode { get; private set; }

    public static BgSkinChangeEventArgs Create(bool isNightMode)
    {
        var instance = ReferencePool.Acquire<BgSkinChangeEventArgs>();
        instance.IsNightMode = isNightMode;
        return instance;
    }

    public override void Clear()
    {
        IsNightMode = false;
    }
}
