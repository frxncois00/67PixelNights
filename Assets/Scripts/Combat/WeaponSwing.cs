using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Procedural, per-weapon attack animations for crafted weapons.
// Each weapon has its own motion (slash, stab, slam, thrust, scoop, bow draw),
// is scaled to the character, leaves a motion trail and plays a small impact
// effect. The hit callback fires on the impact frame instead of at the end.
public static class WeaponSwing
{
    enum Style { Slash, Stab, Slam, Thrust, Scoop, Draw }

    struct Profile
    {
        public Style style;
        public float length, duration, impact;
        public Profile(Style s, float l, float d, float i) { style = s; length = l; duration = d; impact = i; }
    }

    // length = world length of the weapon; duration stays below each weapon's cooldown.
    static Profile For(ItemDefinition item)
    {
        switch (item.Id)
        {
            case "wood_knife": return new Profile(Style.Stab, .4f, .32f, .46f);
            case "wood_hammer": return new Profile(Style.Slam, .66f, .72f, .64f);
            case "wood_spear": return new Profile(Style.Thrust, .92f, .52f, .52f);
            case "wood_shovel": return new Profile(Style.Scoop, .58f, .5f, .5f);
            case "wood_bow": return new Profile(Style.Draw, .55f, .56f, .7f);
            case "wood_longsword": return new Profile(Style.Slash, .72f, .46f, .54f);
        }
        return item.rangedWeapon ? new Profile(Style.Draw, .55f, .56f, .7f) : new Profile(Style.Slash, .66f, .46f, .54f);
    }

    // Shared with the idle held sprite so imported icon dimensions cannot make it oversized.
    public static float WeaponLength(ItemDefinition item) => For(item).length;

    static float Seg(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));
    static float OutCubic(float x) { x = 1 - x; return 1 - x * x * x; }
    static float InCubic(float x) => x * x * x;
    static float OutBack(float x) { const float c = 1.70158f; x -= 1; return 1 + (c + 1) * x * x * x + c * x * x; }

    // Attack rigs and particles exist only during play, never in the saved scene.
    static GameObject Temporary(string name) => new GameObject(name) { hideFlags = HideFlags.DontSave };

    public static IEnumerator Play(PlayerGatherAnimation owner, ItemDefinition item, Vector2 aim, Action impact)
    {
        var p = For(item);
        var visual = owner.visual;
        Transform body = visual.transform;
        owner.SaveBodyPose();
        Vector3 bodyPos = body.localPosition, bodyScale = body.localScale;

        if (aim.sqrMagnitude < .01f) aim = Vector2.down;
        aim.Normalize();
        float heading = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float side = visual.flipX ? -1 : 1; // swing direction mirrors with facing
        bool behind = aim.y > .6f;          // weapon goes behind the body when facing up
        int order = visual.sortingOrder + (behind ? -2 : 2);
        Material fx = FxMaterial(owner);

        // Rig: root sits at the hand and rotates; the art child is scaled to the character.
        var root = Temporary("Attacking " + item.DisplayName).transform;
        owner.TrackProp(root.gameObject);
        var art = Temporary("Weapon").transform;
        art.SetParent(root, false);
        var sr = art.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = item.Icon;
        sr.color = item.IconTint;
        sr.sharedMaterial = visual.sharedMaterial;
        sr.sortingLayerID = visual.sortingLayerID;
        sr.sortingOrder = order;
        Bounds b = item.Icon.bounds; // icons point up, handle at the pivot
        float scale = p.length / Mathf.Max(.01f, b.size.y);
        art.localScale = Vector3.one * scale;
        if (p.style == Style.Draw) art.localPosition = new Vector3(-b.center.x * scale, -b.center.y * scale, 0); // hold the bow by its middle

        TrailRenderer trail = null;
        Transform tip = Temporary("Tip").transform;
        tip.SetParent(root, false);
        tip.localPosition = new Vector3(0, b.max.y * scale * .9f, 0);
        if (p.style != Style.Draw)
        {
            trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = fx;
            trail.time = p.style == Style.Slash || p.style == Style.Slam ? .14f : .1f;
            trail.minVertexDistance = .01f;
            trail.widthMultiplier = p.style == Style.Slash ? .26f : p.style == Style.Slam ? .22f : .12f;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .85f, .55f), 1) },
                      new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = g;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingLayerID = visual.sortingLayerID;
            trail.sortingOrder = order - 1;
            trail.emitting = false;
        }

        // Bow: a nocked arrow that is drawn back and released.
        SpriteRenderer arrow = null;
        if (p.style == Style.Draw && item.projectileSprite)
        {
            arrow = Temporary("Nocked Arrow").AddComponent<SpriteRenderer>();
            arrow.transform.SetParent(root, true);
            arrow.sprite = item.projectileSprite;
            arrow.sharedMaterial = visual.sharedMaterial;
            arrow.sortingLayerID = visual.sortingLayerID;
            arrow.sortingOrder = order + 1;
        }

        bool hit = false;
        float squash = 0;
        try
        {
            for (float elapsed = 0; ; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / p.duration);
                float a = 0, reach = 0, lunge = 0, punch = 1;
                bool emit = false;
                Vector3 hand = owner.transform.position + new Vector3(side * .14f, .36f, 0);

                switch (p.style)
                {
                    case Style.Slash: // wind up high behind, fast wide arc through the target
                        if (t < .38f) a = Mathf.Lerp(0, 125, OutCubic(Seg(t, 0, .38f)));
                        else if (t < .6f) { float k = OutCubic(Seg(t, .38f, .6f)); a = Mathf.Lerp(125, -85, k); emit = true; lunge = Mathf.Sin(k * Mathf.PI) * .08f; punch = 1.12f; }
                        else a = Mathf.Lerp(-85, -60, OutCubic(Seg(t, .6f, 1)));
                        reach = t < .38f ? -.04f * Seg(t, 0, .38f) : 0;
                        break;

                    case Style.Stab: // spin-flip the knife in hand, then a quick jab
                        if (t < .32f) { float k = OutCubic(Seg(t, 0, .32f)); a = 25 - 360 * (1 - k); reach = -.12f * k; }
                        else if (t < .55f) { float k = Seg(t, .32f, .55f); a = Mathf.Lerp(25, 0, k); reach = Mathf.LerpUnclamped(-.12f, .3f, OutBack(k)); emit = true; lunge = .06f * Mathf.Sin(k * Mathf.PI); }
                        else reach = Mathf.Lerp(.3f, 0, OutCubic(Seg(t, .55f, 1)));
                        break;

                    case Style.Thrust: // coil back, lunge forward, hold, retract
                        if (t < .38f) { float k = OutCubic(Seg(t, 0, .38f)); reach = -.22f * k; a = 12 * k; }
                        else if (t < .54f) { float k = OutCubic(Seg(t, .38f, .54f)); reach = Mathf.Lerp(-.22f, .42f, k); a = Mathf.Lerp(12, 0, k); emit = true; lunge = .12f * k; }
                        else if (t < .72f) { reach = .42f; lunge = .12f; }
                        else { float k = OutCubic(Seg(t, .72f, 1)); reach = Mathf.Lerp(.42f, 0, k); lunge = Mathf.Lerp(.12f, 0, k); }
                        break;

                    case Style.Slam: // heave overhead with a tremble, smash down, bounce
                        if (t < .52f) { float k = Seg(t, 0, .52f); a = Mathf.Lerp(0, 165, OutCubic(k)) + Mathf.Sin(elapsed * 70) * 3 * k * k; lunge = -.05f * k; }
                        else if (t < .64f) { a = Mathf.Lerp(165, -35, InCubic(Seg(t, .52f, .64f))); emit = true; lunge = .08f; punch = 1.1f; }
                        else if (t < .74f) a = Mathf.LerpUnclamped(-35, -18, OutBack(Seg(t, .64f, .74f)));
                        else a = Mathf.Lerp(-18, -8, Seg(t, .74f, 1));
                        break;

                    case Style.Scoop: // dig the blade low, then flick it up through the target
                        if (t < .34f) { float k = OutCubic(Seg(t, 0, .34f)); a = -60 * k; reach = .04f * k; lunge = -.03f * k; }
                        else if (t < .54f) { float k = OutCubic(Seg(t, .34f, .54f)); a = Mathf.Lerp(-60, 70, k); emit = true; lunge = .06f * Mathf.Sin(k * Mathf.PI); }
                        else a = Mathf.Lerp(70, 25, OutCubic(Seg(t, .54f, 1)));
                        break;

                    case Style.Draw: // draw the string back, tremble at full draw, release with recoil
                        {
                            float pull = .17f * OutCubic(Seg(t, 0, p.impact * .85f));
                            float tremble = t > p.impact * .85f && t < p.impact ? Mathf.Sin(elapsed * 90) * 1.5f : 0;
                            float recoil = t >= p.impact ? Mathf.Sin(Seg(t, p.impact, 1) * Mathf.PI) * (1 - Seg(t, p.impact, 1)) : 0;
                            Vector3 bowPos = hand + (Vector3)aim * (.16f + .07f * recoil - (t < p.impact ? pull * .25f : 0));
                            root.position = bowPos;
                            root.rotation = Quaternion.Euler(0, 0, heading + tremble + side * 10 * recoil);
                            float bend = t < p.impact ? pull : 0;
                            art.localScale = new Vector3(scale * (1 + bend * 1.6f), scale * (1 - bend * .5f), 1);
                            art.localPosition = new Vector3(-b.center.x * art.localScale.x, -b.center.y * art.localScale.y, 0);
                            if (arrow)
                            {
                                arrow.enabled = t < p.impact;
                                arrow.transform.position = bowPos + (Vector3)aim * (.1f - pull);
                                arrow.transform.rotation = Quaternion.Euler(0, 0, heading);
                            }
                            lunge = -.05f * recoil;
                            break;
                        }
                }

                if (p.style != Style.Draw)
                {
                    float angle = heading + side * a;
                    root.position = hand + (Vector3)aim * reach;
                    root.rotation = Quaternion.Euler(0, 0, angle - 90);
                    art.localScale = Vector3.one * scale * punch;
                }
                if (trail) trail.emitting = emit;

                if (!hit && t >= p.impact)
                {
                    hit = true;
                    Impact(p.style, tip.position, aim, visual, order, fx);
                    if (p.style == Style.Slam) squash = 1;
                    impact?.Invoke();
                }

                // Body: lunge toward the target and squash on heavy impacts.
                squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 6);
                body.localPosition = bodyPos + (Vector3)(aim * lunge);
                body.localScale = new Vector3(bodyScale.x * (1 + .14f * squash), bodyScale.y * (1 - .14f * squash), bodyScale.z);

                if (t >= 1) break;
                yield return null;
            }
        }
        finally
        {
            if (body) { body.localPosition = bodyPos; body.localScale = bodyScale; }
        }
    }

    static void Impact(Style style, Vector3 tip, Vector2 aim, SpriteRenderer visual, int order, Material fx)
    {
        int layer = visual.sortingLayerID;
        switch (style)
        {
            case Style.Slam:
                Burst(tip, new Color(.78f, .66f, .48f), 12, 2.2f, 0, Vector2.zero, layer, order + 1, fx, .45f, 1.6f);
                Burst(tip, Color.white, 6, 3.2f, 0, Vector2.zero, layer, order + 1, fx, .18f, 1.1f);
                break;
            case Style.Scoop:
                Burst(tip, new Color(.55f, .4f, .25f), 9, 2.4f, 7, aim * 1.2f + Vector2.up * 1.6f, layer, order + 1, fx, .5f, 1.4f);
                break;
            case Style.Draw:
                Burst(tip, Color.white, 5, 3.5f, 0, aim * 2.5f, layer, order + 1, fx, .15f, 1f);
                break;
            case Style.Thrust:
            case Style.Stab:
                Burst(tip, Color.white, 6, 2.8f, 0, aim * 1.5f, layer, order + 1, fx, .16f, 1f);
                break;
            default:
                Burst(tip, Color.white, 7, 3f, 0, aim * 1.2f, layer, order + 1, fx, .18f, 1f);
                break;
        }
    }

    // ---------- Effects ----------

    static Material fallbackMaterial;
    static Material FxMaterial(PlayerGatherAnimation owner)
    {
        if (owner.weaponFxMaterial) return owner.weaponFxMaterial;
        if (!fallbackMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            fallbackMaterial = shader ? new Material(shader) : owner.visual.sharedMaterial;
        }
        return fallbackMaterial;
    }

    static Sprite pixel;
    static Sprite Pixel
    {
        get
        {
            if (pixel) return pixel;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 16);
            return pixel;
        }
    }

    static void Burst(Vector3 at, Color color, int count, float speed, float gravity, Vector2 bias,
                      int layer, int order, Material mat, float life, float size)
    {
        for (int i = 0; i < count; i++)
        {
            var go = Temporary("Weapon FX");
            go.transform.position = at;
            go.transform.localScale = Vector3.one * size * UnityEngine.Random.Range(.7f, 1.2f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Pixel;
            r.color = color;
            r.sharedMaterial = mat;
            r.sortingLayerID = layer;
            r.sortingOrder = order;
            var fp = go.AddComponent<WeaponFxParticle>();
            float ang = (i / (float)count) * Mathf.PI * 2 + UnityEngine.Random.Range(-.3f, .3f);
            fp.velocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed * UnityEngine.Random.Range(.6f, 1f) + bias;
            fp.gravity = gravity;
            fp.life = life * UnityEngine.Random.Range(.8f, 1.2f);
        }
    }

}
