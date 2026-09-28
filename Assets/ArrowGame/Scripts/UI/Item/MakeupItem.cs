using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class MakeupItem : UIItemBase
{
    public void SetData(MakeupTask task)
    {
        if (task == null)
            return;

        if (varTxtLv != null)
            varTxtLv.text = task.Level.ToString();

        if (varTxtReward == null)
            return;

        float value = task.Mon > 0f ? task.Mon : task.Reward;
        varTxtReward.text = CommonHelper.GetDollarString(value);
    }
}
