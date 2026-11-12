using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeCombat : MonoBehaviour
{
    public EnemySpawner enemies;
    public EquipmentController equipment;
    public PlayerGatherAnimation actions;
    public PlayerMovement movement;
    [Min(0)] public float damage = 15, range = 1.5f, cooldown = .6f, knockback = 2.5f;
    [Range(1, 180)] public float arc = 100;
    float nextAttack;
    ItemDefinition attackItem;
    readonly List<EnemyHealth> hitTargets = new List<EnemyHealth>(16);
    float Damage => attackItem && attackItem.craftedWeapon ? attackItem.weaponDamage : damage;
    float Reach => attackItem && attackItem.craftedWeapon ? attackItem.weaponRange : range;

    public bool Attack()
    {
        if (!isActiveAndEnabled || !enemies || !equipment.SwordEquipped || PlayerInteractionController.InputBlocked || actions.Busy || Time.time < nextAttack) return false;
        attackItem = equipment.Equipped;
        Vector2 facing = movement.Facing;
        nextAttack = Time.time + Mathf.Max(.1f, attackItem.craftedWeapon ? attackItem.weaponCooldown : cooldown);
        bool started = actions.Play(true, transform.position + (Vector3)facing, () => Strike(facing), equippedTool: attackItem);

        return started;
    }

    void AddTarget(EnemyHealth health)
    {
        if (health && health.isActiveAndEnabled && health.IsAlive && !hitTargets.Contains(health)) hitTargets.Add(health);
    }

    void Strike(Vector2 facing)
    {
        if (PlayerInteractionController.InputBlocked || !equipment.SwordEquipped) return;
        if (attackItem && attackItem.rangedWeapon)
        {
            WoodArrow.Fire(this, facing, Damage, Reach, attackItem);
            return;
        }
        hitTargets.Clear();
        foreach (var enemy in enemies.Snapshot())
            if (enemy && enemy.isActiveAndEnabled) AddTarget(enemy.GetComponent<EnemyHealth>());
        foreach (var enemy in EnemyController.ActiveControllers)
            if (enemy && enemy.isActiveAndEnabled) AddTarget(enemy.GetComponent<EnemyHealth>());
        // Neutral wildlife shares damage/feedback but never joins hostile AI or enemy waves.
        foreach (var animal in PassiveAnimal.ActiveAnimals)
            if (animal && animal.isActiveAndEnabled) AddTarget(animal.GetComponent<EnemyHealth>());
        foreach (var health in hitTargets)
        {
            if (!health || !health.IsAlive) continue;
            Vector2 target = health.transform.position;
            var body = health.GetComponentInChildren<WildlifeHurtbox>();
            if (body && body.Shape.enabled)
                target = body.ClosestPoint(transform.position);
            Vector2 delta = target - (Vector2)transform.position;
            Vector2 aim = delta.sqrMagnitude > .0001f ? delta : (Vector2)health.transform.position - (Vector2)transform.position;
            if (delta.magnitude > Mathf.Max(.1f, Reach) || Vector2.Dot(aim.normalized, facing) < Mathf.Cos(arc * .5f * Mathf.Deg2Rad) ||
                !enemies.Visible(transform.position, target, transform, health.transform)) continue;
            health.TakeDamage(Damage, transform.position, knockback, WeaponHitProfile.ForWeapon(attackItem));
        }
    }
}
