using GameFramework;
using GameFramework.Event;


public class TaskUpdateEventArgs : GameEventArgs
{
    /// <summary>
    /// 任务更新事件编号。
    /// </summary>
    public static readonly int EventId = typeof(TaskUpdateEventArgs).GetHashCode();

    public override int Id
    {
        get
        {
            return EventId;
        }
    }

    public static TaskUpdateEventArgs Create()
    {
        TaskUpdateEventArgs TaskUpdateEventArgs = ReferencePool.Acquire<TaskUpdateEventArgs>();
        return TaskUpdateEventArgs;
    }

    public override void Clear()
    {
    }
}
