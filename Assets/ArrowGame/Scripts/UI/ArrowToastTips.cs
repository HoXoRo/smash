using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/ArrowToastTips")]
public partial class ArrowToastTips : UIFormBase
{
    public const string P_Duration = "Duration";
    public const string P_Text = "Text";
    public const string P_Style = "Style";

    float m_Duration = 1f;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        varContentText.text = Params.Get<VarString>(P_Text);
    }
    protected override void OnOpenAnimationComplete()
    {
        base.OnOpenAnimationComplete();
        ScheduleStart();
    }

    private void ScheduleStart()
    {
        UniTask.Delay(TimeSpan.FromSeconds(m_Duration), true).ContinueWith(() =>
        {
            GF.UI.Close(this.UIForm);
        }).Forget();
    }
}
