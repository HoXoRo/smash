using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
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
        ShowLevel();
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
        GF.BuiltinView.SetLoadingProgress(1f);
        GF.BuiltinView.HideLoadingProgress();
        ToHome();
    }

    public void ToHome()
    {
        menuUIFormId = GF.UI.OpenUIForm(UIViews.HallTabUIForm);
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
