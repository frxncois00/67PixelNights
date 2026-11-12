using UnityEngine;

public class WaterlineRippleAnimator : MonoBehaviour
{
    public SpriteRenderer targetRenderer;
    public float pulseSpeed = 2.4f;
    public float scaleAmplitude = 0.12f;
    public float minAlpha = 0.35f;
    public float maxAlpha = 0.72f;
    float phaseOffset;
    Vector3 baseScale = Vector3.one;

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        phaseOffset = Mathf.Abs(GetEntityId().GetHashCode() % 100) * 0.07f;
    }

    void Update()
    {
        if (targetRenderer == null)
            return;
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phaseOffset);
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed + phaseOffset) * scaleAmplitude;
        transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, 1f);
        Color c = targetRenderer.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
        targetRenderer.color = c;
    }
}
