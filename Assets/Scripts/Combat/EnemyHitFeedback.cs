using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Weapon-specific contact bursts, additive mob recoil, chipped health bars and damage pops.
/// Only the enemy visual is deformed: no camera movement, global hit stop or player-pose changes.
/// </summary>
[DefaultExecutionOrder(50)]
public class EnemyHitFeedback : MonoBehaviour
{
    const float PixelsPerUnit = 32f;
    const float FlashDuration = 0.105f;

    // Canvas-space dimensions (at scale 1/64, 2 canvas units = 1 game pixel at 32 PPU).
    const float CanvasScale = 1f / 64f;
    const float BarInnerWidth = 48f;   // 24 game pixels (0.75 world units)
    const float BarInnerHeight = 6f;   // 3 game pixels (0.09375 world units)

    static Material flashMaterial;
    static Material unlitSpriteMaterial;
    static Sprite pixelSprite, starSprite, slashSprite;

    EnemyController controller;
    EnemyHealth health;
    SpriteRenderer visual;
    Transform visualTransform;
    Vector3 baseScale, baseLocalPos;
    Quaternion baseLocalRot;
    bool hasVisualChild;
    bool poseApplied;
    Vector3 appliedPosition, appliedScale;
    Quaternion appliedRotation;
    WeaponHitProfile hitProfile = WeaponHitProfile.For(WeaponHitKind.Slash);
    float hitStrength = 1f;
    int hitSequence;
    float lastNumberAt = -10f;
    int numberLane;

    SpriteRenderer flashOverlay;
    float flashStarted = -10f;
    float squashStarted = -10f;
    Vector2 hitDirection = Vector2.right;
    float recoilTiltSign = 1f;

    Transform barRoot;
    CanvasGroup barCanvasGroup;
    RectTransform barChipRect, barFillRect;
    Image barChipImage, barFillImage;
    Image barImpactImage;
    RectTransform barImpactRect;
    float displayedFraction = 1f, chipFraction = 1f, chipHoldUntil, barVisibleUntil, barAlpha;
    float barHeightOffset = 0.78f;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        health = GetComponent<EnemyHealth>();
        visual = controller && controller.visual ? controller.visual : GetComponentInChildren<SpriteRenderer>();
        EnsureSharedAssets();

        if (visual)
        {
            visualTransform = visual.transform;
            hasVisualChild = visualTransform != transform;
            baseScale = visualTransform.localScale;
            baseLocalPos = visualTransform.localPosition;
            baseLocalRot = visualTransform.localRotation;
            barHeightOffset = ComputeHeadHeightOffset(visual);
            BuildFlashOverlay();
        }

        BuildHealthBar();
    }

    void OnEnable()
    {
        if (!health) health = GetComponent<EnemyHealth>();
        if (health)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            displayedFraction = chipFraction = health.MaxHealth > 0f ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) : 1f;
        }
    }

    void OnDisable()
    {
        if (health)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
        RestoreVisualPose();
        if (flashOverlay) flashOverlay.enabled = false;
        if (barCanvasGroup) barCanvasGroup.alpha = 0f;
        flashStarted = squashStarted = -10f;
        barAlpha = barVisibleUntil = 0f;
    }

    void Update() => RestoreVisualPose();

    void RestoreVisualPose()
    {
        if (!poseApplied || !visualTransform) return;
        // Remove only our previous pose. A creature/Animator may have supplied a new one.
        if (visualTransform.localPosition == appliedPosition) visualTransform.localPosition = baseLocalPos;
        if (visualTransform.localScale == appliedScale) visualTransform.localScale = baseScale;
        if (visualTransform.localRotation == appliedRotation) visualTransform.localRotation = baseLocalRot;
        poseApplied = false;
    }

    float ComputeHeadHeightOffset(SpriteRenderer sr)
    {
        if (!sr || !sr.sprite) return 0.78f;
        Sprite s = sr.sprite;
        Vector2[] verts = s.vertices;
        float maxY = 0f;
        if (verts != null && verts.Length > 0)
        {
            for (int i = 0; i < verts.Length; i++)
                if (verts[i].y > maxY) maxY = verts[i].y;
        }
        else
        {
            maxY = s.bounds.max.y;
        }

        float tightTop = transform.InverseTransformPoint(sr.transform.TransformPoint(new Vector3(0, maxY, 0))).y;
        return Mathf.Clamp(tightTop + 0.15f, 0.56f, 2.8f);
    }

    // ---------- Shared procedural assets ----------
    static void EnsureSharedAssets()
    {
        if (!flashMaterial)
        {
            var shader = Shader.Find("Hidden/EnemyHitFlash")
                      ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                      ?? Shader.Find("Sprites/Default");
            flashMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        if (!unlitSpriteMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                      ?? Shader.Find("Sprites/Default");
            unlitSpriteMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        if (!pixelSprite)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            pixelSprite.hideFlags = HideFlags.DontSave;
        }

        if (!starSprite)
        {
            // Eight crisp rays with a small white core, like the reference's contact burst.
            const int n = 21;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var clear = new Color(1, 1, 1, 0);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int dx = Mathf.Abs(x - 10), dy = Mathf.Abs(y - 10);
                    int longest = Mathf.Max(dx, dy), shortest = Mathf.Min(dx, dy);
                    bool core = dx + dy <= 5;
                    bool rays = shortest <= (longest < 6 ? 1 : 0);
                    bool diag = Mathf.Abs(dx - dy) <= (longest < 5 ? 1 : 0) && longest <= 7;
                    if (core || rays || diag)
                    {
                        Color c = dx + dy < 6 ? new Color(1f, 1f, .94f) :
                            longest < 7 ? new Color(1f, .91f, .36f) : new Color(1f, .57f, .09f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            starSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            starSprite.hideFlags = HideFlags.DontSave;
        }

        if (!slashSprite)
        {
            // 20x20 procedural crescent slash arc facing +X.
            const int n = 20;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var clear = new Color(1, 1, 1, 0);
            Vector2 arcCenter = new Vector2(3.5f, 9.5f);
            const float radius = 10.2f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2(x, y) - arcCenter;
                    float dist = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    float normAngle = Mathf.Abs(angle) / 68f;
                    if (normAngle <= 1f)
                    {
                        float halfThick = Mathf.Lerp(2.1f, 0.35f, normAngle * normAngle);
                        float radialDiff = Mathf.Abs(dist - radius);
                        if (radialDiff <= halfThick)
                        {
                            float alpha = Mathf.Clamp01(1f - (radialDiff / (halfThick + 0.4f))) * Mathf.Lerp(1f, 0.55f, normAngle);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                            continue;
                        }
                    }
                    tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            slashSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.55f, 0.5f), PixelsPerUnit);
            slashSprite.hideFlags = HideFlags.DontSave;
        }
    }

    void BuildFlashOverlay()
    {
        var go = new GameObject("HitFlashOverlay") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(visualTransform, false);
        flashOverlay = go.AddComponent<SpriteRenderer>();
        flashOverlay.sharedMaterial = flashMaterial;
        flashOverlay.sortingLayerID = visual.sortingLayerID;
        flashOverlay.sortingOrder = visual.sortingOrder + 1;
        flashOverlay.spriteSortPoint = visual.spriteSortPoint;
        flashOverlay.enabled = false;
    }

    static Image MakeUIRect(Transform parent, string name, Color color, Vector2 anchoredPos, Vector2 size, Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = pixelSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    void BuildHealthBar()
    {
        var root = new GameObject("EnemyHealthBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup))
        {
            hideFlags = HideFlags.DontSave
        };
        barRoot = root.transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = new Vector3(0f, barHeightOffset, 0f);
        barRoot.localScale = Vector3.one * CanvasScale;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "Foreground";
        canvas.sortingOrder = 65;

        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(BarInnerWidth + 6f, BarInnerHeight + 6f);

        barCanvasGroup = root.GetComponent<CanvasGroup>();
        barCanvasGroup.interactable = false;
        barCanvasGroup.blocksRaycasts = false;
        barCanvasGroup.alpha = 0f;

        Vector2 centerAnchor = new Vector2(0.5f, 0.5f);
        Vector2 leftAnchor = new Vector2(0f, 0.5f);

        // 1. Subtle drop shadow for ground contrast on bright grass or water.
        MakeUIRect(barRoot, "Shadow", new Color(0f, 0f, 0f, 0.42f),
            new Vector2(0f, -1.5f), new Vector2(BarInnerWidth + 6f, BarInnerHeight + 4f),
            centerAnchor, centerAnchor, centerAnchor);

        // 2. Crisp 1px dark obsidian pixel border.
        MakeUIRect(barRoot, "Border", new Color(0.07f, 0.06f, 0.09f, 0.96f),
            Vector2.zero, new Vector2(BarInnerWidth + 6f, BarInnerHeight + 6f),
            centerAnchor, centerAnchor, centerAnchor);

        MakeUIRect(barRoot, "Frame", new Color(.68f, .73f, .73f, 1f),
            Vector2.zero, new Vector2(BarInnerWidth + 2f, BarInnerHeight + 2f),
            centerAnchor, centerAnchor, centerAnchor);

        // 3. Recessed dark crimson-charcoal track.
        var track = MakeUIRect(barRoot, "Track", new Color(0.07f, 0.10f, 0.12f, 1f),
            Vector2.zero, new Vector2(BarInnerWidth, BarInnerHeight),
            centerAnchor, centerAnchor, centerAnchor);
        Transform trackTransform = track.transform;

        // 4. Delayed golden-ivory "damage chip" bar showing recently lost HP.
        barChipImage = MakeUIRect(trackTransform, "DamageChip", new Color(1f, 0.76f, 0.22f, 1f),
            Vector2.zero, new Vector2(BarInnerWidth, BarInnerHeight),
            leftAnchor, leftAnchor, leftAnchor);
        barChipRect = barChipImage.rectTransform;

        // 5. Active HP fill bar.
        barFillImage = MakeUIRect(trackTransform, "Fill", new Color(0.24f, 0.86f, 0.36f, 1f),
            Vector2.zero, new Vector2(BarInnerWidth, BarInnerHeight),
            leftAnchor, leftAnchor, leftAnchor);
        barFillRect = barFillImage.rectTransform;

        // 6. 1px top specular highlight & 1px bottom bevel inside the fill bar for pixel-art depth.
        MakeUIRect(barFillRect, "TopHighlight", new Color(1f, 1f, 1f, 0.34f),
            Vector2.zero, new Vector2(0f, 2f),
            new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        MakeUIRect(barFillRect, "BottomShade", new Color(0f, 0f, 0f, 0.24f),
            Vector2.zero, new Vector2(0f, 1.5f),
            new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));

        // 7. Subtle 25% / 50% / 75% segment notch ticks along the top edge.
        for (int i = 1; i <= 3; i++)
        {
            float x = BarInnerWidth * (i * 0.25f);
            MakeUIRect(trackTransform, "Tick" + i, new Color(0.07f, 0.06f, 0.09f, 0.5f),
                new Vector2(x, 0f), new Vector2(1.2f, BarInnerHeight),
                new Vector2(0.5f, 0.5f), leftAnchor, leftAnchor);
        }

        barImpactImage = MakeUIRect(trackTransform, "HitGleam", new Color(1f, .97f, .7f, 0f),
            Vector2.zero, new Vector2(3f, BarInnerHeight + 4f), centerAnchor, leftAnchor, leftAnchor);
        barImpactRect = barImpactImage.rectTransform;

        ApplyBarFractions();
    }

    // ---------- Combat Events ----------
    void OnDamaged(float amount, Vector2 source)
    {
        RestoreVisualPose();
        Vector2 away = (Vector2)transform.position - source;
        hitDirection = away.sqrMagnitude > .0001f ? away.normalized : Vector2.right;
        recoilTiltSign = Mathf.Abs(hitDirection.x) > .05f ? -Mathf.Sign(hitDirection.x) : (hitSequence % 2 == 0 ? 1f : -1f);
        hitSequence++;
        hitProfile = WeaponHitProfile.For(health ? health.LastHitKind : WeaponHitKind.Slash);
        bool heavyHit = hitProfile.kind == WeaponHitKind.Slam || amount >= (health ? health.MaxHealth : 30f) * .3f;
        hitStrength = heavyHit ? 1.15f : 1f;
        flashStarted = squashStarted = Time.time;
        chipHoldUntil = Time.time + .18f;
        barVisibleUntil = Time.time + 3.2f;
        barAlpha = 1f;

        // Work from the rendered body, not the health bar: tall mobs still get a body-level hit.
        Vector2 center = visual ? (Vector2)visual.bounds.center : (Vector2)transform.position + Vector2.up * .35f;
        float edge = visual ? Mathf.Min(.22f, visual.bounds.extents.x * .6f) : .14f;
        Vector2 impactPoint = center - hitDirection * edge;
        SpawnWeaponImpact(impactPoint);

        numberLane = Time.time - lastNumberAt < .48f ? (numberLane + 1) % 3 : 0;
        lastNumberAt = Time.time;
        float lane = numberLane == 0 ? 0f : numberLane == 1 ? -.23f : .23f;
        Vector2 numberOrigin = (Vector2)transform.position + new Vector2(lane + hitDirection.x * .12f,
            barHeightOffset + .32f + numberLane * .06f);
        DamageNumber.Show(numberOrigin, amount, heavyHit, hitDirection.x);
    }

    void OnDied()
    {
        // The killing hit already emitted its weapon burst; add only a small finishing scatter.
        Vector2 center = visual ? (Vector2)visual.bounds.center : (Vector2)transform.position;
        SpawnSparks(center, hitDirection, 6, .8f, hitProfile.accent, true);
        SpawnDust((Vector2)transform.position, 6, 1f);
        chipHoldUntil = Time.time;
        barVisibleUntil = Time.time + 0.18f;
    }

    // ---------- Per-frame Animation ----------
    void LateUpdate()
    {
        AnimateVisual();
        AnimateHealthBar();
    }

    void AnimateVisual()
    {
        if (!visual) return;

        // A two-frame contact flash fades rapidly so the mob's artwork remains readable.
        if (flashOverlay)
        {
            float ft = (Time.time - flashStarted) / FlashDuration;
            bool flashing = ft >= 0f && ft < 1f && visual.enabled;
            flashOverlay.enabled = flashing;
            if (flashing)
            {
                flashOverlay.sprite = visual.sprite;
                flashOverlay.flipX = visual.flipX;
                flashOverlay.flipY = visual.flipY;
                flashOverlay.sortingLayerID = visual.sortingLayerID;
                flashOverlay.sortingOrder = visual.sortingOrder + 1;
                Color c = Color.Lerp(Color.white, hitProfile.accent, ft * .35f);
                c.a = .72f * (1f - ft) * visual.color.a;
                flashOverlay.color = c;
            }
        }

        if (!hasVisualChild) return;
        float t = (Time.time - squashStarted) / hitProfile.duration;
        if (t < 0f || t >= 1f) return;

        // Capture the pose AFTER the creature's own LateUpdate (eel surfacing, etc.).
        baseLocalPos = visualTransform.localPosition;
        baseScale = visualTransform.localScale;
        baseLocalRot = visualTransform.localRotation;
        float impulse = t < .14f ? Mathf.Lerp(.7f, 1f, t / .14f) :
            t < .27f ? 1f : Mathf.Pow(1f - (t - .27f) / .73f, 2f);
        float rebound = t > .42f ? Mathf.Sin((t - .42f) / .58f * Mathf.PI) * .22f : 0f;
        float squash = hitProfile.compression * hitStrength * (impulse - rebound);
        bool horizontal = Mathf.Abs(hitDirection.x) >= Mathf.Abs(hitDirection.y);
        bool flatten = hitProfile.kind == WeaponHitKind.Slam || hitProfile.kind == WeaponHitKind.Scoop || !horizontal;
        float sx = flatten ? 1f + squash : 1f - squash;
        float sy = flatten ? 1f - squash : 1f + squash * .7f;
        appliedScale = Vector3.Scale(baseScale, new Vector3(sx, sy, 1f));

        Vector3 offset = (Vector3)(hitDirection * (hitProfile.recoil * impulse * hitStrength));
        if (visualTransform.parent) offset = visualTransform.parent.InverseTransformVector(offset);
        if (hitProfile.kind == WeaponHitKind.Scoop) offset.y += Mathf.Sin(t * Mathf.PI) * .10f;
        // Keep a bottom-anchored foot still even when the imported sprite uses a centre pivot.
        if (visual.sprite) offset.y += visual.sprite.bounds.min.y * (baseScale.y - appliedScale.y);
        offset.x = Mathf.Round(offset.x * PixelsPerUnit) / PixelsPerUnit;
        offset.y = Mathf.Round(offset.y * PixelsPerUnit) / PixelsPerUnit;
        appliedPosition = baseLocalPos + offset;
        appliedRotation = baseLocalRot * Quaternion.Euler(0f, 0f, hitProfile.tilt * recoilTiltSign * impulse);
        visualTransform.localPosition = appliedPosition;
        visualTransform.localScale = appliedScale;
        visualTransform.localRotation = appliedRotation;
        poseApplied = true;
    }

    void AnimateHealthBar()
    {
        if (!barRoot || !health) return;

        float target = health.MaxHealth > 0f ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) : 0f;
        displayedFraction = Mathf.MoveTowards(displayedFraction, target, Time.deltaTime * 9f);

        if (Time.time >= chipHoldUntil)
            chipFraction = Mathf.MoveTowards(chipFraction, displayedFraction, Time.deltaTime * 1.65f);
        if (chipFraction < displayedFraction)
            chipFraction = displayedFraction;

        bool show = Time.time < barVisibleUntil && target < .999f && (health.IsAlive || Time.time - squashStarted < .18f);
        barAlpha = Mathf.MoveTowards(barAlpha, show ? 1f : 0f, Time.deltaTime * (show ? 12f : 3.2f));

        if (barCanvasGroup)
            barCanvasGroup.alpha = barAlpha;

        if (barAlpha > 0.005f)
        {
            ApplyBarFractions();
            float pop = Mathf.Clamp01(1f - (Time.time - squashStarted) / 0.16f);
            float scale = CanvasScale * (1f + 0.12f * pop);
            barRoot.localScale = new Vector3(scale, scale, 1f);
            barRoot.localPosition = new Vector3(0f, barHeightOffset, 0f);
            float gleam = Mathf.Clamp01(1f - (Time.time - squashStarted) / .20f);
            barImpactRect.anchoredPosition = new Vector2(BarInnerWidth * displayedFraction, 0f);
            barImpactImage.color = new Color(1f, .97f, .73f, gleam);
            barChipImage.color = Color.Lerp(new Color(1f, .57f, .13f), new Color(1f, .94f, .63f), gleam);
        }
    }

    void ApplyBarFractions()
    {
        if (!barFillRect || !barChipRect) return;

        float fillW = Mathf.Max(0f, BarInnerWidth * displayedFraction);
        float chipW = Mathf.Max(fillW, BarInnerWidth * chipFraction);
        barFillRect.sizeDelta = new Vector2(fillW, BarInnerHeight);
        barChipRect.sizeDelta = new Vector2(chipW, BarInnerHeight);

        // Smooth RPG health gradient: emerald green -> golden amber -> crimson red.
        Color full = new Color(0.24f, 0.86f, 0.36f, 1f);
        Color mid = new Color(0.96f, 0.74f, 0.18f, 1f);
        Color low = new Color(0.92f, 0.22f, 0.22f, 1f);
        barFillImage.color = displayedFraction > 0.5f
            ? Color.Lerp(mid, full, (displayedFraction - 0.5f) * 2f)
            : Color.Lerp(low, mid, displayedFraction * 2f);
    }

    // ---------- Procedural Combat VFX ----------
    void SpawnWeaponImpact(Vector2 point)
    {
        float size = hitProfile.burstSize * hitStrength;
        float sweep = hitSequence % 2 == 0 ? -1f : 1f;
        SpawnImpactStar(point, size);
        switch (hitProfile.kind)
        {
            case WeaponHitKind.Slash:
                SpawnSlashArc(point, hitDirection, .95f, sweep * 22f);
                break;
            case WeaponHitKind.Cleave:
                SpawnSlashArc(point, hitDirection, 1.40f, sweep * 32f);
                SpawnStreak(point, new Vector2(-hitDirection.y, hitDirection.x), .45f, hitProfile.accent);
                break;
            case WeaponHitKind.Stab:
                SpawnStreak(point, hitDirection, .32f, hitProfile.accent);
                break;
            case WeaponHitKind.Thrust:
                SpawnStreak(point, hitDirection, .72f, hitProfile.accent);
                break;
            case WeaponHitKind.Arrow:
                SpawnStreak(point, hitDirection, .22f, hitProfile.accent);
                break;
            case WeaponHitKind.Slam:
                SpawnStreak(point, Vector2.down, .4f, hitProfile.accent);
                SpawnDust(transform.position, 10, 1.2f);
                break;
            case WeaponHitKind.Scoop:
                SpawnSlashArc(point, Vector2.up, 1f, hitDirection.x < 0f ? -25f : 25f);
                SpawnDust(transform.position, 7, .85f);
                break;
        }
        Vector2 spray = hitProfile.kind == WeaponHitKind.Scoop ? Vector2.up : hitDirection;
        SpawnSparks(point, spray, hitProfile.sparks, hitStrength, hitProfile.accent, hitProfile.kind == WeaponHitKind.Slam);
    }

    static void SpawnSlashArc(Vector2 position, Vector2 direction, float scale = 1f, float tilt = 0f)
    {
        var go = new GameObject("HitSlashArc") { hideFlags = HideFlags.DontSave };
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = slashSprite;
        sr.sharedMaterial = unlitSpriteMaterial;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 55;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + tilt;
        go.AddComponent<HitFxParticle>().InitSlash(angle, scale);
    }

    static void SpawnImpactStar(Vector2 position, float size = 1f)
    {
        var go = new GameObject("HitStar") { hideFlags = HideFlags.DontSave };
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = starSprite;
        sr.sharedMaterial = unlitSpriteMaterial;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 56;
        go.AddComponent<HitFxParticle>().InitStar(size);
    }

    static void SpawnStreak(Vector2 position, Vector2 direction, float length, Color accent)
    {
        var go = new GameObject("HitPuncture") { hideFlags = HideFlags.DontSave };
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pixelSprite;
        sr.sharedMaterial = unlitSpriteMaterial;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 56;
        sr.color = accent;
        go.AddComponent<HitFxParticle>().InitStreak(direction, length * PixelsPerUnit);
    }

    static void SpawnDust(Vector2 position, int count, float power)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("HitDust") { hideFlags = HideFlags.DontSave };
            go.transform.position = position + Vector2.up * .03f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.sharedMaterial = unlitSpriteMaterial;
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 25;
            sr.color = Color.Lerp(new Color(.65f, .49f, .29f, .7f), new Color(.94f, .84f, .6f, .8f), Random.value);
            float side = i % 2 == 0 ? -1f : 1f;
            Vector2 velocity = new Vector2(side * Random.Range(.7f, 1.9f), Random.Range(.2f, .7f)) * power;
            go.AddComponent<HitFxParticle>().InitDust(velocity, Random.Range(2f, 3.5f));
        }
    }

    static void SpawnSparks(Vector2 position, Vector2 direction, int count, float power, Color accent, bool radial = false)
    {

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = radial
                ? Random.insideUnitCircle.normalized
                : (Vector2)(Quaternion.Euler(0f, 0f, Random.Range(-52f, 52f)) * direction);
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;

            var go = new GameObject("HitSpark") { hideFlags = HideFlags.DontSave };
            go.transform.position = position + dir * Random.Range(0.02f, 0.08f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.sharedMaterial = unlitSpriteMaterial;
            sr.color = i % 3 == 0 ? new Color(1f, .98f, .88f) : accent;
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 57;

            float sizePx = i % 3 == 0 ? 2f : 1f;
            Vector2 vel = dir * Random.Range(1.5f, 3.3f) * power + Vector2.up * Random.Range(0.3f, 0.9f);
            go.AddComponent<HitFxParticle>().InitSpark(vel, sizePx);
        }
    }
}

/// <summary>Short-lived procedural combat particle (slash arc, impact star, or pixel spark).</summary>
public class HitFxParticle : MonoBehaviour
{
    enum Kind { Spark, Star, Slash, Streak, Dust }
    Kind kind;
    Vector2 velocity;
    float age, life;
    float baseSize;
    SpriteRenderer sr;
    Color baseColor;

    public void InitSpark(Vector2 v, float px)
    {
        kind = Kind.Spark;
        velocity = v;
        life = Random.Range(0.17f, 0.28f);
        baseSize = px;
        Setup();
        transform.localScale = new Vector3(baseSize * 2f, baseSize, 1f);
        if (v.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
    }

    public void InitStar(float size)
    {
        kind = Kind.Star;
        life = 0.14f;
        baseSize = size;
        Setup();
        transform.localScale = Vector3.one * size;
        transform.rotation = Quaternion.identity;
    }

    public void InitSlash(float angleDeg, float size)
    {
        kind = Kind.Slash;
        life = 0.16f;
        baseSize = size;
        Setup();
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        transform.localScale = new Vector3(size, size, 1f);
    }

    public void InitStreak(Vector2 direction, float lengthPixels)
    {
        kind = Kind.Streak;
        life = .12f;
        baseSize = lengthPixels;
        velocity = direction * .6f;
        Setup();
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.localScale = new Vector3(baseSize, 2f, 1f);
    }

    public void InitDust(Vector2 v, float size)
    {
        kind = Kind.Dust;
        velocity = v;
        life = .32f;
        baseSize = size;
        Setup();
        transform.localScale = new Vector3(size * 1.5f, size, 1f);
    }

    void Setup()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr ? sr.color : Color.white;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / life);

        if (kind == Kind.Star)
        {
            float s = baseSize * (t < .2f ? Mathf.Lerp(.8f, 1.15f, t / .2f) : Mathf.Lerp(1.15f, .75f, (t - .2f) / .8f));
            transform.localScale = new Vector3(s, s, 1f);
            if (sr)
            {
                Color c = Color.white;
                c.a = 1f - Mathf.InverseLerp(.3f, 1f, t);
                sr.color = c;
            }
        }
        else if (kind == Kind.Slash)
        {
            float sx = baseSize * Mathf.Lerp(.85f, 1.18f, t);
            float sy = baseSize * Mathf.Lerp(.95f, 1.18f, t);
            transform.localScale = new Vector3(sx, sy, 1f);
            if (sr)
            {
                Color c = t < 0.45f
                    ? Color.Lerp(new Color(1f, 0.99f, 0.92f, 0.95f), new Color(1f, 0.85f, 0.35f, 0.9f), t / 0.45f)
                    : Color.Lerp(new Color(1f, 0.85f, 0.35f, 0.9f), new Color(1f, 0.45f, 0.15f, 0f), (t - 0.45f) / 0.55f);
                sr.color = c;
            }
        }
        else if (kind == Kind.Streak)
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.localScale = new Vector3(baseSize * (1f + t * .3f), Mathf.Max(.5f, 2f * (1f - t)), 1f);
            if (sr) sr.color = Color.Lerp(Color.white, new Color(baseColor.r, baseColor.g, baseColor.b, 0f), t);
        }
        else if (kind == Kind.Dust)
        {
            velocity *= Mathf.Pow(.05f, Time.deltaTime);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            float size = Mathf.Round(baseSize * (1f + .45f * t));
            transform.localScale = new Vector3(size * 1.5f, size, 1f);
            if (sr) sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t) * (1f - t));
        }
        else
        {
            velocity *= Mathf.Pow(0.03f, Time.deltaTime);
            velocity += Vector2.down * 4.5f * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            if (velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);

            float s = Mathf.Max(1f, Mathf.Round(Mathf.Lerp(baseSize, 1f, t)));
            transform.localScale = new Vector3(s * (t < .5f ? 2f : 1f), s, 1f);
            if (sr)
            {
                Color c = baseColor;
                c.a = 1f - t * t;
                sr.color = c;
            }
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}

/// <summary>Crisp world-space floating damage number that arcs above the enemy health bar.</summary>
public class DamageNumber : MonoBehaviour
{
    Text label;
    Vector3 origin;
    float age, driftX;
    Color baseColor;
    bool heavy;
    const float Lifetime = 0.72f;
    static Font font;

    public static void Show(Vector3 position, float amount, bool isHeavy, float hitDirX = 0f)
    {
        if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("DamageNumber", typeof(RectTransform), typeof(Canvas))
        {
            hideFlags = HideFlags.DontSave
        };
        root.transform.position = position;
        root.transform.localScale = Vector3.one * 0.0085f;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "Foreground";
        canvas.sortingOrder = 120;

        var textObject = new GameObject("Value", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(root.transform, false);

        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = isHeavy ? 34 : 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
        text.rectTransform.sizeDelta = new Vector2(180f, 54f);
        text.color = isHeavy ? new Color(1f, 0.90f, 0.55f, 1f) : Color.white;

        var outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.09f, 0.05f, 0.07f, 0.96f);
        outline.effectDistance = new Vector2(2.4f, -2.4f);

        var n = root.AddComponent<DamageNumber>();
        n.label = text;
        n.origin = position;
        n.baseColor = text.color;
        n.heavy = isHeavy;
        float bias = Mathf.Abs(hitDirX) > 0.1f ? Mathf.Sign(hitDirX) * 0.18f : 0f;
        n.driftX = bias + Random.Range(-0.14f, 0.14f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Lifetime);

        float peakScale = heavy ? 1.3f : 1.18f;
        float pop = t < .08f
            ? Mathf.Lerp(.85f, peakScale, t / .08f)
            : Mathf.Lerp(peakScale, 1f, Mathf.Clamp01((t - .08f) / .16f));
        transform.localScale = Vector3.one * (0.0085f * pop);

        float hop = (1f - Mathf.Pow(1f - t, 3f)) * .35f - .10f * t * t;
        transform.position = origin + new Vector3(driftX * t, hop, 0f);

        if (label)
        {
            Color c = baseColor;
            c.a = 1f - Mathf.InverseLerp(0.58f, 1f, t);
            label.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}
