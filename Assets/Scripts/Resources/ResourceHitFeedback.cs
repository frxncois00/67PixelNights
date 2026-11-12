using UnityEngine;

/// <summary>Moves a temporary visual only. The resource and its collider stay fixed.</summary>
public class ResourceHitFeedback : MonoBehaviour
{
    SpriteRenderer source, reaction;
    ResourceNode node;
    Color baseColor;
    Vector3 visualOffset, visualScale;
    Quaternion visualRotation;
    bool tree, depleted, wasVisible;
    float age, duration, direction;
    readonly SpriteRenderer[] chips = new SpriteRenderer[7];
    readonly Vector3[] chipVelocity = new Vector3[7];
    readonly Vector3[] chipOrigin = new Vector3[7];
    static Sprite chipSprite;

    public void Play(SpriteRenderer original, ResourceType type, Vector2 attacker, bool finalHit)
    {
        if (!source)
        {
            source = original;
            node = GetComponent<ResourceNode>();
            var child = new GameObject("Resource Hit Visual") { hideFlags = HideFlags.DontSave };
            bool separateVisual = source.transform != transform;
            child.transform.SetParent(separateVisual ? source.transform.parent : transform, false);
            visualOffset = separateVisual ? source.transform.localPosition : Vector3.zero;
            visualRotation = separateVisual ? source.transform.localRotation : Quaternion.identity;
            visualScale = separateVisual ? source.transform.localScale : Vector3.one;
            reaction = child.AddComponent<SpriteRenderer>();
            reaction.enabled = false;
            if (!chipSprite)
            {
                var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                chipSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 32);
                chipSprite.hideFlags = HideFlags.DontSave;
            }
            for (int i = 0; i < chips.Length; i++)
            {
                var chip = new GameObject("Resource Chip") { hideFlags = HideFlags.DontSave };
                chip.transform.SetParent(transform, false);
                chips[i] = chip.AddComponent<SpriteRenderer>();
                chips[i].sprite = chipSprite;
                chips[i].sharedMaterial = source.sharedMaterial;
                chips[i].sortingLayerName = "Objects";
                chips[i].sortingOrder = source.sortingOrder + 1;
            }
        }

        if (!reaction.enabled) wasVisible = source.enabled;
        // The node supplies its normal tint, before any impact highlight.
        baseColor = source.color;
        tree = type == ResourceType.Wood;
        depleted = finalHit;
        age = 0;
        duration = finalHit ? .42f : .24f;
        direction = attacker.x <= transform.position.x ? 1 : -1;
        reaction.sprite = source.sprite;
        reaction.sharedMaterial = source.sharedMaterial;
        reaction.sortingLayerID = source.sortingLayerID;
        reaction.sortingOrder = source.sortingOrder;
        reaction.spriteSortPoint = source.spriteSortPoint;
        reaction.flipX = source.flipX;
        reaction.flipY = source.flipY;
        reaction.transform.localPosition = visualOffset;
        reaction.transform.localRotation = visualRotation;
        reaction.transform.localScale = visualScale;
        reaction.enabled = true;
        source.enabled = false;
        Vector3 impact = transform.InverseTransformPoint(node.HitPoint);
        for (int i = 0; i < chips.Length; i++)
        {
            // A few leaves higher in the canopy; wood/stone fragments near impact.
            chipOrigin[i] = impact + (tree && i < 3 ? Vector3.up * source.sprite.bounds.size.y * .55f : Vector3.up * .15f);
            chipVelocity[i] = new Vector3((i - 3) * .36f, .9f + (i % 3) * .4f, 0);
            chips[i].transform.localPosition = chipOrigin[i];
            chips[i].transform.localScale = Vector3.one * (i % 2 == 0 ? 1 : .7f);
            chips[i].color = tree ? (i < 3 ? new Color(.68f, .83f, .28f) : new Color(.87f, .65f, .36f)) : new Color(.7f, .85f, .89f);
            chips[i].enabled = true;
        }
        enabled = true;
    }

    void Update()
    {
        if (!source || !reaction) { enabled = false; return; }
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / duration);
        float kick = Mathf.Sin(t * Mathf.PI * 3) * (1 - t);
        var visual = reaction.transform;
        visual.localPosition = visualOffset + new Vector3(direction * kick * (tree ? .035f : .065f), 0, 0);
        float angle = depleted && tree ? -direction * 12 * t * t : -direction * kick * (tree ? 3 : 1.5f);
        visual.localRotation = visualRotation * Quaternion.Euler(0, 0, angle);
        visual.localScale = visualScale * (depleted ? 1 - .12f * t : 1 + .015f * Mathf.Sin(t * Mathf.PI));
        Color highlight = tree ? new Color(1, .9f, .66f) : new Color(.8f, .95f, 1);
        Color color = Color.Lerp(baseColor, highlight, .35f * (1 - t));
        color.a = baseColor.a * (depleted ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 1, t)) : 1);
        reaction.color = color;
        for (int i = 0; i < chips.Length; i++)
        {
            chips[i].transform.localPosition = chipOrigin[i] + chipVelocity[i] * age + Vector3.down * (3 * age * age);
            var c = chips[i].color; c.a = 1 - t; chips[i].color = c;
        }
        if (t >= 1) enabled = false;
    }

    void OnDisable()
    {
        if (reaction) reaction.enabled = false;
        foreach (var chip in chips) if (chip) chip.enabled = false;
        if (source && node && !node.Depleted) source.enabled = wasVisible;
    }
}
