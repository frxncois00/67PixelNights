using System;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] float maximumHealth = 100;
    [SerializeField, Min(0)] float startingHealth = 100;
    public float MaxHealth => Valid(maximumHealth) ? Mathf.Clamp(maximumHealth, 1, 100000) : 100;
    public float CurrentHealth { get; private set; }
    public float NormalizedHealth => CurrentHealth / MaxHealth;
    public bool IsAlive => CurrentHealth > 0;
    public bool IsDead => !IsAlive;
    public string CauseOfDeath { get; private set; } = "";
    public event Action Changed, Died;
    public event Action<float> Damaged, Healed;
    bool deathRaised;
    static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    void Awake() => CurrentHealth = Valid(startingHealth) ? Mathf.Clamp(startingHealth, 0, MaxHealth) : MaxHealth;
    void Start() { Changed?.Invoke(); if (IsDead) Die("Unknown"); }
    public float TakeDamage(float amount, string cause = "Unknown")
    {
        if (IsDead || !Valid(amount) || amount <= 0) return 0;
        float applied = Mathf.Min(CurrentHealth, amount);
        CurrentHealth -= applied;
        AudioManager.Instance?.PlayPlayerHurt();
        if (IsDead) CauseOfDeath = cause;
        Changed?.Invoke();
        Damaged?.Invoke(applied);
        if (IsDead) Die(cause);
        return applied;
    }
    public float Heal(float amount)
    {
        if (IsDead || !Valid(amount) || amount <= 0) return 0;
        float applied = Mathf.Min(MaxHealth - CurrentHealth, amount);
        if (applied <= 0) return 0;
        CurrentHealth += applied;
        Changed?.Invoke();
        Healed?.Invoke(applied);
        return applied;
    }
    void Die(string cause)
    {
        if (deathRaised) return;
        deathRaised = true;
        CauseOfDeath = cause;
        Died?.Invoke();
    }
    public void SetHealthForTesting(float amount)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!Valid(amount) || deathRaised) return;
        if (amount < CurrentHealth) TakeDamage(CurrentHealth - Mathf.Max(0, amount), "Testing");
        else Heal(amount - CurrentHealth);
#endif
    }
}
