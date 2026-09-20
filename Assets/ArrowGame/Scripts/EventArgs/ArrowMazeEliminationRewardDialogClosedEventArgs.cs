using GameFramework;
using GameFramework.Event;

/// <summary>
/// 连消奖励弹窗关闭后通知，用于衔接刮刮卡与通关流程。
/// </summary>
public class ArrowMazeEliminationRewardDialogClosedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowMazeEliminationRewardDialogClosedEventArgs).GetHashCode();
    public override int Id => EventId;

    public static ArrowMazeEliminationRewardDialogClosedEventArgs Create()
    {
        return ReferencePool.Acquire<ArrowMazeEliminationRewardDialogClosedEventArgs>();
    }

    public override void Clear()
    {
    }
}
