using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class ArrowMakeupGuideUIForm : UIFormBase
{
    private PlayerDataModel m_PlayerData;
    private float m_FireWinReward;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        m_FireWinReward = GF.Config.GetFloat("FireWinReward");
    }

    protected override void OnOpenAnimationComplete()
    {
        base.OnOpenAnimationComplete();
        if (varButton_Makeup == null)
            return;

        Canvas.ForceUpdateCanvases();
        GF.GuideManager.ShowGuide(2, varButton_Makeup.transform, true, () => { OnButtonClick(this, "Button_Makeup"); });
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown,userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        if (btId == "Button_Makeup")
        {
            GF.UI.Close(this.UIForm);
            GF.Event.FireNow(this, RewardCollectedEventArgs.Create(m_FireWinReward, varButton_Makeup.transform.position, PlayerDataType.Diamond));
            GF.Event.Fire(null, ArrowDoGuideEventArgs.Create(3));
        }
    }
}