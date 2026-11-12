using System.Collections.Generic;
using UnityEngine;

public abstract class InteractionTarget : MonoBehaviour {
    internal static readonly List<InteractionTarget> Active = new List<InteractionTarget>();
    public virtual float Range => 1.5f;
    public abstract string Prompt { get; }
    public abstract bool Available { get; }
    public abstract void Use(PlayerInteractionController player);
    protected virtual void OnEnable() => Active.Add(this);
    protected virtual void OnDisable() => Active.Remove(this);
    public bool Reachable(Vector2 position) => isActiveAndEnabled && Available &&
        ((Vector2)transform.position - position).sqrMagnitude <= Range * Range;
}
