using System;

public static class LifeTrackerHelper
{
    public static Action<int> OnLifeCountChanged;

    public static Action<int> OnLifeTimerChanged;

    public static void AddLifeCountTracker(Action<int> listener)
    {
        OnLifeCountChanged = (Action<int>)Delegate.Combine(OnLifeCountChanged, listener);
        TriggerLifeCountTrackers();
    }

    public static void RemoveLifeCountTracker(Action<int> listener)
    {
        OnLifeCountChanged = (Action<int>)Delegate.Remove(OnLifeCountChanged, listener);
    }

    public static void AddLifeTimerTracker(Action<int> listener)
    {
        OnLifeTimerChanged = (Action<int>)Delegate.Combine(OnLifeTimerChanged, listener);
        TriggerLifeTimerTrackers();
    }

    public static void RemoveLifeTimerTracker(Action<int> listener)
    {
        OnLifeTimerChanged = (Action<int>)Delegate.Remove(OnLifeTimerChanged, listener);
    }

    public static void TriggerLifeCountTrackers()
    {
        OnLifeCountChanged?.Invoke(LifeHelper.GetCurrentLifeCount());
    }

    public static void TriggerLifeTimerTrackers()
    {
        OnLifeTimerChanged?.Invoke(LifeHelper.GetSecondsUntilNextLife());
    }
}