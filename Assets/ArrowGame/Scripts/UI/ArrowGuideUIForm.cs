using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/GuideUIForm")]
public partial class ArrowGuideUIForm : UIFormBase
{
    private GuideUIFormData m_GuideData;
    private RectTransform m_CanvasRectTransform;
    private ArrowGuideTable m_GuideTable;
    private Vector2 m_TargetCenter;   // 目标中心（引导界面本地坐标）
    private float m_TargetTopY;       // 目标顶部 Y（用于引导4说明文本）
    private bool m_IsClosing;
    private float m_WeakGuideIgnoreInputUntil;

    // 遮罩的边距，用于调整镂空区域的大小
    private const float MASK_PADDING = 10f;
    private const float FINGER_VERTICAL_OFFSET = 80f; // 手指相对目标向上偏移
    private const float WeakGuideInputDelay = 0.1f;


    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 获取Canvas组件
        // m_Canvas = GetComponentInParent<Canvas>();
        // if (m_Canvas != null)
        // {
        m_CanvasRectTransform = GFBuiltin.RootCanvas.GetComponent<RectTransform>();
        // }

        // 获取UI相机
        // m_UICamera = m_Canvas?.worldCamera;
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Log.Info($"GuideUIForm: SerialId:{this.UIForm.SerialId}");

        m_IsClosing = false;
        if (userData is UIParams uiParams)
        {
            int guideId = uiParams.Get<VarInt32>("GuideId");
            bool showFinger = uiParams.Get<VarBoolean>("ShowFinger");
            Transform targetTransform = uiParams.Get<VarTransform>("TargetTransform");
            Action onComplete = uiParams.Get<VarAction>("OnComplete");
            Vector2 fingerPos = uiParams.Get<VarVector2>("FingerPos");
            m_GuideData = new GuideUIFormData(guideId, showFinger, targetTransform, onComplete, fingerPos);
            m_GuideTable = GF.DataTable.GetDataTable<ArrowGuideTable>().GetDataRow(guideId);
            SetupGuide();
        }
        else
        {
            Log.Error("GuideUIForm: Invalid userData, expected GuideUIFormData");
            GF.UI.Close(this.UIForm);
        }
        // 强制引导才拦截点击；弱引导改为穿透，在 OnUpdate 里任意点击结束
        if (!IsWeakGuide() && varClickImage != null)
            varClickImage.onClick.AddListener(OnGuideClick);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        m_GuideData = null;
        m_GuideTable = null;
        // 清理点击事件
        if (varClickImage != null)
        {
            varClickImage.onClick.RemoveListener(OnGuideClick);
        }
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 设置引导
    /// </summary>
    private void SetupGuide()
    {
        if (m_GuideData?.targetTransform == null)
        {
            Log.Error("GuideUIForm: Target transform is null");
            GF.UI.Close(this.UIForm);
            return;
        }
        
        if (m_GuideData.GuideId == 2)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_enter);
        }

        // 设置遮罩镂空
        SetupMaskHollow();

        // 设置引导手指
        SetupGuideFinger();

        // 设置引导提示
        SetupTips();

        if (IsWeakGuide())
            SetupWeakGuidePassthrough();
        else
            SetupForceGuideInputBlock();
    }

    private bool IsWeakGuide()
    {
        return m_GuideTable != null && m_GuideTable.GuideType == 2;
    }

    /// <summary>
    /// 弱引导不拦截射线，任意点击结束引导并让事件落到下层（箭头/按钮）。
    /// </summary>
    private void SetupWeakGuidePassthrough()
    {
        m_WeakGuideIgnoreInputUntil = Time.unscaledTime + WeakGuideInputDelay;

        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = false;

        if (varClickImage != null)
        {
            varClickImage.enabled = false;
            if (varClickImage.targetGraphic != null)
                varClickImage.targetGraphic.raycastTarget = false;
        }

        if (varMaskArr != null)
        {
            for (int i = 0; i < varMaskArr.Length; i++)
            {
                if (varMaskArr[i] == null)
                    continue;
                var img = varMaskArr[i].GetComponent<Image>();
                if (img != null)
                    img.raycastTarget = false;
            }
        }

        if (varTipsBg != null)
            varTipsBg.raycastTarget = false;
        if (varTipsTxt != null)
            varTipsTxt.raycastTarget = false;
        if (varFinger != null)
            varFinger.raycastTarget = false;
        if (varFinger_2 != null)
            varFinger_2.raycastTarget = false;
    }

    /// <summary>
    /// 强制引导恢复射线拦截（界面可能复用，需从弱引导状态还原）。
    /// </summary>
    private void SetupForceGuideInputBlock()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = true;

        if (varClickImage != null)
        {
            varClickImage.enabled = true;
            if (varClickImage.targetGraphic != null)
                varClickImage.targetGraphic.raycastTarget = true;
        }

        if (varMaskArr != null)
        {
            for (int i = 0; i < varMaskArr.Length; i++)
            {
                if (varMaskArr[i] == null)
                    continue;
                var img = varMaskArr[i].GetComponent<Image>();
                if (img != null)
                    img.raycastTarget = true;
            }
        }
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        TryCompleteWeakGuideOnAnyClick();
    }

    private void TryCompleteWeakGuideOnAnyClick()
    {
        if (!IsWeakGuide() || m_IsClosing || m_GuideData == null)
            return;
        if (Time.unscaledTime < m_WeakGuideIgnoreInputUntil)
            return;
        if (!HasPointerDownThisFrame())
            return;

        OnGuideClick();
    }

    private static bool HasPointerDownThisFrame()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Began;
        return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 设置遮罩镂空
    /// </summary>
    private void SetupMaskHollow()
    {
        if (varMaskArr == null || varMaskArr.Length < 4)
        {
            Log.Error("GuideUIForm: Mask array is null or insufficient");
            return;
        }
        if (m_GuideTable != null)
        {
            if (m_GuideTable.MaskType == 1)
            {
                varMaskArr[0].GetComponent<Image>().color = new Color(0, 0, 0, 0);
                varMaskArr[1].GetComponent<Image>().color = new Color(0, 0, 0, 0);
                varMaskArr[2].GetComponent<Image>().color = new Color(0, 0, 0, 0);
                varMaskArr[3].GetComponent<Image>().color = new Color(0, 0, 0, 0);
            }
            else if (m_GuideTable.MaskType == 2)
            {
                varMaskArr[0].GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
                varMaskArr[1].GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
                varMaskArr[2].GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
                varMaskArr[3].GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            }
        }
        // 将屏幕边界转换为引导界面的本地坐标（Screen Space Camera 需传入 UI 相机）
        Camera uiCam = GFBuiltin.RootCanvas != null && GFBuiltin.RootCanvas.renderMode == RenderMode.ScreenSpaceCamera
            ? GFBuiltin.RootCanvas.worldCamera : null;

        // 获取目标的世界坐标
        Vector3 targetWorldPos = m_GuideData.targetTransform.position;

        // 获取目标的RectTransform组件（UI 目标）；若无则按世界坐标目标处理（如箭头实体）
        RectTransform targetRectTransform = m_GuideData.targetTransform.GetComponent<RectTransform>();
        Vector3[] targetWorldCorners = new Vector3[4];
        if (targetRectTransform != null)
        {
            targetRectTransform.GetWorldCorners(targetWorldCorners);
        }
        else
        {
            // 世界坐标目标：以目标位置为中心，用相机 right/up 构建矩形（边长约 1 世界单位）

            float halfSize = 0.6f;
            Vector3 right = (uiCam != null ? uiCam.transform.right : Vector3.right) * halfSize;
            Vector3 up = (uiCam != null ? uiCam.transform.up : Vector3.up) * halfSize;
            targetWorldCorners[0] = targetWorldPos - right - up;
            targetWorldCorners[1] = targetWorldPos + right - up;
            targetWorldCorners[2] = targetWorldPos + right + up;
            targetWorldCorners[3] = targetWorldPos - right + up;
        }

        // 将世界边界转换为屏幕坐标（世界坐标目标用 Camera.main，UI 目标用 null 即 Canvas 相机）
        Camera cornerCam = targetRectTransform == null ? Camera.main : uiCam;
        Vector2[] targetScreenCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            targetScreenCorners[i] = RectTransformUtility.WorldToScreenPoint(cornerCam, targetWorldCorners[i]);
        }


        Vector2[] targetLocalCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(this.GetComponent<RectTransform>(), targetScreenCorners[i], uiCam, out targetLocalCorners[i]);
        }

        // 计算目标在本地坐标系中的边界
        float minX = Mathf.Min(targetLocalCorners[0].x, targetLocalCorners[1].x, targetLocalCorners[2].x, targetLocalCorners[3].x);
        float maxX = Mathf.Max(targetLocalCorners[0].x, targetLocalCorners[1].x, targetLocalCorners[2].x, targetLocalCorners[3].x);
        float minY = Mathf.Min(targetLocalCorners[0].y, targetLocalCorners[1].y, targetLocalCorners[2].y, targetLocalCorners[3].y);
        float maxY = Mathf.Max(targetLocalCorners[0].y, targetLocalCorners[1].y, targetLocalCorners[2].y, targetLocalCorners[3].y);

        // 添加边距
        float leftMaxX = minX - MASK_PADDING;  // 目标左边界
        float rightMinX = maxX + MASK_PADDING; // 目标右边界
        float bottomMaxY = minY - MASK_PADDING; // 目标下边界
        float topMinY = maxY + MASK_PADDING;   // 目标上边界

        // 计算目标中心位置（用于点击区域）
        Vector2 targetCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 targetSize = new Vector2(maxX - minX, maxY - minY);
        m_TargetCenter = targetCenter;
        m_TargetTopY = topMinY;

        // 设置点击区域：弱引导(GuideType=2)允许点击任意位置关闭，设为全屏
        if (varClickImage != null)
        {
            RectTransform clickRect = varClickImage.GetComponent<RectTransform>();
            if (m_GuideTable != null && m_GuideTable.GuideType == 2)
            {
                clickRect.anchorMin = Vector2.zero;
                clickRect.anchorMax = Vector2.one;
                clickRect.offsetMin = Vector2.zero;
                clickRect.offsetMax = Vector2.zero;
            }
            else
            {
                varClickImage.transform.localPosition = targetCenter;
                clickRect.sizeDelta = targetSize + new Vector2(MASK_PADDING * 2, MASK_PADDING * 2);
            }
        }

        // 获取Canvas的实际尺寸作为界面适配尺寸
        Vector2 canvasSize = Vector2.zero;
        if (m_CanvasRectTransform != null)
        {
            canvasSize = m_CanvasRectTransform.sizeDelta;
            Log.Info($"canvasSize:{canvasSize}");
        }
        else
        {
            // 如果无法获取Canvas尺寸，则使用设计分辨率作为备选
            canvasSize = AppSettings.Instance.DesignResolution;
        }

        // 设置四个方向的遮罩位置和尺寸
        // 上遮罩：覆盖目标上方的区域
        float topMaskPosY = (topMinY + canvasSize.y * 0.5f) * 0.5f;
        float topMaskHeight = canvasSize.y * 0.5f - topMinY;
        SetMaskRect(varMaskArr[0], 0, topMaskPosY, canvasSize.x, topMaskHeight);

        // 下遮罩：覆盖目标下方的区域
        float bottomMaskPosY = (bottomMaxY - canvasSize.y * 0.5f) * 0.5f;
        float bottomMaskHeight = bottomMaxY - (-canvasSize.y * 0.5f);
        SetMaskRect(varMaskArr[1], 0, bottomMaskPosY, canvasSize.x, bottomMaskHeight);

        // 左遮罩：覆盖目标左侧的区域
        float leftMaskPosX = ((-canvasSize.x * 0.5f) + leftMaxX) * 0.5f;
        float leftMaskWidth = leftMaxX - (-canvasSize.x * 0.5f);
        SetMaskRect(varMaskArr[2], leftMaskPosX, targetCenter.y, leftMaskWidth, topMinY - bottomMaxY);

        // 右遮罩：覆盖目标右侧的区域
        float rightMaskPosX = (rightMinX + canvasSize.x * 0.5f) * 0.5f;
        float rightMaskWidth = canvasSize.x * 0.5f - rightMinX;
        SetMaskRect(varMaskArr[3], rightMaskPosX, targetCenter.y, rightMaskWidth, topMinY - bottomMaxY);
    }

    /// <summary>
    /// 设置遮罩矩形
    /// </summary>
    private void SetMaskRect(RectTransform mask, float centerX, float centerY, float width, float height)
    {
        if (mask == null) return;

        // 根据锚点位置计算正确的anchoredPosition
        Vector2 anchorMin = mask.anchorMin;
        Vector2 anchorMax = mask.anchorMax;

        // 计算锚点偏移
        float anchorOffsetX = (anchorMax.x - anchorMin.x) * 0.5f + anchorMin.x;
        float anchorOffsetY = (anchorMax.y - anchorMin.y) * 0.5f + anchorMin.y;

        // 计算相对于锚点的位置
        float posX = centerX - (anchorOffsetX - 0.5f) * width;
        float posY = centerY - (anchorOffsetY - 0.5f) * height;

        // 设置遮罩的位置和尺寸
        mask.anchoredPosition = new Vector2(posX, posY);
        mask.sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// 是否展示手指引导动画（需同时满足运行时参数与配置表 FingerShow）
    /// </summary>
    private bool ShouldShowFinger()
    {
        if (!m_GuideData.isShowShou) return false;
        if (m_GuideTable != null && !m_GuideTable.FingerShow) return false;
        return true;
    }

    /// <summary>
    /// 设置引导手指
    /// </summary>
    private void SetupGuideFinger()
    {
        bool showFinger = ShouldShowFinger();

        if (m_GuideData.GuideId == 4)
        {
            // 引导4：双指缩放，特殊处理
            SetupPinchGuideFinger(showFinger);
            return;
        }

        if (varFinger == null) return;

        // 控制手指显示
        varFinger.gameObject.SetActive(showFinger);
        if (varFinger_2 != null) varFinger_2.gameObject.SetActive(false);

        if (showFinger && m_GuideData.targetTransform != null)
        {
            SetupFingerPosition();
        }
    }

    /// <summary>
    /// 引导4：双指缩放，隐藏 finger，显示 finger_2（双指动画由 Skeleton 控制）
    /// </summary>
    private void SetupPinchGuideFinger(bool showFinger)
    {
        if (varFinger != null) varFinger.gameObject.SetActive(false);
        if (varFinger_2 == null) return;

        varFinger_2.gameObject.SetActive(showFinger);
        if (!showFinger) return;

        RectTransform parentRect = varFinger_2.rectTransform.parent as RectTransform;
        RectTransform guideRect = this.GetComponent<RectTransform>();
        if (parentRect == null || guideRect == null) return;

        Vector2 localCenter = (parentRect == guideRect)
            ? m_TargetCenter
            : (Vector2)parentRect.InverseTransformPoint(guideRect.TransformPoint(m_TargetCenter));

        varFinger_2.rectTransform.anchoredPosition = localCenter + new Vector2(0f, FINGER_VERTICAL_OFFSET);
    }

    /// <summary>
    /// 设置手指位置（基于 targetTransform 转为 UI 本地坐标）
    /// </summary>
    private void SetupFingerPosition()
    {
        if (varFinger == null || m_GuideData.targetTransform == null) return;

        // 世界坐标目标(如箭头)用 Camera.main，UI 目标用 Canvas 相机
        bool isWorldTarget = m_GuideData.targetTransform.GetComponent<RectTransform>() == null;
        Camera uiCam = GFBuiltin.RootCanvas != null && GFBuiltin.RootCanvas.renderMode == RenderMode.ScreenSpaceCamera
            ? GFBuiltin.RootCanvas.worldCamera : null;
        Camera worldCam = isWorldTarget ? Camera.main : uiCam;
        Vector3 targetScreenPos = RectTransformUtility.WorldToScreenPoint(worldCam, m_GuideData.targetTransform.position);

        // 转换为 varFinger 父节点的本地坐标，需传入 UI 相机
        RectTransform parentRect = varFinger.rectTransform.parent as RectTransform;
        if (parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPos, uiCam, out Vector2 localPos);

        Vector2 fingerOffsetPos = new Vector2(50f, -80f + FINGER_VERTICAL_OFFSET);
        varFinger.rectTransform.anchoredPosition = localPos + fingerOffsetPos;
    }

    private void SetupTips()
    {
        if (varTipsBg == null || varTipsTxt == null) return;
        if (m_GuideTable != null)
        {
            varTipsBg.gameObject.SetActive(m_GuideTable.TipsShow);
            if (m_GuideTable.TipsShow)
            {
                varTipsTxt.text = GF.Localization.GetString(m_GuideTable.TipsKey);
                if (m_GuideData.GuideId == 4)
                {
                    // 引导4：说明文本在 targetTransform 顶部
                    float tipsOffsetY = 60f;
                    varTipsBg.rectTransform.anchoredPosition = new Vector2(m_TargetCenter.x, m_TargetTopY + tipsOffsetY);
                }
                else if (m_GuideTable.TipsPos == Vector2.zero)
                {
                    varTipsBg.transform.localPosition = varFinger != null ? varFinger.transform.localPosition - new Vector3(-200, 200, 0) : Vector3.zero;
                }
                else
                {
                    varTipsBg.rectTransform.anchoredPosition = m_GuideTable.TipsPos;
                }
            }
        }
    }


    /// <summary>
    /// 引导点击事件
    /// </summary>
    private void OnGuideClick()
    {
        if (m_IsClosing)
            return;
        m_IsClosing = true;

        RecordGuideComplete();
        m_GuideData?.onComplete?.Invoke();
        GF.UI.Close(this.UIForm);
    }

    /// <summary>
    /// 记录引导完成
    /// </summary>
    private void RecordGuideComplete()
    {
        if (m_GuideData == null) return;

        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData != null)
        {
            // 检查是否已经完成过这个引导
            if (!IsGuideCompleted(m_GuideData.GuideId))
            {
                // 添加到完成列表
                AddCompletedGuide(m_GuideData.GuideId);
                // DataCollectManager.Instance.LogEvent("login",new Dictionary<string, object>(){
                //     {"guideid", m_GuideData.GuideId}
                // });
                Log.Info($"引导 {m_GuideData.GuideId} 已完成");
            }
        }
    }

    /// <summary>
    /// 检查引导是否已完成
    /// </summary>
    public static bool IsGuideCompleted(int guideId)
    {
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData?.CompleteGuideIds == null) return false;

        return playerData.CompleteGuideIds.Contains(guideId);
    }

    /// <summary>
    /// 添加已完成的引导
    /// </summary>
    public static void AddCompletedGuide(int guideId)
    {
        var playerData = GF.DataModel.GetDataModel<PlayerDataModel>();
        if (playerData == null) return;
        
        if (guideId == 2)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_1);
        }
        else if (guideId == 3)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_2);
        }
        else if (guideId == 5)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_3);
        }
        else if (guideId == 6)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_4);
        }
        else if (guideId == 7)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_5);
        }
        else if (guideId == 1)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_6);
        }
        else if (guideId == 4)
        {
            CommonHelper.LogEvent(AdjustEventCodeEvent.guide_step_7);
        }


        // 检查是否已经存在
        if (IsGuideCompleted(guideId)) return;
        playerData.CompleteGuideIds.Add(guideId);
        playerData.Save();
    }




}

public class GuideUIFormData
{
    public int GuideId;
    public bool isShowShou = false;
    public Transform targetTransform;
    public Action onComplete = null;
    public Vector2 fingerPos = default;

    public GuideUIFormData(int guideId, bool isShowShou, Transform targetTransform, Action onComplete, Vector2 fingerPos = default)
    {
        GuideId = guideId;
        this.isShowShou = isShowShou;
        this.targetTransform = targetTransform;
        this.onComplete = onComplete;
        this.fingerPos = fingerPos;
    }
}