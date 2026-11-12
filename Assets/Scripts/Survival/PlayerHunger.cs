using System;
using UnityEngine;

public class PlayerHunger : MonoBehaviour
{
    public PlayerHealth health;
    [SerializeField, Min(1)] float maximumHunger = 100;
    [SerializeField, Min(0)] float startingHunger = 100, depletionPerSecond = .2f;
    [SerializeField, Min(.1f)] float starvationDamage = 5, starvationInterval = 5;
    public float CurrentHunger { get; private set; }
    public float MaxHunger => Safe(maximumHunger, 100, 1);
    public float NormalizedHunger => CurrentHunger / MaxHunger;
    public bool IsStarving => CurrentHunger <= 0;
    public event Action Changed;
    double starvationElapsed;
    static float Safe(float value, float fallback, float minimum = 0) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(minimum, value);
    void Awake()
    {
        if (!health) health = GetComponent<PlayerHealth>();
        CurrentHunger = Mathf.Clamp(Safe(startingHunger, 100), 0, MaxHunger);
        if (!health) { Debug.LogError("PlayerHunger needs PlayerHealth.", this); enabled = false; }
    }
    void Update() => Advance(Time.deltaTime);
    public void Advance(float seconds)
    {
        if (!health || health.IsDead || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        double remaining = seconds;
        float rate = Safe(depletionPerSecond, .2f);
        if (CurrentHunger > 0)
        {
            if (rate <= 0) return;
            double fedTime = Math.Min(remaining, CurrentHunger / (double)rate);
            CurrentHunger = Mathf.Max(0, CurrentHunger - (float)(fedTime * rate));
            remaining -= fedTime;
            if (remaining > 0) CurrentHunger = 0;
            Changed?.Invoke();
        }
        if (!IsStarving) return;
        starvationElapsed += remaining;
        double interval = Safe(starvationInterval, 5, .1f);
        double ticks = Math.Floor(starvationElapsed / interval);
        if (ticks < 1) return;
        starvationElapsed -= ticks * interval;
        // A long frame applies the exact accumulated interval damage in one bounded call.
        health.TakeDamage((float)Math.Min(health.CurrentHealth, ticks * Safe(starvationDamage, 5, .1f)), "Starvation");
    }
    public float Restore(float amount)
    {
        if (!health || health.IsDead || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return 0;
        float applied = Mathf.Min(MaxHunger - CurrentHunger, amount);
        CurrentHunger += applied;
        if (CurrentHunger > 0) starvationElapsed = 0;
        if (applied > 0) Changed?.Invoke();
        return applied;
    }
    public void SetHungerForTesting(float value)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        CurrentHunger = Mathf.Clamp(value, 0, MaxHunger);
        starvationElapsed = 0;
        Changed?.Invoke();
#endif
    }
}
