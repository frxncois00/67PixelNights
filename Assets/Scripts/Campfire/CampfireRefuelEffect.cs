using UnityEngine;

// Runs after successful fuel spending, alongside the existing wood-transfer action.
[DefaultExecutionOrder(100)]
public class CampfireRefuelEffect : MonoBehaviour
{
    public CampfireController fire;
    public CampfireLevelVisual visuals;
    [Min(.1f)] public float duration = .9f;
    SpriteRenderer[] sparks;
    GameObject root;
    Texture2D texture;
    Sprite sprite;
    Material material;
    Vector3 normalScale;
    float elapsed;
    public bool IsPlaying { get; private set; }
    void OnEnable()
    {
        if (!fire) fire = GetComponent<CampfireController>();
        if (!visuals) visuals = GetComponent<CampfireLevelVisual>();
        if (!fire || !visuals || !visuals.effectVisual) { Debug.LogError("CampfireRefuelEffect needs fuel and native flame references.", this); enabled=false; return; }
        normalScale=visuals.effectVisual.transform.localScale;
        fire.FuelAdded += Play;
    }
    void Play()
    {
        if (!root)
        {
            root=new GameObject("Refuel Embers");root.transform.SetParent(transform,false);
            texture=new Texture2D(1,1);texture.filterMode=FilterMode.Point;texture.SetPixel(0,0,Color.white);texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),32);
            material=new Material(Shader.Find("Sprites/Default"));
            sparks=new SpriteRenderer[12];
            for(int i=0;i<sparks.Length;i++) {var go=new GameObject("Ember "+i);go.transform.SetParent(root.transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.sortingLayerName="Foreground";r.sortingOrder=19;sparks[i]=r;}
        }
        elapsed=0;IsPlaying=true;root.SetActive(true);
    }
    void LateUpdate()
    {
        if(!IsPlaying)return;
        elapsed+=Time.deltaTime;
        if(!fire.IsBurning||elapsed>=Mathf.Max(.1f,duration)){Stop();return;}
        float t=elapsed/Mathf.Max(.1f,duration);
        float flare=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/.7f))*(1-t);
        visuals.effectVisual.transform.localScale=Vector3.Scale(normalScale,new Vector3(1+flare*.25f,1+flare*.55f,1));
        for(int i=0;i<sparks.Length;i++)
        {
            float a=i*2.39996f;
            var r=sparks[i];r.transform.localPosition=new Vector3(Mathf.Cos(a)*t*(.5f+(i%3)*.2f),.25f+t*(1.2f+(i%4)*.2f)-t*t*.25f,0);
            r.transform.localScale=Vector3.one*(i%3==0?2:1);
            r.color=new Color(1,Mathf.Lerp(.85f,.3f,t),.08f,1-t);
        }
    }
    void Stop(){IsPlaying=false;if(root)root.SetActive(false);if(visuals&&visuals.effectVisual)visuals.effectVisual.transform.localScale=normalScale;}
    void OnDisable(){if(fire)fire.FuelAdded-=Play;Stop();}
    void OnDestroy(){if(root)Destroy(root);if(sprite)Destroy(sprite);if(texture)Destroy(texture);if(material)Destroy(material);}
}
