using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 引导管理器
/// </summary>
public class GuideManager
{
    private static GuideManager m_Instance;
    public static GuideManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new GuideManager();
            }
            return m_Instance;
        }
    }



    /// <summary>
    /// 显示引导
    /// </summary>
    /// <param name="guideId">引导ID</param>
    /// <param name="targetTransform">目标Transform</param>
    /// <param name="showFinger">是否显示手指</param>
    /// <param name="onComplete">完成回调</param>
    public void ShowGuide(int guideId, Transform targetTransform, bool showFinger = true, Action onComplete = null, Vector2 fingerPos = default)
    {
        // 检查是否已经完成过
        if (IsGuideCompleted(guideId))
        {
            Log.Info($"引导 {guideId} 已完成，跳过显示");
            onComplete?.Invoke();
            return;
        }

        var guideData = new GuideUIFormData(guideId, showFinger, targetTransform, onComplete);
        var uiParams = UIParams.Create();
        uiParams.Set<VarInt32>("GuideId", guideId);
        uiParams.Set<VarBoolean>("ShowFinger", showFinger);
        uiParams.Set<VarTransform>("TargetTransform", targetTransform);
        uiParams.Set<VarVector2>("FingerPos", fingerPos);
        VarAction varAction = GameFramework.ReferencePool.Acquire<VarAction>();
        varAction.Value = onComplete;
        uiParams.Set<VarAction>("OnComplete", varAction);
        Log.Info($"触发引导 {guideId} ，目标位置：{targetTransform.position}，手指位置：{fingerPos}");

        GF.UI.OpenUIForm(UIViews.ArrowGuideUIForm, uiParams);
    }

    /// <summary>
    /// 显示引导（通过GameObject名称查找目标）
    /// </summary>
    /// <param name="guideId">引导ID</param>
    /// <param name="targetName">目标GameObject名称</param>
    /// <param name="showFinger">是否显示手指</param>
    /// <param name="onComplete">完成回调</param>
    public void ShowGuideByName(int guideId, string targetName, bool showFinger = true, Action onComplete = null)
    {
        GameObject target = GameObject.Find(targetName);
        if (target != null)
        {
            ShowGuide(guideId, target.transform, showFinger, onComplete);
        }
        else
        {
            Log.Error($"GuideManager: Cannot find target GameObject '{targetName}'");
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 显示引导（通过UI路径查找目标）
    /// </summary>
    /// <param name="guideId">引导ID</param>
    /// <param name="uiPath">UI路径，如"Canvas/MainPanel/Button"</param>
    /// <param name="showFinger">是否显示手指</param>
    /// <param name="onComplete">完成回调</param>
    public void ShowGuideByPath(int guideId, string uiPath, bool showFinger = true, Action onComplete = null)
    {
        Transform target = FindTransformByPath(uiPath);
        if (target != null)
        {
            ShowGuide(guideId, target, showFinger, onComplete);
        }
        else
        {
            Log.Error($"GuideManager: Cannot find target at path '{uiPath}'");
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 通过路径查找Transform
    /// </summary>
    private Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string[] pathParts = path.Split('/');
        Transform current = null;

        // 从根开始查找
        foreach (string part in pathParts)
        {
            if (current == null)
            {
                // 查找根对象
                GameObject root = GameObject.Find(part);
                if (root == null) return null;
                current = root.transform;
            }
            else
            {
                // 在子对象中查找
                Transform child = current.Find(part);
                if (child == null) return null;
                current = child;
            }
        }

        return current;
    }

    /// <summary>
    /// 检查引导是否已完成
    /// </summary>
    /// <param name="guideId">引导ID</param>
    /// <returns>是否已完成</returns>
    public bool IsGuideCompleted(int guideId)
    {
        //配置开关
        // if (GF.Config.GetInt("GuideType") == 0 || !CommonHelper.IsSpec()) return true;
        if (GF.Config.GetInt("GuideType") == 0) return true;
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData?.CompleteGuideIds == null) return false;
        return playerData.CompleteGuideIds.Contains(guideId);
    }

    /// <summary>
    /// 清除所有引导记录
    /// </summary>
    public void ClearAllGuideRecords()
    {
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData != null)
        {
            playerData.CompleteGuideIds = new List<int>();
            playerData.Save();
            Log.Info("所有引导记录已清除");
        }
    }

    /// <summary>
    /// 获取已完成的引导数量
    /// </summary>
    /// <returns>已完成的引导数量</returns>
    public int GetCompletedGuideCount()
    {
        // if (GF.Config.GetInt("GuideType") != 1 || !CommonHelper.IsSpec()) return 0 ;
        if (GF.Config.GetInt("GuideType") != 1) return 0 ;
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        return playerData?.CompleteGuideIds?.Count ?? 0;
    }


    /// <summary>
    /// 检查是否是新玩家（没有完成任何引导）
    /// </summary>
    /// <returns>是否是新玩家</returns>
    public bool IsNewPlayer()
    {
        return GetCompletedGuideCount() == 0;
    }

    /// <summary>
    /// 显示新手引导序列
    /// </summary>
    /// <param name="guideSequence">引导序列</param>
    /// <param name="onAllComplete">全部完成回调</param>
    public void ShowGuideSequence(GuideStep[] guideSequence, Action onAllComplete = null)
    {
        if (guideSequence == null || guideSequence.Length == 0)
        {
            onAllComplete?.Invoke();
            return;
        }

        ShowNextGuide(guideSequence, 0, onAllComplete);
    }

    /// <summary>
    /// 显示下一个引导
    /// </summary>
    private void ShowNextGuide(GuideStep[] guideSequence, int currentIndex, Action onAllComplete)
    {
        if (currentIndex >= guideSequence.Length)
        {
            onAllComplete?.Invoke();
            return;
        }

        GuideStep step = guideSequence[currentIndex];

        // 检查是否已完成
        if (IsGuideCompleted(step.guideId))
        {
            // 跳过已完成的引导
            ShowNextGuide(guideSequence, currentIndex + 1, onAllComplete);
            return;
        }

        // 显示当前引导
        ShowGuide(step.guideId, step.targetTransform, step.showFinger, () =>
        {
            // 当前引导完成，显示下一个
            ShowNextGuide(guideSequence, currentIndex + 1, onAllComplete);
        });
    }
}

/// <summary>
/// 引导步骤
/// </summary>
[System.Serializable]
public class GuideStep
{
    public int guideId;
    public Transform targetTransform;
    public bool showFinger = true;

    public GuideStep(int guideId, Transform targetTransform, bool showFinger = true)
    {
        this.guideId = guideId;
        this.targetTransform = targetTransform;
        this.showFinger = showFinger;
    }
}