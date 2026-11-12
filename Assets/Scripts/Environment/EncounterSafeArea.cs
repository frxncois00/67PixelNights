using System.Collections.Generic;
using UnityEngine;

// Local exploration refuges. These never change the campfire's radius or block player navigation.
public class EncounterSafeArea : MonoBehaviour
{
    public Vector2 size = new Vector2(8, 8);
    static readonly List<EncounterSafeArea> areas = new List<EncounterSafeArea>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() => areas.Clear();
    void OnEnable() { if (!areas.Contains(this)) areas.Add(this); }
    void OnDisable() => areas.Remove(this);

    public static bool Contains(Vector2 point, float margin = 0)
    {
        foreach (var area in areas)
        {
            if (!area || !area.isActiveAndEnabled) continue;
            Vector2 local = area.transform.InverseTransformPoint(point);
            if (Mathf.Abs(local.x) <= area.size.x * .5f + margin &&
                Mathf.Abs(local.y) <= area.size.y * .5f + margin) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.3f, .9f, .7f, .7f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, .1f));
    }
}
