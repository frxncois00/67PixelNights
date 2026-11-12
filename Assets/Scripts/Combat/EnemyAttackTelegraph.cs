using UnityEngine;

// Lightweight ring telegraph; actual damage is owned by EnemyController.
public class EnemyAttackTelegraph : MonoBehaviour
{
    LineRenderer ring;
    public void Initialize(Material material)
    {
        ring = gameObject.AddComponent<LineRenderer>();
        ring.sharedMaterial = material;
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 32;
        ring.widthMultiplier = .035f;
        ring.sortingLayerName = "Foreground";
        ring.sortingOrder = 5;
        ring.enabled = false;
    }

    public void Show(float radius, Color color)
    {
        if (!ring)
            return;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2 / 32;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0));
        }

        ring.startColor = ring.endColor = color;
        ring.enabled = false; // circle outline hidden; the enemy's orange wind-up tint is the telegraph
    }

    public void Hide()
    {
        if (ring)
            ring.enabled = false;
    }
}
