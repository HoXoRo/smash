using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameStartMainProcedure : ProcedureBase
{
    int menuUIFormId = -1;

    IFsm<IProcedureManager> procedure;
    protected override void OnInit(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnInit(procedureOwner);
    }
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        procedure = procedureOwner;
        ShowLevel();//加载关卡
        //var res = await GF.WebRequest.AddWebRequestAsync("https://blog.csdn.net/final5788");
        //Log.Info(Utility.Converter.GetString(res.Bytes));
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
    }
    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (!isShutdown && menuUIFormId != -1)
        {
            GF.UI.CloseUIForm(menuUIFormId);
        }
        base.OnLeave(procedureOwner, isShutdown);
    }
    public void EnterGame()
    {
        // ChangeState<GameProcedure>(procedure);
    }
    public void ShowLevel()
    {
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }
        GF.UI.CloseAllLoadingUIForms();
        GF.UI.CloseAllLoadedUIForms();
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();



        // 检查是否需要弹出签到
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerDm.RefreshNewDay();
        playerDm.RefreshSignin();

        // 处理归因信息并设置用户类型
        CommonHelper.ApplySpecStatusUserType();
        if (playerDm != null && playerDm.IsRecognition)
        {
            Log.Info($"===> 用户类型已识别过，跳过更新。当前类型: {(playerDm.UserType == 0 ? "自然量用户" : "非自然量用户")}");
        }
#if UNITY_EDITOR
        // playerDm.LevelId = 1;
        // playerDm.CompleteGuideIds.Clear();
        // playerDm.ArrowAdvancedSkinId = 0;
        // playerDm.CapsuleToysCondition = 20;
#endif

        GF.Sound.PlayBGM("bgm/bgm.mp3");
        GF.Event.FireNow(this, GFEventArgs.Create(GFEventType.AppOpenMenu));
        if (playerDm.LastBubbleTime == "0")
        {
            playerDm.LastBubbleTime = TimerManager.Instance.GetCurrentTime().ToString();
        }

        // if (CommonHelper.IsSpec() && !GuideManager.Instance.IsGuideCompleted(0))
        // {
        //     StartGame();
        // }
        // else if (CommonHelper.IsSpec())
        // {
        //     //异步打开主菜单UI
        //     menuUIFormId = GF.UI.OpenUIForm(UIViews.MahjongMenuUIForm);
        // }
        // else
        // {
        // GF.Sound.PlayBGM("flower/flowerBgm.mp3");
        // GF.UI.OpenUIForm(UIViews.FlowerGameUIForm);
        // }
        ChangeState<ArrowMazeProcedure>(procedure);
    }
    public void ToHome()
    {
        // GF.UI.OpenUIForm(UIViews.MahjongGameTransitionUIForm);
        // // 检查是否需要弹出签到
        // var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        // playerDm.RefreshNewDay();
        // if (menuUIFormId == -1)
        // {
        //     //异步打开主菜单UI
        //     menuUIFormId = GF.UI.OpenUIForm(UIViews.MahjongMenuUIForm);
        // }
    }
    public void StartGame(bool IsEdit = false, int levelId = -1)
    {
        // if (menuUIFormId != -1)
        // {
        //     GF.UI.Close(menuUIFormId);
        //     menuUIFormId = -1;
        // }
        // if (IsEdit)
        // {
        //     // GF.UI.OpenUIForm(UIViews.GameTransitionUIForm);
        //     GF.UI.OpenUIForm(UIViews.MahjongGameLevelEdit);
        //     return;
        // }

        // if(!CommonHelper.IsSpec())
        // {
        //     GF.UI.OpenUIForm(UIViews.FlowerGameUIForm);
        //     return;
        // }
        // var startUIparm = UIParams.Create();
        // startUIparm.Set<VarBoolean>("IsHasTransition", IsEdit);
        // startUIparm.Set<VarInt32>("LevelId", levelId == -1 ? GF.DataModel.GetOrCreate<PlayerDataModel>().LevelId : levelId);
        // GF.UI.OpenUIForm(UIViews.MahjongGameUIForm, startUIparm);
    }
}
