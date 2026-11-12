using UnityEngine;

public class ResourceNode : InteractionTarget {
    public ResourceType type;
    public bool acceptsAxe = true, regrows = true;
    [Min(1)] public int maxHealth=3, minYield=2, maxYield=4;
    [Min(.1f)] public float respawnSeconds=120;
    public Sprite dropSprite;
    public int Health {get;private set;}
    public bool Depleted {get;private set;}
    public override bool Available => type==ResourceType.Scrap && !Depleted;
    public override string Prompt => "[E] Gather scrap";
    [SerializeField] SpriteRenderer visual;
    Collider2D solid;
    Bounds footprint;
    Color tint;
    float restoreAt;
    ResourceHitFeedback hitFeedback;
    int harvest;
    readonly System.Collections.Generic.List<ResourcePickup> uncollected = new System.Collections.Generic.List<ResourcePickup>();
    void Awake() {
        if (!visual) visual = GetComponent<SpriteRenderer>();
        if (!visual) { Debug.LogError("Missing resource visual on " + name, this); enabled = false; return; }
        solid=GetComponent<Collider2D>();
        tint=visual.color; Health=Mathf.Max(1,maxHealth);
        if (!dropSprite && type!=ResourceType.Scrap) {Debug.LogError("Missing drop sprite on "+name,this);enabled=false;}
    }
    public Vector2 HitPoint => solid && solid.enabled ? (Vector2)solid.bounds.center : (Vector2)transform.position;
    public override void Use(PlayerInteractionController p) {
        if (PlayerInteractionController.InputBlocked || !Reachable(p.transform.position)) return;
        int amount=Yield();
        if (!p.wallet.TryAddResource(type,amount)) { p.Notice("INVENTORY FULL!"); return; }
        Deplete();
        p.Notice("+"+amount+" SCRAP");
    }
    int Yield() => Random.Range(Mathf.Clamp(minYield,1,32),Mathf.Clamp(maxYield,Mathf.Clamp(minYield,1,32),32)+1);
    public void Hit(int damage,PlayerInteractionController player) {
        if (!isActiveAndEnabled || !acceptsAxe || Depleted || damage<=0) return;
        Health=Mathf.Max(0,Health-damage);
        if (type == ResourceType.Wood) AudioManager.Instance?.PlayWoodHit(); else if (type == ResourceType.Stone) AudioManager.Instance?.PlayStoneHit();
        if (!hitFeedback) hitFeedback=gameObject.AddComponent<ResourceHitFeedback>();
        hitFeedback.Play(visual,type,player ? (Vector2)player.transform.position : HitPoint+Vector2.left,Health==0);
        if (Health>0) {player.Notice(type+" node: "+Health+" hits left");return;}
        Deplete();
        int amount=Yield(), dropped=0;
        for(int i=0;i<amount;i++) if(Drop(i)) dropped++;
        HarvestPopup.Show(footprint.center,dropped,type,player && player.message ? player.message.font : null);
        player.Notice(type==ResourceType.Wood?"TREE DEPLETED":"ROCK DEPLETED");
    }
    void Deplete() {
        Depleted=true; Health=0; harvest++;
        footprint=solid?solid.bounds:new Bounds(transform.position,Vector3.one*.6f);
        if(solid)solid.enabled=false;
        visual.enabled=false; restoreAt=Time.time+Mathf.Max(.1f,respawnSeconds);
    }
    bool Drop(int i) {
        Vector2 center=footprint.center;
        for(int attempt=0;attempt<48;attempt++) {
            float angle=i*2.4f+attempt*.61f;
            Vector2 pos=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(.4f+.035f*attempt);
            if(Physics2D.OverlapCircle(pos,.18f))continue;
            var go=new GameObject(name+" Drop "+harvest+"-"+i);
            go.transform.position=pos;
            var r=go.AddComponent<SpriteRenderer>();
            r.sprite=dropSprite;r.sharedMaterial=visual.sharedMaterial;r.sortingLayerID=visual.sortingLayerID;r.spriteSortPoint=SpriteSortPoint.Pivot;
            var pickup=go.AddComponent<ResourcePickup>();
            pickup.type=type;
            uncollected.Add(pickup);
            return true;
        }
        Debug.LogWarning("No clear space for drop at "+name,this);
        return false;
    }
    void Update() {
        if(!Depleted)return;
        if(!regrows || Time.time<restoreAt)return;
        uncollected.RemoveAll(p => !p || !p.isActiveAndEnabled);
        if(uncollected.Count>0){restoreAt=Time.time+.5f;return;}
        if(Physics2D.OverlapBox(footprint.center,footprint.size,0)){restoreAt=Time.time+.5f;return;}
        Health=Mathf.Max(1,maxHealth);Depleted=false;visual.color=tint;visual.enabled=true;
        if(solid)solid.enabled=true;
    }
}
