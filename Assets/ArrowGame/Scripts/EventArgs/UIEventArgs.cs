using GameFramework.Event;
using UnityEngine;
using GameFramework;

namespace UnityGameFramework.Runtime
{
    public class RewardCollectedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(RewardCollectedEventArgs).GetHashCode();
        public override int Id => EventId;
        public PlayerDataType type;
        public float Value { get; private set; }
        public Vector3 WorldPosition { get; private set; }

        public static RewardCollectedEventArgs Create(float value, Vector3 worldPosition,PlayerDataType type)
        {
            var e = ReferencePool.Acquire<RewardCollectedEventArgs>();
            e.Value = value;
            e.WorldPosition = worldPosition;
            e.type = type;
            return e;
        }

        public override void Clear()
        {
            Value = 0f;
            WorldPosition = Vector3.zero;
        }
    }

    public class LargeDollarRewardEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LargeDollarRewardEventArgs).GetHashCode();
        public override int Id => EventId;

        public float Value { get; private set; }
        public Vector3 WorldPosition { get; private set; }

        public static LargeDollarRewardEventArgs Create(float value, Vector3 worldPosition)
        {
            var e = ReferencePool.Acquire<LargeDollarRewardEventArgs>();
            e.Value = value;
            e.WorldPosition = worldPosition;
            return e;
        }

        public override void Clear()
        {
            Value = 0f;
            WorldPosition = Vector3.zero;
        }
    }

    public class TreasureBoxRewardEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(TreasureBoxRewardEventArgs).GetHashCode();
        public override int Id => EventId;

        public float Value { get; private set; }
        public Vector3 WorldPosition { get; private set; }

        public static TreasureBoxRewardEventArgs Create(float value, Vector3 worldPosition)
        {
            var e = ReferencePool.Acquire<TreasureBoxRewardEventArgs>();
            e.Value = value;
            e.WorldPosition = worldPosition;
            return e;
        }

        public override void Clear()
        {
            Value = 0f;
            WorldPosition = Vector3.zero;
        }
    }
} 