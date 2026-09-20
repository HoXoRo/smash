using GameFramework;
using GameFramework.Event;

/// <summary>
/// 提现数据变更事件
/// </summary>
public class MakeupDataChangedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(MakeupDataChangedEventArgs).GetHashCode();
    public override int Id => EventId;
    
    /// <summary>
    /// 变更类型
    /// </summary>
    public MakeupDataChangeType ChangeType { get; private set; }
    
    /// <summary>
    /// 提现类型
    /// </summary>
    public MakeupType MakeupType { get; private set; }
    
    /// <summary>
    /// 提现ID
    /// </summary>
    public int MakeupId { get; private set; }
    
    /// <summary>
    /// 提现步骤
    /// </summary>
    public MakeupStep MakeupStep { get; private set; }

    public static MakeupDataChangedEventArgs Create(MakeupDataChangeType changeType, MakeupType makeupType = MakeupType.Mon, int makeupId = 0, MakeupStep makeupStep = MakeupStep.Step1)
    {
        var instance = ReferencePool.Acquire<MakeupDataChangedEventArgs>();
        instance.ChangeType = changeType;
        instance.MakeupType = makeupType;
        instance.MakeupId = makeupId;
        instance.MakeupStep = makeupStep;
        return instance;
    }

    public override void Clear()
    {
        ChangeType = MakeupDataChangeType.None;
        MakeupType = MakeupType.Mon;
        MakeupId = 0;
        MakeupStep = MakeupStep.Step1;
    }
}

/// <summary>
/// 提现数据变更类型
/// </summary>
public enum MakeupDataChangeType
{
    None = 0,
    Added,          // 新增提现数据
    Updated,        // 更新提现数据
    StepChanged,    // 步骤变更
    Completed,      // 完成
    Failed,         // 失败
    Removed         // 移除
} 