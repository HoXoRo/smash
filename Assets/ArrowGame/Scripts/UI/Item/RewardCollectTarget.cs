using System.Collections.Generic;
using UnityEngine;

// Keeps layout/parenting intact while temporarily raising the target above other UI.
public sealed class RewardCollectTarget : MonoBehaviour
{
    private static readonly List<RewardCollectTarget> targets = new List<RewardCollectTarget>();
    private PlayerDataType type;
    private bool configured;
    private Canvas targetCanvas;
    private bool wasEnabled, wasOverride;
    private int oldOrder, oldLayer, users;
    public RectTransform Rect => transform as RectTransform;
    public Canvas ParentCanvas => transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;

    public static void Bind(PlayerDataType type, RectTransform rect)
    {
        if (rect == null) return;
        var target = rect.GetComponent<RewardCollectTarget>();
        if (target == null) target = rect.gameObject.AddComponent<RewardCollectTarget>();
        target.type = type;
        target.configured = true;
        if (target.isActiveAndEnabled && !targets.Contains(target)) targets.Add(target);
    }

    public static RewardCollectTarget Find(PlayerDataType type)
    {
        RewardCollectTarget best = null;
        int order = int.MinValue;
        foreach (var target in targets)
        {
            if (target == null || !target.isActiveAndEnabled || target.type != type) continue;
            var canvas = target.ParentCanvas;
            int candidate = canvas != null ? canvas.sortingOrder : 0;
            if (best == null || candidate >= order) { best = target; order = candidate; }
        }
        return best;
    }

    public void Raise(int order, int layer)
    {
        if (users++ > 0) return;
        var parentCanvas = ParentCanvas;
        // Nested canvases are culled by their own GameObject layer. These icon prefabs
        // use Default, while the UI camera only renders UI; keep the reusable canvas on UI.
        if (parentCanvas != null) gameObject.layer = parentCanvas.gameObject.layer;
        targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            targetCanvas = gameObject.AddComponent<Canvas>();
            if (parentCanvas != null)
                targetCanvas.additionalShaderChannels = parentCanvas.additionalShaderChannels;
        }
        wasEnabled = targetCanvas.enabled;
        wasOverride = targetCanvas.overrideSorting;
        oldOrder = targetCanvas.sortingOrder;
        oldLayer = targetCanvas.sortingLayerID;
        targetCanvas.enabled = true;
        targetCanvas.overrideSorting = true;
        targetCanvas.sortingLayerID = layer;
        targetCanvas.sortingOrder = order;
        RefreshGraphics();
        // A new nested Canvas needs its own raycaster to keep buttons interactive.
        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            var addedRaycaster = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var parentRaycaster = ParentCanvas != null ? ParentCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() : null;
            if (parentRaycaster != null)
            {
                addedRaycaster.ignoreReversedGraphics = parentRaycaster.ignoreReversedGraphics;
                addedRaycaster.blockingObjects = parentRaycaster.blockingObjects;
                addedRaycaster.blockingMask = parentRaycaster.blockingMask;
            }
        }
    }

    public void Release()
    {
        if (users == 0 || --users > 0) return;
        Restore();
    }

    private void Restore()
    {
        users = 0;
        if (targetCanvas == null) return;
        targetCanvas.sortingLayerID = oldLayer;
        targetCanvas.sortingOrder = oldOrder;
        targetCanvas.overrideSorting = wasOverride;
        targetCanvas.enabled = wasEnabled;
        // Newly added canvases return to inherited sorting and remain usable for rendering/raycasting.
        // Reuse them rather than scheduling Destroy, which can race another collection this frame.
        targetCanvas = null;
        RefreshGraphics();
    }

    private void RefreshGraphics()
    {
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            if (graphic is UnityEngine.UI.MaskableGraphic maskable)
            {
                maskable.RecalculateClipping();
                maskable.RecalculateMasking();
            }
            graphic.SetAllDirty();
        }
    }

    private void OnEnable() { if (configured && !targets.Contains(this)) targets.Add(this); }
    private void OnDisable()
    {
        targets.Remove(this);
        RewardCollectUI.CancelTarget(this);
        if (users > 0) Restore();
    }
}
