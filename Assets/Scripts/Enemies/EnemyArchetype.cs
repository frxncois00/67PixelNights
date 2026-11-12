using UnityEngine;

public enum EnemyBehaviorProfile
{
    Stalker,
    Wolf,
    Crawler,
    Brute,
    Ancient
}

[CreateAssetMenu(menuName = "Survival/Enemy Archetype")]
public class EnemyArchetype : ScriptableObject
{
    public string id, displayName;
    public EnemyBehaviorProfile behavior;
    public float health = 30, patrolSpeed = 1.2f, chaseSpeed = 2, detectionRange = 5, attackRange = 1, damage = 10, cooldown = 2, windup = .35f, recovery = .3f, structureDamage = 8;
    [Range(0, 1)]
    public float knockbackResistance;
    public int firstNight = 1, maximumActive = 10;
    public float spawnWeight = 1;
    public Sprite sprite;
    public Color tint = Color.white;
    public Sprite[] idleFrames, walkFrames;
    public float animationFPS = 8;
    public float burstDuration = .7f, burstCooldown = 4, burstMultiplier = 1.4f, lungeDistance = 1.5f, slamRadius = 1.6f;
    public static float Safe(float value, float fallback, float min, float max) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    public void Validate()
    {
        health = Safe(health, 30, 1, 10000);
        patrolSpeed = Safe(patrolSpeed, 1.2f, 0, 6);
        chaseSpeed = Safe(chaseSpeed, 2, 0, 6);
        detectionRange = Safe(detectionRange, 5, .1f, 20);
        attackRange = Safe(attackRange, 1, .1f, 3);
        damage = Safe(damage, 10, 0, 100);
        cooldown = Safe(cooldown, 2, .2f, 30);
        windup = Safe(windup, .35f, .15f, 5);
        recovery = Safe(recovery, .3f, .15f, 5);
        structureDamage = Safe(structureDamage, 8, 0, 100);
        knockbackResistance = Safe(knockbackResistance, 0, 0, .95f);
        firstNight = Mathf.Max(1, firstNight);
        maximumActive = Mathf.Clamp(maximumActive, 1, 10);
        spawnWeight = Safe(spawnWeight, 1, 0, 100);
        burstDuration = Safe(burstDuration, .7f, .1f, 2);
        burstCooldown = Safe(burstCooldown, 4, 1, 20);
        burstMultiplier = Safe(burstMultiplier, 1.4f, 1, 2);
        lungeDistance = Safe(lungeDistance, 1.5f, .1f, 2);
        slamRadius = Safe(slamRadius, 1.6f, .1f, 2);
        animationFPS = Safe(animationFPS, 8, 1, 20);
    }

    void OnValidate() => Validate();
}
