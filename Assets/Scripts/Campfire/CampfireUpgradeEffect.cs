using UnityEngine;

// Local visual celebration only: never changes fuel, safety, time or player input.
public class CampfireUpgradeEffect : MonoBehaviour
{
    public CampfireUpgrade upgrade;
    [Min(.2f)] public float duration = 1.8f;
    public Color gold = new Color(1f, .76f, .2f);
    GameObject effectRoot;
    LineRenderer ring, echo;
    SpriteRenderer[] sparks;
    TextMesh label;
    Material material;
    Texture2D texture;
    Sprite sparkSprite;
    float elapsed;
    int observedLevel;
    public bool IsPlaying => effectRoot && effectRoot.activeSelf;

    void OnEnable()
    {
        if (!upgrade) upgrade = GetComponent<CampfireUpgrade>();
        if (!upgrade) { Debug.LogError("CampfireUpgradeEffect requires CampfireUpgrade.", this); enabled = false; return; }
        observedLevel = upgrade.CurrentLevel;
        upgrade.Changed += OnUpgrade;
    }
    void OnUpgrade()
    {
        if (upgrade.CurrentLevel <= observedLevel) return;
        observedLevel = upgrade.CurrentLevel;
        if (!effectRoot) CreateVisuals();
        elapsed = 0;
        label.text = "CAMPFIRE\nLEVEL " + observedLevel;
        effectRoot.SetActive(true);
        Render(0);
    }
    LineRenderer MakeRing(string name)
    {
        var go = new GameObject(name); go.transform.SetParent(effectRoot.transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material; line.useWorldSpace = false; line.loop = true;
        line.positionCount = 48; line.widthMultiplier = .04f;
        line.sortingLayerName = "Foreground"; line.sortingOrder = 20;
        return line;
    }
    void CreateVisuals()
    {
        effectRoot = new GameObject("Campfire Upgrade Celebration");
        effectRoot.transform.SetParent(transform, false);
        material = new Material(Shader.Find("Sprites/Default"));
        ring = MakeRing("Golden ripple"); echo = MakeRing("Delayed ripple");
        texture = new Texture2D(1, 1); texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white); texture.Apply();
        sparkSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f,.5f), 32);
        sparks = new SpriteRenderer[20];
        for (int i = 0; i < sparks.Length; i++)
        {
            var go = new GameObject("Golden spark " + i); go.transform.SetParent(effectRoot.transform, false);
            sparks[i] = go.AddComponent<SpriteRenderer>(); sparks[i].sprite = sparkSprite;
            sparks[i].sharedMaterial = material; sparks[i].sortingLayerName = "Foreground"; sparks[i].sortingOrder = 21;
        }
        var text = new GameObject("Level label"); text.transform.SetParent(effectRoot.transform, false);
        label = text.AddComponent<TextMesh>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48; label.characterSize = .075f; label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center; label.fontStyle = FontStyle.Bold;
        var renderer = text.GetComponent<MeshRenderer>(); renderer.sharedMaterial = label.font.material;
        renderer.sortingLayerName = "Foreground"; renderer.sortingOrder = 22;
    }
    void Update()
    {
        if (!IsPlaying) return;
        elapsed += Time.deltaTime;
        if (elapsed >= Mathf.Max(.2f, duration)) { effectRoot.SetActive(false); return; }
        Render(elapsed / Mathf.Max(.2f, duration));
    }
    void Ripple(LineRenderer line, float t)
    {
        line.enabled = t >= 0 && t < 1;
        if (!line.enabled) return;
        float radius = Mathf.Lerp(.25f, 2.2f, 1 - (1-t)*(1-t));
        var c = gold; c.a = (1-t)*.85f; line.startColor = line.endColor = c;
        for (int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.55f,0)); }
    }
    void Render(float t)
    {
        Ripple(ring,t); Ripple(echo,(t-.16f)/.84f);
        for(int i=0;i<sparks.Length;i++)
        {
            float a=i*2.39996f; float speed=.5f+(i%5)*.17f;
            var r=sparks[i]; r.transform.localPosition=new Vector3(Mathf.Cos(a)*t*speed, .25f+Mathf.Sin(a)*t*.35f+t*2-t*t*.5f,0);
            r.transform.localScale=Vector3.one*(i%3==0?2:1);
            var c=Color.Lerp(gold,Color.white,(i%3)*.3f);c.a=1-t;r.color=c;
        }
        var color=gold;color.a=Mathf.Clamp01(t/.08f)*Mathf.Clamp01((1-t)/.3f);label.color=color;
        label.transform.localPosition=new Vector3(0,-1.25f+t*.25f,0);
        label.transform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(t/.2f)*Mathf.PI)*.15f);
    }
    void OnDisable() { if(upgrade)upgrade.Changed-=OnUpgrade;if(effectRoot)effectRoot.SetActive(false); }
    void OnDestroy() { if(effectRoot)Destroy(effectRoot);if(material)Destroy(material);if(sparkSprite)Destroy(sparkSprite);if(texture)Destroy(texture); }
}
