using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/LoadingUIForm")]
public partial class LoadingUIForm : UIFormBase
{
    public const string P_Duration = "Duration";
    public const string P_OnClosed = "OnClosed";
    public const string P_ManualClose = "ManualClose";

    private float remainingTime;
    private bool isClosing;
    private bool manualClose;
    private Action onClosed;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        remainingTime = Mathf.Max(0f, Params.Get<VarSingle>(P_Duration)?.Value ?? 0f);
        onClosed = Params.Get<VarAction>(P_OnClosed)?.Value;
        manualClose = Params.Get<VarBoolean>(P_ManualClose)?.Value ?? false;
        isClosing = false;
        varLoading.transform.localRotation = Quaternion.identity;
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (isClosing) return;
        varLoading.transform.Rotate(0f, 0f, -360f * realElapseSeconds);
        if (manualClose) return;
        remainingTime -= realElapseSeconds;
        if (remainingTime > 0f) return;
        isClosing = true;
        GF.UI.CloseUIForm(Id);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        isClosing = true;
        Action callback = onClosed;
        onClosed = null;
        base.OnClose(isShutdown, userData);
        if (!isShutdown && callback != null) callback();
    }
    
}
