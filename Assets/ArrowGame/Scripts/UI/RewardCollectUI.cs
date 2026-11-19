using System;
using System.Collections.Generic;
using DG.Tweening;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/RewardCollectUI")]
public sealed class RewardCollectUI : UIFormBase
{
    private sealed class Flight
    {
        public MonoBehaviour Owner;
        public PlayerDataType Type;
        public RewardCollectTarget Target;
        public readonly List<ArrowFlyRewardItem> Items = new List<ArrowFlyRewardItem>();
        public int Remaining;
        public Action Complete;
        public GameObject Effect;
    }
    private static RewardCollectUI instance;
    private readonly List<Flight> flights = new List<Flight>();
    [SerializeField] private GameObject itemPrefab;
    private Canvas canvas;
    private RectTransform rect;

    public static void EnsureOpen()
    {
        if (GF.UI.HasUIForm(UIViews.RewardCollectUI) || GF.UI.IsLoadingUIForm(UIViews.RewardCollectUI)) return;
        GF.UI.OpenUIForm(UIViews.RewardCollectUI);
    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        canvas = UICanvas;
        rect = (RectTransform)transform;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        instance = this;
        GF.Event.Subscribe(RewardCollectedEventArgs.EventId, OnRewardCollected);
    }

    protected override void OnOpenAnimationComplete()
    {
        base.OnOpenAnimationComplete();
        Interactable = false;
    }

    private static Camera CameraFor(Canvas canvas) => canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
        ? canvas.rootCanvas.worldCamera : null;

    public static bool Play(MonoBehaviour owner, PlayerDataType type, Vector3 localStart, Transform source,
        int count, Action complete = null, Coffee.UIExtensions.UIParticle effect = null)
    {
        var ui = instance;
        var target = RewardCollectTarget.Find(type);
        if (ui == null || !ui.Visible || !ui.isActiveAndEnabled || ui.itemPrefab == null || target == null || source == null || count <= 0) return false;
        var screenStart = RectTransformUtility.WorldToScreenPoint(CameraFor(source.GetComponentInParent<Canvas>()), source.TransformPoint(localStart));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.rect, screenStart, CameraFor(ui.canvas), out Vector2 start);
        var screenEnd = RectTransformUtility.WorldToScreenPoint(CameraFor(target.ParentCanvas), target.Rect.TransformPoint(target.Rect.rect.center));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.rect, screenEnd, CameraFor(ui.canvas), out Vector2 end);
        var flight = new Flight { Owner = owner, Type = type, Target = target, Remaining = count, Complete = complete };
        target.Raise(ui.canvas.sortingOrder - 1, ui.canvas.sortingLayerID);
        ui.flights.Add(flight);
        if (effect != null)
        {
            var copy = Instantiate(effect, ui.rect);
            flight.Effect = copy.gameObject;
            copy.transform.localPosition = start;
            copy.gameObject.SetActive(true);
            copy.Play();
        }
        for (int i = 0; i < count; i++)
        {
            var spawned = ui.SpawnItem<UIItemObject>(ui.itemPrefab, ui.rect);
            var item = spawned.itemLogic as ArrowFlyRewardItem;
            if (item == null || item.rectTransform == null || item.rewardIcon == null)
            {
                ui.UnspawnItem<UIItemObject>(ui.itemPrefab, spawned.gameObject);
                ui.Finish(flight, false);
                return false;
            }
            flight.Items.Add(item);
            item.gameObject.SetActive(true);
            item.rewardIcon.raycastTarget = false;
            item.rectTransform.anchoredPosition = start + (count > 1 ? UnityEngine.Random.insideUnitCircle * 110f : Vector2.zero);
            item.InitReward(type, end, () =>
            {
                if (--flight.Remaining == 0) ui.Finish(flight, true);
            }, 0.02f * i, i);
        }
        return true;
    }

    private void Finish(Flight flight, bool notify)
    {
        if (!flights.Remove(flight)) return;
        foreach (var item in flight.Items)
        {
            if (item == null) continue;
            DOTween.Kill(item);
            UnspawnItem<UIItemObject>(itemPrefab, item.gameObject);
        }
        if (flight.Effect != null) Destroy(flight.Effect);
        if (flight.Target != null) flight.Target.Release();
        if (notify && flight.Owner != null && flight.Owner.isActiveAndEnabled) flight.Complete?.Invoke();
    }

    public static void Cancel(MonoBehaviour owner, PlayerDataType? type = null)
    {
        if (instance == null) return;
        foreach (var flight in instance.flights.ToArray())
            if (flight.Owner == owner && (!type.HasValue || flight.Type == type.Value)) instance.Finish(flight, false);
    }

    public static void CancelTarget(RewardCollectTarget target)
    {
        if (instance == null) return;
        foreach (var flight in instance.flights.ToArray())
            if (flight.Target == target) instance.Finish(flight, true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (instance == this) instance = null;
        GF.Event.Unsubscribe(RewardCollectedEventArgs.EventId, OnRewardCollected);
        foreach (var flight in flights.ToArray()) Finish(flight, !isShutdown);
        base.OnClose(isShutdown, userData);
    }

    protected override void InternalSetVisible(bool visible)
    {
        base.InternalSetVisible(visible);
        if (!visible)
            foreach (var flight in flights.ToArray()) Finish(flight, true);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        for (int i = flights.Count - 1; i >= 0; i--)
        {
            var flight = flights[i];
            if (flight.Owner == null || !flight.Owner.isActiveAndEnabled) Finish(flight, false);
        }
    }
    private void OnRewardCollected(object sender, GameEventArgs e)
    {
        if (!(e is RewardCollectedEventArgs args) || args.type != PlayerDataType.PhoneFragment || !args.PlayFlyAnimation) return;
        // Phone fragments appear around the screen center, not screen pixel (0, 0).
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,
            screenCenter, CameraFor(canvas), out Vector2 start);
        Play(this, args.type, start, rect, Mathf.RoundToInt(args.Value));
    }
}
