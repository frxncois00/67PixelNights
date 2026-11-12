using System;
using UnityEngine;

/// <summary>Decorative clouds drifting through configured lanes, independent of gameplay.</summary>
public sealed class OuterWaterClouds : MonoBehaviour
{
    [Serializable]
    public class Cloud
    {
        public SpriteRenderer visual;
        public Vector3 from, to;
        public float speed = .12f;
        [Range(0, 1)] public float phase, opacity = .75f;
    }

    public Cloud[] clouds = Array.Empty<Cloud>();

    void Update()
    {
        foreach (var cloud in clouds)
        {
            if (!cloud.visual) continue;
            float distance = Vector3.Distance(cloud.from, cloud.to);
            float t = (float)((cloud.phase + Time.timeSinceLevelLoadAsDouble * cloud.speed / Mathf.Max(.01f, distance)) % 1);
            cloud.visual.transform.localPosition = Vector3.Lerp(cloud.from, cloud.to, t);
            // Fade before wrapping so clouds never visibly jump across their lanes.
            float fade = Mathf.SmoothStep(0, 1, Mathf.Min(t, 1 - t) / .12f);
            Color color = cloud.visual.color;
            color.a = cloud.opacity * fade;
            cloud.visual.color = color;
        }
    }
}
