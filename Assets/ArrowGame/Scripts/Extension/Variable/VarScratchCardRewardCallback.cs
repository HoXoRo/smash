using GameFramework;
using System;

public class VarScratchCardRewardCallback : Variable<Action<ScratchCardRewardInfo>>
{
    public VarScratchCardRewardCallback()
    {
    }

    public static implicit operator VarScratchCardRewardCallback(Action<ScratchCardRewardInfo> value)
    {
        VarScratchCardRewardCallback varValue = ReferencePool.Acquire<VarScratchCardRewardCallback>();
        varValue.Value = value;
        return varValue;
    }

    public static implicit operator Action<ScratchCardRewardInfo>(VarScratchCardRewardCallback value)
    {
        return value.Value;
    }
}
