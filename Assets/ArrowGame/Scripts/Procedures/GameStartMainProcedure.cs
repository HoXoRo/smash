using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
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
    private bool _isLoadingGameplay;

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
        if (_isLoadingGameplay) return;
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }
        GF.UI.CloseAllLoadingUIForms();
        GF.UI.CloseAllLoadedUIForms();
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();
        LoadGameplayAsync().Forget();
    }

    private async UniTask LoadGameplayAsync()
    {
        _isLoadingGameplay = true;
        try
        {
            Time.timeScale = 1f;
            GF.BuiltinView.SetLoadingProgress(0.9f);
            // Keep the launch scene's framework services alive.
            var operation = SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Additive);
            if (operation == null)
            {
                throw new System.InvalidOperationException("Unable to load Gameplay scene.");
            }
            await operation;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Gameplay"));
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            GF.BuiltinView.SetLoadingProgress(1f);
            GF.BuiltinView.HideLoadingProgress();
        }
        catch (System.Exception exception)
        {
            Log.Error($"Failed to load Gameplay: {exception}");
        }
        finally
        {
            _isLoadingGameplay = false;
        }
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
