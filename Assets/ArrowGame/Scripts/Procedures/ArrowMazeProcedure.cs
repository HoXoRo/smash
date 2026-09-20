using Cysharp.Threading.Tasks;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using ArrowMaze;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
/// <summary>
/// 箭头迷宫游戏流程
/// </summary>
public class ArrowMazeProcedure : ProcedureBase
{
    private int m_ArrowMazeUIFormId = -1; // 主游戏UI表单ID
    private bool m_IsGameStarted = false;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        Log.Info("进入箭头迷宫游戏流程");

        // 订阅游戏状态事件
        SubscribeGameEvents();

        // 加载游戏场景
        LoadGameScene();
    }

    private async void LoadGameScene()
    {
        try
        {
            // 加载游戏场景
            await GF.Scene.LoadSceneAwait(UtilityBuiltin.AssetsPath.GetScenePath("ArrowMazeGameScene"));

            Log.Info("场景加载完成，初始化游戏管理器");

            // 初始化游戏管理器
            ArrowMazeManager.Instance.Initialize();

            // 同步相机设置
            var cameraController = ArrowMazeManager.Instance.CameraController;
            if (cameraController != null)
            {
                cameraController.SyncWithUICamera();
                var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
                cameraController.SetDayNightMode(playerDm != null && playerDm.IsNightMode);
            }

            // 打开主游戏UI，待 UI 就绪后再隐藏 Loading，避免白屏闪烁
            await OpenMainUIAsync();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            GF.BuiltinView.HideLoadingProgress();

            m_IsGameStarted = true;
        }
        catch (System.Exception e)
        {
            Log.Error($"加载场景失败: {e.Message}");
            GF.BuiltinView.HideLoadingProgress();
        }
    }

    /// <summary>
    /// 打开主游戏UI
    /// </summary>
    private async UniTask OpenMainUIAsync()
    {
        if (m_ArrowMazeUIFormId >= 0 && GF.UI.HasUIForm(m_ArrowMazeUIFormId))
            return;

        var uiForm = await GF.UI.OpenUIFormAwait(UIViews.ArrowMazeUIForm);
        if (uiForm != null)
        {
            m_ArrowMazeUIFormId = uiForm.UIForm.SerialId;
            Log.Info($"打开主游戏UI: {m_ArrowMazeUIFormId}");
        }
    }

    /// <summary>
    /// 订阅游戏状态事件
    /// </summary>
    private void SubscribeGameEvents()
    {
        // GF.Event.Subscribe(ArrowMazeGameWinEventArgs.EventId, OnGameWin);
        // GF.Event.Subscribe(ArrowMazeGameOverEventArgs.EventId, OnGameOver);
        // GF.Event.Subscribe(ArrowMazeArrowHitEventArgs.EventId, OnArrowHit);
    }

    /// <summary>
    /// 取消订阅游戏状态事件
    /// </summary>
    private void UnsubscribeGameEvents()
    {
        // GF.Event.Unsubscribe(ArrowMazeGameWinEventArgs.EventId, OnGameWin);
        // GF.Event.Unsubscribe(ArrowMazeGameOverEventArgs.EventId, OnGameOver);
        // GF.Event.Unsubscribe(ArrowMazeArrowHitEventArgs.EventId, OnArrowHit);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        if (!m_IsGameStarted) return;

    }



    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        base.OnLeave(procedureOwner, isShutdown);

        // 取消订阅事件
        UnsubscribeGameEvents();

        // 关闭所有UI表单
        CloseAllUIForms();

        // 清理资源（场景卸载时会自动销毁GameObject）
        if (ArrowMazeManager.Instance != null)
        {
            ArrowMazeManager.Instance.Cleanup();
        }

        m_IsGameStarted = false;
    }

    /// <summary>
    /// 关闭所有UI表单
    /// </summary>
    private void CloseAllUIForms()
    {
        // 关闭主游戏UI
        if (m_ArrowMazeUIFormId >= 0 && GF.UI.HasUIForm(m_ArrowMazeUIFormId))
        {
            GF.UI.Close(m_ArrowMazeUIFormId);
            m_ArrowMazeUIFormId = -1;
        }
    }
}

