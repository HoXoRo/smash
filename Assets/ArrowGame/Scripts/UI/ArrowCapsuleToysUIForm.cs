using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class ArrowCapsuleToysUIForm : UIFormBase
{
    // private PlayerDataModel m_PlayerData;
    // private VarAction m_OnCapsuleToysReward;
    // protected override void OnInit(object userData)
    // {
    //     base.OnInit(userData);
    // }
    // protected override void OnOpen(object userData)
    // {
    //     base.OnOpen(userData);
    //     m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
    //     m_OnCapsuleToysReward = Params.Get<VarAction>("OnCapsuleToysReward");
    //
    //     if (m_PlayerData.CapsuleToysCondition < GF.Config.GetInt("CapsuleToysCondition"))
    //     {
    //         GF.UI.Close(this.UIForm);
    //         return;
    //     }
    //     varNiudanSpine.AnimationState.SetAnimation(0, "animation", true);
    //     GF.Timer.AddTimer("CapsuleToysRewardGiftTimer", 4, OnCapsuleToysRewardGiftTimer);
    // }
    // private void OnCapsuleToysRewardGiftTimer()
    // {
    //     m_OnCapsuleToysReward?.Value?.Invoke();
    //     GF.UI.Close(this.UIForm);
    //     GF.Timer.RemoveTimer("CapsuleToysRewardGiftTimer");
    // }
    // protected override void OnClose(bool isShutdown, object userData)
    // {
    //     base.OnClose(isShutdown, userData);
    //     m_OnCapsuleToysReward = null;
    // }
}