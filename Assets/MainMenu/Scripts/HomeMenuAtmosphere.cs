using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeMenuAtmosphere : MonoBehaviour
{
    [Serializable] public class Spark { public RectTransform rect; public Image image; public Vector2 origin; public float phase, duration, rise; }
    [Serializable] public class Mist { public RectTransform rect; public Image image; public Vector2 origin; public float phase, speed, distance, alpha; }
    public Spark[] sparks = Array.Empty<Spark>();
    public Mist[] mists = Array.Empty<Mist>();
    void Update()
    {
        double time = Time.unscaledTimeAsDouble;
        foreach (var s in sparks)
        {
            float t = (float)((time / s.duration + s.phase) % 1);
            s.rect.anchoredPosition = s.origin + new Vector2(Mathf.Sin(t * 5 + s.phase * 20) * 9, t * s.rise);
            Color c = s.image.color; c.a = Mathf.Sin(t * Mathf.PI) * .75f; s.image.color = c;
        }
        foreach (var m in mists)
        {
            float t = (float)((time * m.speed + m.phase) % 1);
            m.rect.anchoredPosition = m.origin + Vector2.right * ((t - .5f) * m.distance);
            Color c = m.image.color; c.a = Mathf.Sin(t * Mathf.PI) * m.alpha; m.image.color = c;
        }
    }
}
