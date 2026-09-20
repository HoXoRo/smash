using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class ArrowMazeOverUIForm : UIFormBase
{
    private VarAction m_OnRestartAction;
    private VarAction m_OnReviveAction;
    private VarInt32 m_FailureType;
    private const string AD_REVIVE_PLACEMENT = "arrow_maze_revive";

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_OnRestartAction = Params.Get<VarAction>("OnRestart") ?? Params.Get<VarAction>("OnClose");
        m_OnReviveAction = Params.Get<VarAction>("OnRevive");
        m_FailureType = Params.Get<VarInt32>("FailureType");
        var gameProgres = Params.Get<VarFloat>("GameProgress");
        varLevelProgressBar.fillAmount = gameProgres;
        varProgressTxt.text = $"{gameProgres * 100}%";
        GF.Sound.PlayEffect("arrowPlay/over.mp3");
        
        if (m_FailureType == (int)ArrowMazeFailureType.WatermelonEscaped)
        {
            varButton_Revive.SetActive(false);
        }
        else
        {
            varButton_Revive.SetActive(true);
        }
        
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        if (btId == "Button_Close")
        {
            m_OnRestartAction?.Value?.Invoke();
            GF.UI.Close(this.UIForm);
        }
        else if (btId == "Button_Revive")
        {
            CommonHelper.ShowVideoAd(AD_REVIVE_PLACEMENT, isOk =>
            {
                if (isOk)
                {
                    m_OnReviveAction?.Value?.Invoke();
                    GF.UI.Close(this.UIForm);
                }
            });

            
        }
    }
}