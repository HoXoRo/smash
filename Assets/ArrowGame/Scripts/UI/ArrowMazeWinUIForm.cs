using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class ArrowMazeWinUIForm : UIFormBase
{
    private VarAction m_OnCloseAction;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        if (Params.Get<VarAction>("OnClose") != null)
        {
            m_OnCloseAction = Params.Get<VarAction>("OnClose");
        }
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        if (btId == "Button_Close")
        {
            m_OnCloseAction?.Value?.Invoke();
            GF.UI.Close(this.UIForm);
        }
    }
}