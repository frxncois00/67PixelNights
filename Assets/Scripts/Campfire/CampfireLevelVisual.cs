using System;
using UnityEngine;

// Native Pixel Crawler bases, flames and smoke share the existing fuel authority.
public class CampfireLevelVisual : MonoBehaviour
{
    [Serializable] public class Level { public Sprite[] embers; public Sprite cold; public Sprite[] flames; public float flameHeight; }
    public CampfireController fire;
    public CampfireUpgrade upgrade;
    public SpriteRenderer baseVisual, effectVisual;
    public Level[] levels;
    public Sprite[] smoke;
    public float framesPerSecond = 8;
    float elapsed;
    int lastLevel = -1;
    bool wasBurning;
    void LateUpdate() => Refresh(Time.deltaTime);
    public void Refresh(float delta)
    {
        if (!fire || !upgrade || !baseVisual || !effectVisual || levels == null || levels.Length == 0) return;
        int index = Mathf.Clamp(upgrade.CurrentLevel - 1, 0, levels.Length - 1);
        if (index != lastLevel || wasBurning != fire.IsBurning) elapsed = 0;
        lastLevel = index; wasBurning = fire.IsBurning;
        elapsed += Mathf.Max(0, delta);
        int frame = (int)(elapsed * Mathf.Max(1, framesPerSecond));
        var level = levels[index];
        baseVisual.enabled = true; baseVisual.color = Color.white;
        baseVisual.sprite = fire.IsBurning && level.embers.Length > 0 ? level.embers[frame % level.embers.Length] : level.cold;
        var effects = fire.IsBurning ? level.flames : smoke;
        effectVisual.enabled = effects != null && effects.Length > 0;
        if (effectVisual.enabled) effectVisual.sprite = effects[frame % effects.Length];
        effectVisual.color = fire.IsBurning ? Color.white : new Color(1,1,1,.45f);
        effectVisual.transform.localPosition = Vector3.up * level.flameHeight;
    }
}
