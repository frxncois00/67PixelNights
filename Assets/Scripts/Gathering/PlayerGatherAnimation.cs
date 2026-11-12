using System;
using System.Collections;
using UnityEngine;

public class PlayerGatherAnimation : MonoBehaviour
{
    public SpriteRenderer visual;
    public Animator animator;
    public PlayerMovement movement;
    public Sprite[] chopFrames, carryFrames, miningFrames, swordFrames;
    public Sprite pickaxeSprite;
    public Sprite woodSprite, axeSprite, neutralWorker;
    [Tooltip("Unlit material for weapon trails and impact effects (glows at night).")] public Material weaponFxMaterial;
    public bool Busy { get; private set; }
    bool bodyPoseSaved; Vector3 bodyPos, bodyScale;
    // Used by WeaponSwing so interrupted attacks still clean up.
    public void TrackProp(GameObject go)
    {
        prop = go;
        if (prop) prop.hideFlags = HideFlags.DontSave;
    }
    public void SaveBodyPose() { if (bodyPoseSaved || !visual) return; bodyPos = visual.transform.localPosition; bodyScale = visual.transform.localScale; bodyPoseSaved = true; }

    bool wasMoving, wasAnimating, flip;
    Sprite saved;
    GameObject prop;

    public bool PlayEat(ItemDefinition item)
    {
        if (Busy || !isActiveAndEnabled || !item || !item.Icon) return false;
        StartCoroutine(Eat(item));
        return true;
    }

    IEnumerator Eat(ItemDefinition item)
    {
        Busy = true;
        saved = visual.sprite; flip = visual.flipX;
        wasMoving = movement.enabled; wasAnimating = animator.enabled;
        movement.enabled = false; animator.enabled = false;
        if (neutralWorker) visual.sprite = neutralWorker;
        TrackProp(new GameObject("Eating " + item.DisplayName));
        var renderer = prop.AddComponent<SpriteRenderer>();
        renderer.sprite = item.Icon; renderer.color = item.IconTint;
        renderer.sharedMaterial = visual.sharedMaterial;
        renderer.sortingLayerID = visual.sortingLayerID;
        renderer.sortingOrder = visual.sortingOrder + 1;
        try
        {
            for (float elapsed = 0; elapsed < .9f; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / .9f);
                float bite = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3));
                prop.transform.position = transform.position + new Vector3(flip ? -.22f : .22f, .38f + bite * .12f, 0);
                prop.transform.localScale = Vector3.one * Mathf.Lerp(.8f, .15f, t);
                prop.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 6) * 12);
                yield return null;
            }
        }
        finally { Restore(); }
    }

    public bool Play(bool chopping, Vector3 destination, Action complete, bool pickup = false, bool mining = false, ItemDefinition equippedTool = null)
    {
        if (Busy || !isActiveAndEnabled) return false;
        StartCoroutine(Animate(chopping, destination, complete, pickup, mining, equippedTool));
        return true;
    }

    IEnumerator Animate(bool chopping, Vector3 destination, Action complete, bool pickup, bool mining, ItemDefinition equippedTool)
    {
        Busy = true;
        saved = visual.sprite;
        flip = visual.flipX;
        wasMoving = movement.enabled;
        wasAnimating = animator.enabled;
        movement.enabled = false;
        animator.enabled = false;

        Vector2 aim = ((Vector2)(destination - (transform.position + Vector3.up * .25f))).normalized;
        if (aim.sqrMagnitude < .01f) aim = movement.Facing;
        if (Mathf.Abs(aim.x) > .01f) visual.flipX = aim.x < 0;
        // Use the same upright pose in every direction. Each tool's head must face
        // the target, rather than inheriting the side-facing sheet's baked-in swing.
        Sprite toolSprite = equippedTool ? equippedTool.Icon : mining ? pickaxeSprite : axeSprite;
        string toolId = equippedTool ? equippedTool.Id : mining ? "basic_pickaxe" : "basic_axe";
        float headAngle = ToolGripPose.HeadAngle(toolId);
        bool directional = chopping && toolSprite && neutralWorker;
        var frames = equippedTool && equippedTool.Id == "basic_sword" ? swordFrames : mining ? miningFrames : chopping ? chopFrames : carryFrames;
        // Native sheets are side-facing; vertical attacks retain the aimed grip animation.
        if (chopping && Mathf.Abs(aim.x) > .7f && frames != null && frames.Length > 0) directional = false;
        var bodyFrames = GetComponent<BodyAActionFrames>();
        if (bodyFrames) {
            frames = bodyFrames.Choose(aim, chopping, toolId == "basic_sword", mining);
            visual.flipX = Mathf.Abs(aim.x) >= Mathf.Abs(aim.y) && aim.x < 0;
            directional = false;
        }
        // Crafted weapons get their own per-weapon attack animation.
        if (chopping && equippedTool && equippedTool.craftedWeapon && equippedTool.Icon)
        {
            var sawmill = GetComponent<SawmillWeaponAnimations>();
            var authored = sawmill ? sawmill.Find(equippedTool) : null;
            if (authored != null)
            {
                try { yield return sawmill.PlayAttack(authored, aim, visual, complete); }
                finally { Restore(); }
                yield break;
            }
            if (neutralWorker) visual.sprite = neutralWorker;
            if (!bodyFrames && Mathf.Abs(aim.x) > .01f) visual.flipX = aim.x < 0;
            try { yield return WeaponSwing.Play(this, equippedTool, aim, complete); }
            finally { Restore(); }
            yield break;
        }
        if(equippedTool && equippedTool.craftedWeapon){directional=true;headAngle=90;}
        if (directional) visual.sprite = neutralWorker;
        if (directional || !chopping)
        {
            TrackProp(new GameObject(directional ? (equippedTool ? "Swinging " + equippedTool.DisplayName : mining ? "Swinging Pickaxe" : "Swinging Axe") : "Wood Transfer"));
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = directional ? toolSprite : woodSprite;
            renderer.sharedMaterial = visual.sharedMaterial;
            renderer.sortingLayerID = visual.sortingLayerID;
            renderer.sortingOrder = visual.sortingOrder + 1;
            if (directional) prop.transform.localScale = Vector3.one;
        }

        try
        {
            float duration = chopping ? .65f : .5f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                if (directional)
                {
                    float heading = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                    float angle = heading + Mathf.Lerp(-55, 0, Mathf.SmoothStep(0, 1, Mathf.Min(1, t / .6f)));
                    var hand = transform.position + new Vector3(visual.flipX ? -.25f : .25f, .48f, 0);
                    if(equippedTool && equippedTool.craftedWeapon){prop.transform.position=hand;prop.transform.rotation=Quaternion.Euler(0,0,angle-headAngle);}else ToolGripPose.Place(prop.GetComponent<SpriteRenderer>(), toolId, hand, angle - headAngle);
                }
                else
                {
                    if (frames != null && frames.Length > 0)
                        visual.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t * frames.Length))];
                    if (prop)
                    {
                        var hand = transform.position + Vector3.up * .4f;
                        prop.transform.position = Vector3.Lerp(pickup ? destination : hand, pickup ? hand : destination, t)
                            + Vector3.up * Mathf.Sin(t * Mathf.PI) * .5f;
                    }
                }
                yield return null;
            }
            complete?.Invoke();
        }
        finally { Restore(); }
    }

    void Restore()
    {
        if (!Busy) return;
        if (prop) Destroy(prop);
        if (visual) { visual.sprite = saved; visual.flipX = flip; }
        if (visual && bodyPoseSaved) { visual.transform.localPosition = bodyPos; visual.transform.localScale = bodyScale; }
        bodyPoseSaved = false;
        if (animator) animator.enabled = wasAnimating;
        if (movement) movement.enabled = wasMoving;
        Busy = false;
    }

    void OnDisable()
    {
        if (!Busy) return;
        StopAllCoroutines();
        Restore();
    }
}
