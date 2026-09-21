using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Event;
using GameFramework;
using DG.Tweening;

namespace ArrowMaze.UI
{
    /// <summary>
    /// 关卡验证结果：用于自动模拟点击验证通关可能及最快通关时长
    /// </summary>
    public struct LevelVerificationResult
    {
        /// <summary>是否通关（所有箭头已移除）</summary>
        public bool Solved;
        /// <summary>模拟点击次数（最少步数）</summary>
        public int StepCount;
        /// <summary>从开始到结束的用时（秒）</summary>
        public float DurationSeconds;
        /// <summary>若未通关，剩余箭头数（死锁）</summary>
        public int RemainingArrows;
    }

    /// <summary>
    /// 箭头迷宫UI界面
    /// </summary>
    public partial class ArrowMazeUIForm : UIFormBase
    {
        private LevelManager m_LevelManager;
        private PlayerDataModel m_PlayerData;
        private int m_LevelIndex = 0;
        private bool m_IsVerifyingLevel;
        private Coroutine m_VerificationCoroutine;
        #region 气泡配置
        private float bubbleSpawnInterval = 120f;  // 气泡出现间隔（秒）
        private float bubbleAutoDisappearTime = 60f;  // 气泡自动消失时间（秒）
        private float bubbleCooldownAfterClose = 60f;  // 关闭界面后的冷却时间（秒）
        private float bubbleCooldownAfterClick = 60f;  // 点击气泡后的冷却时间（秒）

        private float m_BubbleTimer = 0f;  // 气泡计时器
        private float m_BubbleCooldownTimer = 0f;  // 冷却计时器
        private bool m_IsBubbleCooldown = false;  // 是否在冷却中
        private ArrowBubbleRewardItem m_CurrentBubble = null;  // 当前气泡
        private float m_BubbleLifetimeTimer = 0f;  // 气泡生命周期计时器
        private bool m_IsBubbleActive = false;  // 是否有活跃的气泡
        private const int BubbleUnlockLevel = 4; // 前3关不展示气泡
        #endregion
        private const float LevelLoadCutsceneMinDurationSeconds = 0.4f;
        private const float CutsceneRevealDurationSeconds = 0.8f;
        private const float GameWinRewardDelaySec = 1.5f;
        private Coroutine m_LoadLevelCutsceneCoroutine;
        private Coroutine m_GameWinCoroutine;
        private Vector2 m_CutsceneTopImageBaseAnchoredPos;
        private Vector2 m_CutsceneBottomImageBaseAnchoredPos;
        private bool m_CutsceneBaseLayoutCached;
        private bool m_IsSyncingProgressZoom;
        private bool m_ForceEliminatePendingConsume;
        private const float ImageHStepInterval = 0.35f;
        private const float ImageHLoopPause = 0.35f;
        private ArrowComboDisplayController m_ComboDisplay;

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            var uiparms = UIParams.Create();
            uiparms.Set<VarBoolean>(ArrowUITopbar.P_EnableBG, true);
            uiparms.Set<VarBoolean>(ArrowUITopbar.P_EnableSettingBtn, true);
            this.OpenSubUIForm(UIViews.ArrowUITopbar, 1, uiparms);
            varDebugObj.SetActive(CommonHelper.IsDebug());

            m_PlayerData = GF.DataModel.GetDataModel<PlayerDataModel>();
            m_LevelIndex = m_PlayerData.LevelId;

            GF.Event.Subscribe(ArrowMazeGameWinEventArgs.EventId, OnGameWin);
            GF.Event.Subscribe(ArrowMazeGameOverEventArgs.EventId, OnGameOver);
            GF.Event.Subscribe(ArrowMazeArrowHitEventArgs.EventId, OnArrowHit);
            GF.Event.Subscribe(UserSkinChangeEventArgs.EventId, OnUserSkinChange);
            GF.Event.Subscribe(BgSkinChangeEventArgs.EventId, OnBgSkinChange);
            GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
            GF.Event.Subscribe(UserTypeChangeEventArgs.EventId, OnUserTypeChange);
            GF.Event.Subscribe(ArrowMazeForceEliminateUsedEventArgs.EventId, OnForceEliminateUsed);
            // GF.Event.Subscribe(ArrowMazeArrowEliminatedEventArgs.EventId, OnArrowEliminatedForCombo);
            GF.Event.Subscribe(ArrowMazeEliminationRewardDialogClosedEventArgs.EventId, OnEliminationRewardDialogClosed);
            // ResetComboDisplay();
            // InitComboDisplay();

            bubbleSpawnInterval = GF.Config.GetFloat("BubbleSpawnInterval");
            bubbleAutoDisappearTime = GF.Config.GetFloat("BubbleAutoDisappearTime");
            bubbleCooldownAfterClose = GF.Config.GetFloat("BubbleCooldownAfterClose");
            bubbleCooldownAfterClick = GF.Config.GetFloat("BubbleCooldownAfterClick");

            // 获取LevelManager
            if (ArrowMazeManager.Instance != null)
            {
                m_LevelManager = ArrowMazeManager.Instance.LevelManager;
            }

            UpdateUI();
            
            
            // 设置相机可显示范围（顶部栏与底部栏之间的空白区域），需在 LoadLevelData 之前调用
            if (ArrowMazeManager.Instance != null && ArrowMazeManager.Instance.CameraController != null && varCameraView != null)
            {
                ArrowMazeManager.Instance.CameraController.SetCameraViewRect(varCameraView);
            }
            ApplyBgSkinColor(m_PlayerData != null && m_PlayerData.IsNightMode);
            CacheCutsceneBaseLayoutOnce();
            HideCutsceneRoot();
            SetGameWinVisible(false);
            LoadLevelData(m_LevelIndex);
            
            UpdateUIByUserType();
            ResetForceEliminatePropState();
        }

        private void UpdateUIByUserType()
        {
            UpdateForceEliminateUI();
        }

        private void OnUserTypeChange(object sender, GameEventArgs e)
        {
            var args = e as UserTypeChangeEventArgs;
            if (args == null) return;

            UpdateUIByUserType();
        }

        private void OnPlayerDataChanged(object sender, GameEventArgs e)
        {
            PlayerDataChangedEventArgs args = e as PlayerDataChangedEventArgs;
            if (args == null) return;

            if (args.DataType == PlayerDataType.Prop3)
                UpdateForceEliminateUI();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (m_VerificationCoroutine != null)
            {
                StopCoroutine(m_VerificationCoroutine);
                m_VerificationCoroutine = null;
            }
            if (m_LoadLevelCutsceneCoroutine != null)
            {
                StopCoroutine(m_LoadLevelCutsceneCoroutine);
                m_LoadLevelCutsceneCoroutine = null;
            }
            KillCutsceneTweens();
            StopComboDisplay();
            HideCutsceneRoot();
            m_IsVerifyingLevel = false;

            GF.Event.Unsubscribe(ArrowMazeGameWinEventArgs.EventId, OnGameWin);
            GF.Event.Unsubscribe(ArrowMazeGameOverEventArgs.EventId, OnGameOver);
            GF.Event.Unsubscribe(ArrowMazeArrowHitEventArgs.EventId, OnArrowHit);
            GF.Event.Unsubscribe(UserSkinChangeEventArgs.EventId, OnUserSkinChange);
            GF.Event.Unsubscribe(BgSkinChangeEventArgs.EventId, OnBgSkinChange);
            GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
            GF.Event.Unsubscribe(UserTypeChangeEventArgs.EventId, OnUserTypeChange);
            GF.Event.Unsubscribe(ArrowMazeForceEliminateUsedEventArgs.EventId, OnForceEliminateUsed);
            // GF.Event.Unsubscribe(ArrowMazeArrowEliminatedEventArgs.EventId, OnArrowEliminatedForCombo);
            GF.Event.Unsubscribe(ArrowMazeEliminationRewardDialogClosedEventArgs.EventId, OnEliminationRewardDialogClosed);

            m_LevelManager?.SetGameplayInputBlocked(false);
            ResetForceEliminatePropState();
            ResetBubbleReward();
            base.OnClose(isShutdown, userData);
        }
        protected override void OnButtonClick(object sender, string btId)
        {
            base.OnButtonClick(sender, btId);
            if (btId == "BtnMenu")
            {
                GF.UI.OpenUIForm(UIViews.ArrowSettingDialog);
            }
            if (btId == "LoadLevelButton")
            {
                OnLoadLevelButtonClick();
            }
            if (btId == "LevelAddButton")
            {
                m_LevelIndex = m_LevelIndex + 1;
                m_PlayerData.LevelId  = m_LevelIndex;
                m_PlayerData.Save();
                LoadLevelData(m_LevelIndex);
            }
            if (btId == "LevelSubButton")
            {
                m_LevelIndex = m_LevelIndex - 1;
                m_PlayerData.LevelId  = m_LevelIndex;
                m_PlayerData.Save();
                LoadLevelData(m_LevelIndex);
            }
            if (btId == "AutoRemoveButton")
            {
                RunLevelVerification((result) =>
                {
                    Debug.Log($"验证结果: 通关={result.Solved}, 步数={result.StepCount}, 用时={result.DurationSeconds:F2}s, 剩余箭头={result.RemainingArrows}");
                });
            }
            if (btId == "QuickVerifyButton")
            {
                QuickRunLevelVerification((result) =>
                {
                    Debug.Log($"急速验证结果: 通关={result.Solved}, 步数={result.StepCount}, 用时={result.DurationSeconds:F2}s, 剩余箭头={result.RemainingArrows}");
                });
            }
            
            if (btId == "ItemAutoRemove")
            {
                UseItemAutoRemoveArrow();
            }
            if (btId == "ItemShowAllGuideLines")
            {
                UseItemShowAllGuideLines();
            }
            if (btId == "ItemForceEliminate")
            {
                UseItemForceEliminate();
            }
            if (btId == "ArrowColorModeToggle")
            {
                ToggleArrowColorMode();
            }
        }

        /// <summary>
        /// 道具1：自动移除一个箭头。在场景内找一个可直接移除的箭头并模拟点击；若无则提示道具使用失败。
        /// 用法：将道具按钮的 btId 设为 "ItemAutoRemove"，或直接调用本方法。
        /// </summary>
        public void UseItemAutoRemoveArrow()
        {
            if (m_LevelManager == null || (ArrowMazeManager.Instance?.CameraController != null && ArrowMazeManager.Instance.CameraController.IsEntranceAnimating))
                return;
            if (m_LevelManager.TryUseItemAutoRemoveArrow())
                return;
            if (GF.UI != null)
                GF.UI.ShowToast(GF.Localization.GetString("ArrowMazeUIForm.ItemAutoRemoveFail"));
        }

        /// <summary>
        /// 道具2：显示所有引导线。使用后将所有未移动的箭头的引导线显示出来，点击任意箭头后隐藏全部。
        /// 用法：将道具按钮的 btId 设为 "ItemShowAllGuideLines"，或直接调用本方法。
        /// </summary>
        public void UseItemShowAllGuideLines()
        {
            if (m_LevelManager == null || (ArrowMazeManager.Instance?.CameraController != null && ArrowMazeManager.Instance.CameraController.IsEntranceAnimating))
                return;
            m_LevelManager?.ActivateShowAllGuideLines();
        }

        /// <summary>
        /// 道具3：强制消除箭头。B 面有次数直接使用，无次数看广告获得 1 次并立即进入使用态；实际消除箭头后才扣次数。
        /// </summary>
        public void UseItemForceEliminate()
        {
            if (m_LevelManager == null || (ArrowMazeManager.Instance?.CameraController != null && ArrowMazeManager.Instance.CameraController.IsEntranceAnimating))
                return;
            if (ArrowLineEntity.ForceEliminateNextClick)
                return;

            if (CommonHelper.IsSpec())
            {
                int count = m_PlayerData != null ? m_PlayerData.Prop3 : 0;
                if (count > 0)
                {
                    TryActivateForceEliminate(true);
                    return;
                }

                CommonHelper.LogEvent(AdjustEventCodeEvent.erase_ad);
                CommonHelper.ShowVideoAd("video_erase", isSuccess =>
                {
                    if (!isSuccess || m_PlayerData == null)
                        return;
                    
                    CommonHelper.LogEvent(AdjustEventCodeEvent.erase_obtain);
                    m_PlayerData.Prop3 += 1;
                    m_PlayerData.Save();
                    TryActivateForceEliminate(true);
                });
                return;
            }

            TryActivateForceEliminate(false);
        }

        private void TryActivateForceEliminate(bool trackConsume)
        {
            m_LevelManager?.ActivateForceEliminateNextClick();
            m_ForceEliminatePendingConsume = trackConsume;
            SetForceEliminateHitVisible(true);
        }

        private void OnForceEliminateUsed(object sender, GameEventArgs e)
        {
            if (e is not ArrowMazeForceEliminateUsedEventArgs)
                return;

            SetForceEliminateHitVisible(false);
            CommonHelper.LogEvent(AdjustEventCodeEvent.erase_use);
            if (m_ForceEliminatePendingConsume && m_PlayerData != null && m_PlayerData.Prop3 > 0)
            {
                m_PlayerData.Prop3 -= 1;
                m_PlayerData.Save();
            }

            m_ForceEliminatePendingConsume = false;
            UpdateForceEliminateUI();
        }

        private void ResetForceEliminatePropState()
        {
            m_ForceEliminatePendingConsume = false;
            SetForceEliminateHitVisible(false);
            UpdateForceEliminateUI();
        }

        private void SetForceEliminateHitVisible(bool visible)
        {
            if (varImgPropHit != null)
                varImgPropHit.SetActive(visible);
        }

        private void UpdateForceEliminateUI()
        {
            if (!CommonHelper.IsSpec())
            {
                if (varImgEliminateAd != null)
                    varImgEliminateAd.SetActive(false);
                if (varImgEliminateTime != null)
                    varImgEliminateTime.SetActive(false);
                return;
            }

            int count = m_PlayerData != null ? m_PlayerData.Prop3 : 0;
            bool hasCount = count > 0;
            if (varImgEliminateTime != null)
                varImgEliminateTime.SetActive(hasCount);
            if (varImgEliminateAd != null)
                varImgEliminateAd.SetActive(!hasCount);
            if (varTxtEliminateTime != null)
                varTxtEliminateTime.text = count.ToString();
        }

        private void OnUserSkinChange(object sender, GameEventArgs e)
        {
            var args = e as UserSkinChangeEventArgs;
            if (args == null) return;
            SetArrowColorMode(GetArrowColorMode());
        }

        private void OnBgSkinChange(object sender, GameEventArgs e)
        {
            var args = e as BgSkinChangeEventArgs;
            if (args == null) return;
            ApplyBgSkinColor(args.IsNightMode);
            // 仅默认箭头皮肤随日/夜切换颜色（夜间白、日间黑），彩色皮肤不变；用事件中的 IsNightMode 保证与背景一致
            m_LevelManager?.ApplyArrowColorMode(GetArrowColorMode(), args.IsNightMode);
        }

        /// <summary>
        /// 应用背景日/夜间色到玩法相机与玩法界面顶部 varTop（若有 Image）。日间 #EBEBEB，夜间 #232633。
        /// </summary>
        private void ApplyBgSkinColor(bool isNightMode)
        {
            if (ArrowMazeManager.Instance != null && ArrowMazeManager.Instance.CameraController != null)
                ArrowMazeManager.Instance.CameraController.SetDayNightMode(isNightMode);
            // if (varTop != null)
            // {
            //     var img = varTop.GetComponent<UnityEngine.UI.Image>();
            //     if (img != null && UnityEngine.ColorUtility.TryParseHtmlString(isNightMode ? "#232633" : "#EBEBEB", out var c))
            //         img.color = c;
            // }
            foreach (var buttonBg in varButtonBgArr)
            {
                if (buttonBg != null)
                {
                    buttonBg.color = isNightMode?new Color(255, 255, 255, 0.5f):new Color(255, 255, 255, 1f);
                }
            }
        }

        /// <summary>
        /// 获取当前箭头颜色模式（持久化数据）：true=彩色，false=默认黑色。
        /// 用法：切换按钮、设置界面等可据此刷新显示状态。
        /// </summary>
        public static bool GetArrowColorMode()
        {
            var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
            return playerData != null ? playerData.UseColorfulArrows : false;
        }

        /// <summary>
        /// 设置箭头颜色模式并立即应用到当前场景所有非特殊货币箭头，同时写入玩家数据持久化。
        /// </summary>
        /// <param name="useColorful">true=彩色（随机色板，排除绿/黑/白），false=默认黑色</param>
        public void SetArrowColorMode(bool useColorful)
        {
            if (m_PlayerData != null)
                m_PlayerData.UseColorfulArrows = useColorful;
            m_LevelManager?.ApplyArrowColorMode(m_PlayerData.UseColorfulArrows,m_PlayerData.IsNightMode);
        }

        /// <summary>
        /// 切换箭头彩色/默认色模式，并返回切换后的状态（true=彩色，false=默认）。会持久化到玩家数据。
        /// 用法：将切换按钮的 btId 设为 "ArrowColorModeToggle"，或直接调用本方法后根据返回值更新按钮状态。
        /// </summary>
        public bool ToggleArrowColorMode()
        {
            bool next = !GetArrowColorMode();
            SetArrowColorMode(next);
            return next;
        }
        
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (m_LevelManager != null)
            {
                UpdateUI();
                UpdateBubbleReward(realElapseSeconds);
            }
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            if (m_LevelManager == null) return;
            
            // varCountdown.SetActive(LevelManager.IS_NEED_COUNTDOWN);
            // if (LevelManager.IS_NEED_COUNTDOWN && varCountdownTxt != null)
            // {
            //     int sec = Mathf.Max(0, Mathf.FloorToInt(m_LevelManager.RemainingTime));
            //     varCountdownTxt.text = $"{sec / 60:D2}:{sec % 60:D2}";
            // }

            if (varHeartArr != null && varHeartArr.Length > 0)
            {
                for (int i = 0; i < varHeartArr.Length; i++)
                    varHeartArr[i].gameObject.SetActive((i + 1) <= m_LevelManager.Lives);
            }

        }
        

        private void OnEliminationRewardDialogClosed(object sender, GameEventArgs e)
        {
            m_LevelManager?.RequestCheckGameWin();
        }
        

        private void OnProgressZoomValueChanged(float value)
        {
            if (m_IsSyncingProgressZoom) return;
            var cam = ArrowMazeManager.Instance?.CameraController;
            if (cam == null || cam.IsEntranceAnimating) return;
            cam.SetZoomProgress(value, immediate: true);
        }

        /// <summary>
        /// 更新主/子关卡进度 UI（levelCurTxt、levelTargetTxt、imgHit、levelSubProgressTxt）。
        /// </summary>
        private int GetDisplayLinearLevelId()
        {
            if (m_LevelIndex > 0)
                return m_LevelIndex;
            return m_PlayerData != null ? m_PlayerData.LevelId : 1;
        }
        

        private static void SetProgressIndicatorActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        /// <summary>
        /// 加载关卡按钮点击
        /// </summary>
        private void OnLoadLevelButtonClick()
        {
            // 这里可以从文件选择器加载关卡
            // 暂时使用测试关卡
            LoadLevelData(m_LevelIndex);
        }

        /// <summary>
        /// 加载测试关卡（示例）
        /// </summary>
        private void LoadLevelData(int linearLevelId = 1)
        {
            m_LevelIndex = linearLevelId;
            ResetComboDisplay();
            ResetForceEliminatePropState();
            if (!ShouldShowBubbleReward())
                ResetBubbleReward();
            var progress = ArrowLevelProgressUtility.GetStateFromLinearLevelId(linearLevelId);
            int levelFileIndex = progress.ActualLevelFileIndex;
            // varGuideComponet.SetActive(linearLevelId > 1);
            string levelName = $"Level{levelFileIndex}";

            if (m_LoadLevelCutsceneCoroutine != null)
            {
                StopCoroutine(m_LoadLevelCutsceneCoroutine);
                m_LoadLevelCutsceneCoroutine = null;
            }
            m_LoadLevelCutsceneCoroutine = StartCoroutine(LoadLevelDataWithCutscene(linearLevelId, levelName));
        }

        private void CacheCutsceneBaseLayoutOnce()
        {
            if (m_CutsceneBaseLayoutCached)
                return;
            if (varTopImage != null)
                m_CutsceneTopImageBaseAnchoredPos = varTopImage.anchoredPosition;
            if (varBottomImage != null)
                m_CutsceneBottomImageBaseAnchoredPos = varBottomImage.anchoredPosition;
            m_CutsceneBaseLayoutCached = true;
        }

        private void KillCutsceneTweens()
        {
            if (varTopImage != null)
                varTopImage.DOKill();
            if (varBottomImage != null)
                varBottomImage.DOKill();
        }

        
        private void HideCutsceneRoot()
        {
            KillCutsceneTweens();
            if (varCutscene != null)
                varCutscene.SetActive(false);
        }

        private void BeginLevelLoadCutscene()
        {
            CacheCutsceneBaseLayoutOnce();
            KillCutsceneTweens();
            if (varCutscene == null)
                return;
            varCutscene.SetActive(true);
            GF.Sound.PlayEffect("arrowPlay/transition.mp3");
            if (varTopImage != null)
                varTopImage.anchoredPosition = m_CutsceneTopImageBaseAnchoredPos;
            if (varBottomImage != null)
                varBottomImage.anchoredPosition = m_CutsceneBottomImageBaseAnchoredPos;
        }

        private float ComputeCutsceneSlideOffset()
        {
            float maxEdge = 800f;
            if (varTopImage != null)
                maxEdge = Mathf.Max(maxEdge, varTopImage.rect.height, varTopImage.rect.width * 0.5f);
            if (varBottomImage != null)
                maxEdge = Mathf.Max(maxEdge, varBottomImage.rect.height, varBottomImage.rect.width * 0.5f);
            var canvas = varCutscene != null ? varCutscene.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                var rootRt = canvas.transform as RectTransform;
                if (rootRt != null)
                    maxEdge = Mathf.Max(maxEdge, rootRt.rect.height, rootRt.rect.width);
            }
            return maxEdge * 0.55f + 400f;
        }

        private IEnumerator LoadLevelDataWithCutscene(int linearLevelId, string levelName)
        {
            float loadStartRealtime = Time.unscaledTime;
            BeginLevelLoadCutscene();

            ArrowLevelData loadedLevel = null;
            string failureMessage = null;
            bool loadSettled = false;

            ArrowLevelDataUtility.LoadFromLevelDataJson(levelName,
                levelData =>
                {
                    loadedLevel = levelData;
                    loadSettled = true;
                },
                errorMessage =>
                {
                    failureMessage = errorMessage;
                    loadSettled = true;
                });

            yield return new WaitUntil(() => loadSettled);

            float elapsed = Time.unscaledTime - loadStartRealtime;
            if (elapsed < LevelLoadCutsceneMinDurationSeconds)
                yield return new WaitForSecondsRealtime(LevelLoadCutsceneMinDurationSeconds - elapsed);

            if (loadedLevel != null && m_LevelManager != null)
            {
                ArrowLineEntity.UseColorfulArrows = m_PlayerData != null && m_PlayerData.UseColorfulArrows;
                m_LevelManager.LoadLevel(loadedLevel);
                if (varLevelTxt != null)
                    varLevelTxt.text = GF.Localization.GetString("ArrowMazeUIForm.LevelTxt", linearLevelId);
                yield return PlayCutsceneRevealThenHide();
            }
            else
            {
                if (!string.IsNullOrEmpty(failureMessage))
                    Debug.LogError($"加载关卡数据失败: {failureMessage}");
                HideCutsceneRoot();
            }

            CheckGuide();
            m_LoadLevelCutsceneCoroutine = null;
        }

        private IEnumerator PlayCutsceneRevealThenHide()
        {
            if (varCutscene == null || !varCutscene.activeSelf)
                yield break;
            if (varTopImage == null && varBottomImage == null)
            {
                HideCutsceneRoot();
                yield break;
            }

            float slide = ComputeCutsceneSlideOffset();
            float d = CutsceneRevealDurationSeconds;
            if (varTopImage != null)
                varTopImage.DOAnchorPos(m_CutsceneTopImageBaseAnchoredPos + new Vector2(0f, slide), d).SetEase(Ease.OutCubic).SetUpdate(true);
            if (varBottomImage != null)
                varBottomImage.DOAnchorPos(m_CutsceneBottomImageBaseAnchoredPos + new Vector2(0f, -slide), d).SetEase(Ease.OutCubic).SetUpdate(true);
            yield return new WaitForSecondsRealtime(d);
            HideCutsceneRoot();
        }

        private void CheckGuide()
        {
            if (!CommonHelper.IsSpec()) return;

            if (m_PlayerData.LevelId == 2 && !m_PlayerData.CompleteGuideIds.Contains(4))
            {
                GuideManager.Instance.ShowGuide(4, varCameraView, true, null);
            }
        }

        private void OnGameWin(object sender, GameEventArgs e)
        {
            int completedLinearLevel = m_PlayerData.LevelId;
            m_PlayerData.LevelId += 1;
            Log.Info($"通关,等级+1，当前等级{m_PlayerData.LevelId}");

            GF.Sound.PlayEffect("arrowPlay/win.mp3");

            if (m_GameWinCoroutine != null)
            {
                StopCoroutine(m_GameWinCoroutine);
                m_GameWinCoroutine = null;
            }
            m_GameWinCoroutine = StartCoroutine(HandleGameWinFlow(completedLinearLevel));
        }

        private IEnumerator HandleGameWinFlow(int completedLinearLevel)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.level_win);
            CommonHelper.LogLevelCompleteEvents(completedLinearLevel);
            
            SetGameWinVisible(true);
            yield return new WaitForSecondsRealtime(GameWinRewardDelaySec);

            if (!CommonHelper.IsSpec())
            {
                SetGameWinVisible(false);
                NextLevel();
                m_GameWinCoroutine = null;
                yield break;
            }

            SetGameWinVisible(false);
            var winreward = m_LevelManager.GenerateRandomReward(2);
            var rewardParams = RewardDialogParams.Create();
            rewardParams.rewardSource = RewardSourceConst.GamewinReward;
            rewardParams.Rewards.Add(new RewardData(PlayerDataType.Diamond, winreward));
            rewardParams.isADDouble = true;
            rewardParams.OnDialogClosed = () =>
            {
                SetGameWinVisible(false);
                NextLevel();
            };

            var uiParams = UIParams.CreateWithReward(rewardParams);
            GF.UI.OpenUIForm(UIViews.ArrowRewardDialogUIForm, uiParams);
            m_GameWinCoroutine = null;
        }

        private void SetGameWinVisible(bool visible)
        {
            if (varGameWin != null)
                varGameWin.SetActive(visible);
        }
        private void OnGameOver(object sender, GameEventArgs e)
        {
            var args = e as ArrowMazeGameOverEventArgs;
            var failureType = args != null ? args.FailureType : ArrowMazeFailureType.LivesOver;

            var gameoverParms = UIParams.Create();
            gameoverParms.Set<VarInt32>("FailureType", (int)failureType);
            gameoverParms.Set<VarFloat>("GameProgress", (float)m_LevelManager.ArrowCount / m_LevelManager.AllArrowCount);

            VarAction onRestart = ReferencePool.Acquire<VarAction>();
            onRestart.Value = () => { ReStartGame(); };
            gameoverParms.Set<VarAction>("OnRestart", onRestart);

            VarAction onRevive = ReferencePool.Acquire<VarAction>();
            onRevive.Value = () =>
            {
                if (m_LevelManager == null) return;
                if (failureType == ArrowMazeFailureType.TimeOver)
                    m_LevelManager.ReviveByTime();
                else if (failureType == ArrowMazeFailureType.LivesOver)
                    m_LevelManager.ReviveByLife();
            };
            gameoverParms.Set<VarAction>("OnRevive", onRevive);

            CommonHelper.LogEvent(AdjustEventCodeEvent.level_lose);
            GF.UI.OpenUIForm(UIViews.ArrowMazeOverUIForm, gameoverParms);
            Log.Info("打开失败UI");
        }
        private void OnArrowHit(object sender, GameEventArgs e)
        {
            if (varHeartArr != null)
            {
                for (int i = 0; i < varHeartArr.Length; i++)
                    varHeartArr[i].gameObject.SetActive((i + 1) <= m_LevelManager.Lives);
            }
        }

        private void OnArrowEliminatedForCombo(object sender, GameEventArgs e)
        {
            var args = e as ArrowMazeArrowEliminatedEventArgs;
            if (args == null || args.ComboCount <= 0)
                return;

            m_ComboDisplay?.Show(args.ComboCount, args.WorldPosition);
        }

        private void InitComboDisplay()
        {
            if (varCameraView == null || varComboFloatItem == null)
                return;

            m_ComboDisplay ??= new ArrowComboDisplayController(this, varComboFloatItem, varCameraView);
        }

        internal ArrowComboFloatItem SpawnComboFloatItem()
        {
            if (varComboFloatItem == null || varCameraView == null)
                return null;

            var itemObject = SpawnItem<UIItemObject>(varComboFloatItem, varCameraView, 2f, 15, 30f);
            return itemObject?.itemLogic as ArrowComboFloatItem;
        }

        internal void UnspawnComboFloatItem(ArrowComboFloatItem item)
        {
            if (varComboFloatItem == null || item == null)
                return;

            item.StopAnimation();
            UnspawnItem<UIItemObject>(varComboFloatItem, item.gameObject);
        }

        internal void UnspawnAllComboFloatItems()
        {
            if (varComboFloatItem == null)
                return;

            UnspawnAllItem<UIItemObject>(varComboFloatItem);
        }

        private void StopComboDisplay()
        {
            if (m_ComboDisplay != null)
            {
                m_ComboDisplay.Reset();
                m_ComboDisplay = null;
            }
        }

        private void ResetComboDisplay()
        {
            m_ComboDisplay?.Reset();
        }
        private void ReStartGame()
        {
            LoadLevelData(m_LevelIndex);
        }
        private void NextLevel()
        {
            SetGameWinVisible(false);
            m_LevelIndex = m_PlayerData.LevelId;
            LoadLevelData(m_LevelIndex);
        }

        /// <summary>
        /// 自动模拟点击移除箭头，直到关卡内所有箭头移除或出现死循环。
        /// 用于验证关卡是否可通关以及最快通关步数/时长。
        /// </summary>
        /// <param name="onComplete">完成回调：通关则 Solved=true 并带步数与时长；死锁则 Solved=false 并带剩余箭头数</param>
        /// <returns>是否已开始验证（关卡未加载或已在验证中则返回 false）</returns>
        public bool RunLevelVerification(Action<LevelVerificationResult> onComplete)
        {
            if (m_LevelManager == null || m_IsVerifyingLevel)
            {
                onComplete?.Invoke(new LevelVerificationResult { Solved = false, StepCount = 0, DurationSeconds = 0f, RemainingArrows = m_LevelManager?.ArrowCount ?? 0 });
                return false;
            }
            if (m_VerificationCoroutine != null)
                StopCoroutine(m_VerificationCoroutine);
            m_IsVerifyingLevel = true;
            m_VerificationCoroutine = StartCoroutine(LevelVerificationCoroutine(onComplete, speedMultiplier: 1f));
            return true;
        }

        /// <summary>
        /// 急速验证：每轮同时移除所有无阻挡的箭头，箭头移速加倍，用于快速验证关卡可通关性及通关时长。
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        /// <returns>是否已开始验证</returns>
        public bool QuickRunLevelVerification(Action<LevelVerificationResult> onComplete)
        {
            if (m_LevelManager == null || m_IsVerifyingLevel)
            {
                onComplete?.Invoke(new LevelVerificationResult { Solved = false, StepCount = 0, DurationSeconds = 0f, RemainingArrows = m_LevelManager?.ArrowCount ?? 0 });
                return false;
            }
            if (m_VerificationCoroutine != null)
                StopCoroutine(m_VerificationCoroutine);
            m_IsVerifyingLevel = true;
            m_VerificationCoroutine = StartCoroutine(LevelVerificationCoroutine(onComplete, speedMultiplier: 2f, clickAllUnblocked: true));
            return true;
        }

        private IEnumerator LevelVerificationCoroutine(Action<LevelVerificationResult> onComplete, float speedMultiplier = 1f, bool clickAllUnblocked = false)
        {
            int stepCount = 0;
            float startTime = Time.time;
            const int maxSteps = 10000; // 防止异常关卡无限循环

            float prevMultiplier = ArrowLineEntity.SpeedMultiplier;
            ArrowLineEntity.SpeedMultiplier = speedMultiplier;

            try
            {
                while (m_LevelManager != null && m_LevelManager.ArrowCount > 0 && stepCount < maxSteps)
                {
                    var entities = m_LevelManager.GetArrowEntities();
                    if (clickAllUnblocked)
                    {
                        // 急速模式：本轮所有无阻挡的箭头同时点击
                        int clicked = 0;
                        foreach (var e in entities)
                        {
                            if (e != null && e.CanAdvance())
                            {
                                e.TryAdvance();
                                clicked++;
                            }
                        }
                        if (clicked == 0)
                            break; // 死锁：无任何箭头可前进
                        stepCount += clicked;
                    }
                    else
                    {
                        // 普通模式：每轮只点击一个无阻挡箭头
                        ArrowLineEntity toClick = null;
                        foreach (var e in entities)
                        {
                            if (e != null && e.CanAdvance())
                            {
                                toClick = e;
                                break;
                            }
                        }
                        if (toClick == null)
                            break;
                        toClick.TryAdvance();
                        stepCount++;
                    }
                    yield return new WaitUntil(() => m_LevelManager == null || !m_LevelManager.AnyArrowMoving());
                }
            }
            finally
            {
                ArrowLineEntity.SpeedMultiplier = prevMultiplier;
            }

            float duration = Time.time - startTime;
            int remaining = m_LevelManager != null ? m_LevelManager.ArrowCount : 0;
            bool solved = remaining == 0;
            m_IsVerifyingLevel = false;
            m_VerificationCoroutine = null;

            var result = new LevelVerificationResult
            {
                Solved = solved,
                StepCount = stepCount,
                DurationSeconds = duration,
                RemainingArrows = remaining
            };
            onComplete?.Invoke(result);
            Log.Info($"[关卡验证] 通关={result.Solved}, 步数={result.StepCount}, 用时={result.DurationSeconds:F2}s, 剩余箭头={result.RemainingArrows}");
        }

        /// <summary>
        /// 更新气泡奖励逻辑
        /// </summary>
        private void UpdateBubbleReward(float deltaTime)
        {
            if (!CommonHelper.IsSpec()) return;

            if (!ShouldShowBubbleReward())
            {
                if (m_IsBubbleActive)
                    ResetBubbleReward();
                return;
            }

            // 如果正在冷却中，更新冷却计时器
            if (m_IsBubbleCooldown)
            {
                m_BubbleCooldownTimer += deltaTime;
                if (m_BubbleCooldownTimer >= bubbleSpawnInterval)
                {
                    m_IsBubbleCooldown = false;
                    m_BubbleCooldownTimer = 0f;
                    m_BubbleTimer = bubbleSpawnInterval;
                    // Log.Info("[气泡奖励] 自动消失 冷却时间结束，可以重新生成气泡");
                }
                return;
            }

            // 如果已有活跃气泡，更新生命周期计时器
            if (m_IsBubbleActive && m_CurrentBubble != null)
            {
                m_BubbleLifetimeTimer += deltaTime;
                if (m_BubbleLifetimeTimer >= bubbleAutoDisappearTime)
                {
                    // Log.Info("[气泡奖励] 气泡超时自动消失");
                    AutoDisappearBubble();
                    return;
                }
            }

            // 如果没有活跃气泡且不在冷却中，更新生成计时器
            if (!m_IsBubbleActive && !m_IsBubbleCooldown)
            {
                m_BubbleTimer += deltaTime;
                if (m_BubbleTimer >= bubbleSpawnInterval)
                {
                    // Log.Info("[气泡奖励] 开始生成气泡");
                    SpawnBubble();
                    m_BubbleTimer = 0f;
                }
            }
        }

        /// <summary>
        /// 生成气泡
        /// </summary>
        private void SpawnBubble()
        {
            float rewardValue = m_LevelManager.GenerateRandomReward(3);
            if (rewardValue <= 0f)
            {
                Log.Info("[气泡奖励] 奖励数值<=0，跳过生成气泡");
                return;
            }

            bool useUpwardDiagonalMove = GF.Config.GetInt("BubbleFullScreenMove", 0) == 1;
            RectTransform spawnParent = useUpwardDiagonalMove
                ? transform as RectTransform
                : varBubbleParentRect;
            if (spawnParent == null)
            {
                Log.Error("[气泡奖励] 气泡父节点为空");
                return;
            }

            // 创建气泡
            var bubbleObject = SpawnItem<UIItemObject>(varBubbleRewardItem, spawnParent);
            if (bubbleObject == null)
            {
                Log.Error("[气泡奖励] 创建气泡失败");
                return;
            }

            var bubbleLogic = bubbleObject.itemLogic as ArrowBubbleRewardItem;
            if (bubbleLogic == null)
            {
                Log.Error("[气泡奖励] 气泡逻辑组件为空");
                return;
            }

            // 获取父容器 RectTransform（varBubbleParentRect 区域）
            var parentRect = useUpwardDiagonalMove ? spawnParent : varBubbleParentRect;
            if (parentRect == null)
            {
                Log.Error("[气泡奖励] 父节点RectTransform为空");
                return;
            }

            Rect r = useUpwardDiagonalMove ? GetFullScreenLocalRect(spawnParent) : parentRect.rect;
            const float bubbleWidth = 300f;
            float halfWidth = bubbleWidth * 0.5f;
            float bottomY = r.yMin + 20f;
            float topY = r.yMax - 20f;

            // 起点：父节点左侧边界外（气泡中心在左边界左侧半宽处）
            float startX = r.xMin - halfWidth;
            // 终点：父节点右侧边界外（气泡完全移出后再回到左侧）
            float endX = r.xMax + halfWidth;

            if (bottomY >= topY)
            {
                Log.Warning("[气泡奖励] varBubbleParentRect 区域过小，无法生成路径");
                return;
            }

            float centerY = (bottomY + topY) * 0.5f;
            float amplitudeY = Mathf.Max(10f, (topY - bottomY) * 0.5f - 20f);

            // 单次从左到右的时长，在气泡生命周期内做多次循环
            float cycleDuration = Mathf.Max(4f, bubbleAutoDisappearTime * 0.5f);
            float moveSpeed = GF.Config.GetFloat("BubbleMoveSpeed");

            var bubbleRect = bubbleObject.gameObject.GetComponent<RectTransform>();
            if (bubbleRect != null && !useUpwardDiagonalMove)
                bubbleRect.localPosition = new Vector3(startX, centerY, 0f);

            bubbleLogic.SetBubbleReward(PlayerDataType.Diamond, rewardValue, true);
            Log.Info($"[气泡奖励] 设置奖励：{PlayerDataType.Diamond} x {rewardValue}");
            bubbleLogic.SetCallbacks(OnBubbleClicked, OnBubbleDisappear);
            if (useUpwardDiagonalMove)
            {
                bool spawnBottomLeft = UnityEngine.Random.value > 0.5f;
                bubbleLogic.StartUpwardDiagonalMove(r, spawnBottomLeft, moveSpeed);
            }
            else
            {
                // 启动水平运动：从左边界外进入，完全移出右边界后循环回左侧
                bubbleLogic.StartHorizontalMove(startX, endX, centerY, amplitudeY, cycleDuration);
            }
            m_CurrentBubble = bubbleLogic;
            m_IsBubbleActive = true;
            m_BubbleLifetimeTimer = 0f;
            CommonHelper.LogEvent(AdjustEventCodeEvent.bubble_enter);
        }

        /// <summary>
        /// 气泡被点击
        /// </summary>
        private void OnBubbleClicked(ArrowBubbleRewardItem bubble, bool isAd = false)
        {
            if (bubble != m_CurrentBubble) return;
            CommonHelper.LogEvent(AdjustEventCodeEvent.bubble_click);
            // 播放奖励音效
            GF.Sound.PlayEffect("reward.mp3");
            var m_rewardValue = bubble.m_rewardValue;
            CommonHelper.ShowVideoAd(RewardSourceConst.clickpaopao, (isSuccess) =>
            {
                if (isSuccess)
                {
                    CommonHelper.LogEvent(AdjustEventCodeEvent.reward_collect, new Dictionary<string, string>() {{"placement", "bubble"}});
                    OpenBubbleRewardUIForm(m_rewardValue);
                    // 仅当气泡仍为当前活跃时回收（广告>60秒时可能已被 AutoDisappearBubble 回收，避免重复 UnspawnItem 导致对象池异常）
                    if (m_CurrentBubble == bubble && bubble != null && bubble.gameObject != null)
                    {
                        DOTween.Kill(bubble.transform);
                        UnspawnItem<UIItemObject>(varBubbleRewardItem, bubble.gameObject);
                    }
                    m_BubbleTimer = 0f;
                    m_IsBubbleActive = false;
                    m_CurrentBubble = null;
                    m_BubbleLifetimeTimer = 0f;
                    bubbleSpawnInterval = bubbleCooldownAfterClick;
                    m_IsBubbleCooldown = true;
                }
            });

        }
        /// <summary>
        /// 气泡消失
        /// </summary>
        private void OnBubbleDisappear(ArrowBubbleRewardItem bubble)
        {
            if (bubble != m_CurrentBubble) return;

            Log.Info("[气泡奖励] 气泡消失");

            // 回收气泡
            if (bubble != null && bubble.gameObject != null)
            {
                DOTween.Kill(bubble.transform);
                UnspawnItem<UIItemObject>(varBubbleRewardItem, bubble.gameObject);
            }

            m_IsBubbleActive = false;
            m_CurrentBubble = null;
            m_BubbleLifetimeTimer = 0f;

            if (bubble.ApplyPassiveCooldownOnDisappear && !m_IsBubbleCooldown)
            {
                m_IsBubbleCooldown = true;
                m_BubbleCooldownTimer = 0f;
                m_BubbleTimer = 0f;
                bubbleSpawnInterval = bubbleCooldownAfterClose;
            }
        }

        static Rect GetFullScreenLocalRect(RectTransform localSpace)
        {
            if (localSpace == null)
                return new Rect();

            Canvas rootCanvas = GFBuiltin.RootCanvas;
            if (rootCanvas == null)
                return localSpace.rect;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return localSpace.rect;

            Vector3[] corners = new Vector3[4];
            canvasRect.GetWorldCorners(corners);
            Vector2 bottomLeft = localSpace.InverseTransformPoint(corners[0]);
            Vector2 topRight = localSpace.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        /// <summary>
        /// 气泡自动消失
        /// </summary>
        private void AutoDisappearBubble()
        {
            if (m_CurrentBubble != null)
            {
                m_CurrentBubble.HideBubble();
            }
            // 进入冷却状态
            m_IsBubbleCooldown = true;
            m_BubbleCooldownTimer = 0f;
            m_BubbleTimer = 0f;
            bubbleSpawnInterval = bubbleCooldownAfterClose;
            // Log.Info($"[气泡奖励] 气泡自动消失，进入冷却状态 时间{bubbleSpawnInterval}");
        }

        private void OpenBubbleRewardUIForm(float rewardValue)
        {
            var uiParams = UIParams.Create();
            var varReward = ReferencePool.Acquire<VarSingle>();
            varReward.Value = rewardValue;
            uiParams.Set(ArrowRewardUIForm.P_RewardValue, varReward);
            GF.UI.OpenUIForm(UIViews.ArrowRewardUIForm, uiParams);
        }

        /// <summary>
        /// 重置气泡奖励状态
        /// </summary>
        private void ResetBubbleReward()
        {
            if (m_CurrentBubble != null && m_CurrentBubble.gameObject != null)
            {
                UnspawnItem<UIItemObject>(varBubbleRewardItem, m_CurrentBubble.gameObject);
            }

            m_CurrentBubble = null;
            // 重置状态
            m_IsBubbleActive = false;
            m_BubbleLifetimeTimer = 0f;
            m_BubbleTimer = 0f;
            m_IsBubbleCooldown = true;  // 关闭界面时进入冷却
            m_BubbleCooldownTimer = 0f;

            // Log.Info("[气泡奖励] 气泡奖励状态已重置");
        }

        private bool ShouldShowBubbleReward()
        {
            return GetDisplayLinearLevelId() >= BubbleUnlockLevel;
        }
    }
}