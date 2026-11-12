using UnityEngine;

public class WoodArrow : MonoBehaviour
{
    PlayerMeleeCombat owner;
    Vector2 direction;
    float damage, left;
    WeaponHitKind hitKind;

    public static void Fire(PlayerMeleeCombat owner, Vector2 direction, float damage, float range, ItemDefinition weapon = null)
    {
        if (!weapon) weapon = owner.equipment.Equipped;
        var go = new GameObject("Wood Bow Shot");
        go.transform.position = owner.transform.position + Vector3.up * .25f;
        var arrow = go.AddComponent<WoodArrow>();
        arrow.owner = owner;
        arrow.direction = direction.normalized;
        arrow.damage = damage;
        arrow.left = range;
        // Snapshot the weapon on release; swapping equipment cannot change an in-flight hit.
        arrow.hitKind = WeaponHitProfile.ForWeapon(weapon);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = weapon.projectileSprite;
        renderer.sharedMaterial = owner.actions.visual.sharedMaterial;
        renderer.sortingLayerName = "Objects";
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void Update()
    {
        float step = Mathf.Min(left, 8 * Time.deltaTime);
        var from = (Vector2)transform.position;
        foreach (var hit in Physics2D.RaycastAll(from, direction, step))
        {
            if ((hit.collider.isTrigger && !hit.collider.GetComponent<WildlifeHurtbox>()) ||
                (owner && hit.collider.transform.IsChildOf(owner.transform))) continue;
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            // React along the arrow's travel direction even if the archer has moved away.
            if (enemy) enemy.TakeDamage(damage, from - direction, 1, hitKind);
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(direction * step);
        left -= step;
        if (left <= 0) Destroy(gameObject);
    }
}

