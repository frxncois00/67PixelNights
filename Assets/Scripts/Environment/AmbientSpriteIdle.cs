using UnityEngine;

/// <summary>Sprite-only idle loop. Never changes transforms, visibility, or gameplay state.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AmbientSpriteIdle : MonoBehaviour
{
    public Sprite[] frames;
    [Min(1)] public float framesPerSecond = 8;
    SpriteRenderer visual;
    Sprite original;
    double phase;
    int lastFrame = -1;

    void OnEnable()
    {
        visual = GetComponent<SpriteRenderer>();
        original = visual.sprite;
        Vector3 p = transform.position;
        phase = Mathf.Repeat(p.x * 0.173f + p.y * 0.317f, 1f);
        lastFrame = -1;
    }

    void LateUpdate()
    {
        // Harvest feedback temporarily hides the source; depletion keeps it hidden.
        if (!visual || !visual.enabled || !visual.isVisible || frames == null || frames.Length < 2)
            return;
        // Respect replacements such as a harvested tree's stump.
        if (System.Array.IndexOf(frames, visual.sprite) < 0)
            return;
        int index = (int)((Time.timeAsDouble * framesPerSecond + phase * frames.Length) % frames.Length);
        if (index == lastFrame && visual.sprite == frames[index]) return;
        lastFrame = index;
        visual.sprite = frames[index];
    }

    void OnDisable()
    {
        if (visual && frames != null && System.Array.IndexOf(frames, visual.sprite) >= 0)
            visual.sprite = original;
    }
}
