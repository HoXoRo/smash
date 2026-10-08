using UnityEngine;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif

public partial class PcardRewardItem : UIItemBase
{
    public RectTransform RectTransform { get; private set; }

    protected override void OnInit()
    {
        base.OnInit();
        RectTransform = transform as RectTransform;
    }

    public void SetValue(float value)
    {
        if (varTxtValue != null)
        {
            varTxtValue.text = CommonHelper.GetDollarString(value, value >= 0.01f);
        }
    }
}
