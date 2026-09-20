using GameFramework;
using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 箭头成功消除时通知（用于 Combo 展示等）。
/// </summary>
public class ArrowMazeArrowEliminatedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ArrowMazeArrowEliminatedEventArgs).GetHashCode();
    public override int Id => EventId;

    public int ComboCount { get; private set; }
    public Vector3 WorldPosition { get; private set; }

    public static ArrowMazeArrowEliminatedEventArgs Create(int comboCount, Vector3 worldPosition)
    {
        var args = ReferencePool.Acquire<ArrowMazeArrowEliminatedEventArgs>();
        args.ComboCount = comboCount;
        args.WorldPosition = worldPosition;
        return args;
    }

    public override void Clear()
    {
        ComboCount = 0;
        WorldPosition = Vector3.zero;
    }
}
