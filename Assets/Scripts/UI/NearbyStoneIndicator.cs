using UnityEngine;

// A world-space indicator stays legible even when the player's body covers a rock.
public class NearbyStoneIndicator : MonoBehaviour
{
    public SpriteRenderer[] rocks;
    public PlayerToolController tools;
    public EquipmentController equipment;
    [Header("Optional label styling")] public Font font;
    public Sprite labelSprite, pointerSprite, iconSprite;
    SpriteRenderer backdrop, pointer, icon;
    LineRenderer ring;
    TextMesh label;
    Material material;
    void Start()
    {
        var go = new GameObject("Nearby Stone Indicator");
        go.transform.SetParent(transform, false);
        ring = go.AddComponent<LineRenderer>();
        material = new Material(Shader.Find("Sprites/Default"));
        ring.sharedMaterial = material;
        ring.useWorldSpace = true; ring.loop = true; ring.positionCount = 32;
        ring.startWidth = ring.endWidth = .035f;
        ring.sortingLayerName = "Foreground"; ring.sortingOrder = 100;
        var text = new GameObject("Stone Hint");
        text.transform.SetParent(go.transform, false);
        label = text.AddComponent<TextMesh>();
        label.fontSize = 48; label.characterSize = .065f;
        label.anchor = TextAnchor.MiddleCenter;
        var renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Foreground"; renderer.sortingOrder = 101;
        if (font) { label.font = font; renderer.sharedMaterial = font.material; label.fontSize = 40; label.characterSize = .06f; }
        if (labelSprite)
        {
            ring.sortingOrder = 99;
            backdrop = Part("Label Backdrop", go.transform, labelSprite, 100);
            backdrop.drawMode = SpriteDrawMode.Sliced;
            if (pointerSprite) pointer = Part("Label Pointer", go.transform, pointerSprite, 100);
            if (iconSprite) icon = Part("Label Icon", go.transform, iconSprite, 101);
        }
    }
    SpriteRenderer Part(string name, Transform parent, Sprite sprite, int order)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(parent, false);
        r.sprite = sprite; r.sortingLayerName = "Foreground"; r.sortingOrder = order;
        return r;
    }
    void LateUpdate()
    {
        if (!ring) return;
        SpriteRenderer nearest = null;
        float distance = 1.5f;
        if (!InventoryUI.IsOpen && !PlayerSurvivalController.InputLocked && rocks != null)
        foreach (var rock in rocks)
        {
            if (!rock || !rock.enabled || !rock.gameObject.activeInHierarchy) continue;
            var collider = rock.GetComponent<Collider2D>();
            if (!collider || !collider.enabled) continue;
            float d = Vector2.Distance(transform.position, collider.ClosestPoint(transform.position));
            if (d >= distance) continue;
            distance = d; nearest = rock;
        }
        ring.enabled = false;
        label.gameObject.SetActive(nearest);
        if (backdrop) backdrop.enabled = nearest;
        if (pointer) pointer.enabled = nearest;
        if (icon) icon.enabled = nearest;
        if (!nearest) return;
        var solid = nearest.GetComponent<Collider2D>();
        var node = nearest.GetComponent<ResourceNode>();
        bool mineable = node && node.isActiveAndEnabled && !node.Depleted && node.type == ResourceType.Stone && node.acceptsAxe;
        Color color = mineable ? new Color(1,.85f,.2f) : new Color(.75f,.8f,.85f);
        ring.startColor = ring.endColor = color; label.color = color;
        label.text = mineable ? (!equipment.PickaxeEquipped ? "EQUIP PICKAXE TO MINE" :
            tools.FindTarget(ResourceType.Stone) == node ? "[R] MINE  " + node.Health + "/" + node.maxHealth : "MINEABLE — FACE ROCK")
            : "SOLID ROCK";
        var center = solid.bounds.center;
        float radius = Mathf.Max(.3f, solid.bounds.extents.x + .1f);
        for (int i=0;i<32;i++)
        {
            float angle = i * Mathf.PI * 2 / 32;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle)*radius, Mathf.Sin(angle)*radius*.55f,0));
        }
        label.transform.position = center + Vector3.up * (Mathf.Max(.6f,nearest.bounds.extents.y)+.35f);
        if (backdrop)
        {
            // Pill: [icon] TEXT, with a small pointer aimed at the rock.
            Vector3 anchor = label.transform.position;
            float textW = label.GetComponent<MeshRenderer>().bounds.size.x;
            float iconW = icon ? .26f : 0, gap = icon ? .08f : 0, padX = .14f, height = .38f;
            float total = iconW + gap + textW;
            float left = anchor.x - total * .5f;
            label.transform.position = new Vector3(left + iconW + gap + textW * .5f, anchor.y, anchor.z);
            backdrop.transform.position = anchor;
            backdrop.size = new Vector2(total + padX * 2, height);
            if (icon)
            {
                icon.transform.position = new Vector3(left + iconW * .5f, anchor.y, anchor.z);
                float s = iconW / Mathf.Max(.01f, Mathf.Max(icon.sprite.bounds.size.x, icon.sprite.bounds.size.y));
                icon.transform.localScale = Vector3.one * s;
            }
            if (pointer) pointer.transform.position = anchor + Vector3.down * (height * .5f + pointer.sprite.bounds.extents.y - .02f);
        }
    }
    void OnDisable()
    {
        if (ring) ring.enabled = false;
        if (backdrop) backdrop.enabled = false;
        if (pointer) pointer.enabled = false;
        if (icon) icon.enabled = false;
        if (label) label.gameObject.SetActive(false);
    }
    void OnDestroy() { if (material) Destroy(material); }
}
