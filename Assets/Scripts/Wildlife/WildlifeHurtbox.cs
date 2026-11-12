using UnityEngine;

/// <summary>Damage-only body shape, separate from the animal's movement footprint.</summary>
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class WildlifeHurtbox : MonoBehaviour
{
    public Collider2D Shape => GetComponent<CapsuleCollider2D>();

    public Vector2 ClosestPoint(Vector2 from) => Shape.ClosestPoint(from);
}
