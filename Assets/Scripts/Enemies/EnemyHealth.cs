using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Min(1)]
    public float maximumHealth = 30;
    public float MaxHealth => EnemyArchetype.Safe(maximumHealth, 30, 1, 10000);
    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsDead => !IsAlive;
    public WeaponHitKind LastHitKind { get; private set; }

    public event Action Died;
    /// <summary>Raised after damage is applied: (amount dealt, attacker position).</summary>
    public event Action<float, Vector2> Damaged;
    public Func<float, Vector2, float> damageModifier;
    void Awake()
    {
        if (CurrentHealth <= 0)
            CurrentHealth = MaxHealth;
    }

    public void Initialize(float value)
    {
        maximumHealth = EnemyArchetype.Safe(value, 30, 1, 10000);
        CurrentHealth = MaxHealth;
    }

    public bool TakeDamage(float amount, Vector2 source, float knockback = 2.5f, WeaponHitKind hitKind = WeaponHitKind.Slash)
    {
        if (IsDead || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0)
            return false;
        if (damageModifier != null)
        {
            amount = damageModifier(amount, source);
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return false;
        }
        LastHitKind = hitKind;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        AudioManager.Instance?.PlayEnemyHit();
        Damaged?.Invoke(amount, source);
        var controller = GetComponent<EnemyController>();
        if (controller) controller.React(source, Mathf.Clamp(knockback, 0, 10));
        if (IsDead)
        {
            Died?.Invoke();
            if (controller) controller.Defeat();
        }

        return true;
    }
}
