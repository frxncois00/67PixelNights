using System;
using System.Collections;
using UnityEngine;

// Pre-authored Body_A sheets contain both hands and the equipped weapon.
// Gameplay still owns input, cooldowns, damage, and projectile creation.
public class SawmillWeaponAnimations : MonoBehaviour
{
    [Serializable]
    public class DirectionFrames
    {
        public Sprite[] down, up, side;
        public Sprite[] Get(Vector2 direction) => Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? side : direction.y > 0 ? up : down;
    }

    [Serializable]
    public class WeaponFrames
    {
        public string itemId;
        public DirectionFrames idle = new DirectionFrames();
        public DirectionFrames walk = new DirectionFrames();
        public DirectionFrames attack = new DirectionFrames();
        public float duration = .6f;
        [Range(.01f, .99f)] public float impactTime = .5f;
        public int impactFrame = 3;
    }

    public WeaponFrames[] weapons;
    public WeaponFrames[] basicHeldWeapons;
    string currentItem;
    bool walking;
    float loopStarted;

    public WeaponFrames Find(ItemDefinition item)
    {
        if (!item) return null;
        var entries = item.craftedWeapon ? weapons : basicHeldWeapons;
        if (entries == null) return null;
        foreach (var entry in entries)
            if (entry != null && entry.itemId == item.Id) return entry;
        return null;
    }

    public bool ShowHeld(ItemDefinition item, Vector2 facing, bool moving, SpriteRenderer visual)
    {
        var entry = Find(item);
        if (entry == null) { currentItem = null; return false; }
        var frames = (moving ? entry.walk : entry.idle).Get(facing);
        if (frames == null || frames.Length == 0) return false;
        if (currentItem != item.Id || walking != moving)
        {
            currentItem = item.Id; walking = moving; loopStarted = Time.time;
        }
        float fps = moving ? 10f : 1f / .15f;
        visual.sprite = frames[(int)((Time.time - loopStarted) * fps) % frames.Length];
        visual.flipX = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y) && facing.x < 0;
        return true;
    }

    public IEnumerator PlayAttack(WeaponFrames entry, Vector2 facing, SpriteRenderer visual, Action impact)
    {
        var frames = entry.attack.Get(facing);
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("Missing sawmill attack frames: " + entry.itemId, this);
            yield break;
        }
        visual.flipX = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y) && facing.x < 0;
        bool hit = false;
        float duration = Mathf.Max(.01f, entry.duration);
        float impactTime = Mathf.Clamp(entry.impactTime, .01f, .99f);
        int impactFrame = Mathf.Clamp(entry.impactFrame, 1, frames.Length - 1);
        for (float elapsed = 0; ; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            int index = t < impactTime ? Mathf.FloorToInt(t / impactTime * impactFrame)
                : impactFrame + Mathf.FloorToInt((t - impactTime) / (1 - impactTime) * (frames.Length - impactFrame));
            visual.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (!hit && t >= impactTime) { hit = true; impact?.Invoke(); }
            if (t >= 1) break;
            yield return null;
        }
        currentItem = null;
    }
}
