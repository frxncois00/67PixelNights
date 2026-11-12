using UnityEngine;

// Visual-only roasting feedback; CampfireCooking remains the sole recipe/timer authority.
public class CookingAnimation : MonoBehaviour
{
    public CampfireCooking cooking;
    public SpriteRenderer fireVisual;
    SpriteRenderer meat;
    LineRenderer[] steam;
    Material steamMaterial;
    float animationTime;
    void Start()
    {
        var go = new GameObject("Roasting Meat");
        go.transform.SetParent(transform, false);
        meat = go.AddComponent<SpriteRenderer>();
        meat.sharedMaterial = fireVisual.sharedMaterial;
        meat.sortingLayerID = fireVisual.sortingLayerID;
        meat.sortingOrder = fireVisual.sortingOrder + 2;
        steamMaterial = new Material(Shader.Find("Sprites/Default"));
        steam = new LineRenderer[2];
        for(int i=0;i<2;i++)
        {
            var trail = new GameObject("Cooking Steam " + (i+1));
            trail.transform.SetParent(go.transform,false);
            var line = trail.AddComponent<LineRenderer>();steam[i]=line;
            line.sharedMaterial=steamMaterial;line.useWorldSpace=false;line.positionCount=8;
            line.startWidth=.025f;line.endWidth=.008f;
            line.sortingLayerID=meat.sortingLayerID;line.sortingOrder=meat.sortingOrder;
        }
        go.SetActive(false);
    }
    void LateUpdate()
    {
        if (!meat || !cooking) return;
        meat.gameObject.SetActive(cooking.IsCooking);
        if (!cooking.IsCooking) { animationTime=0; return; }
        if (!cooking.Paused) animationTime += Time.deltaTime;
        meat.sprite=cooking.rawMeat.Icon;
        meat.color=Color.Lerp(cooking.rawMeat.IconTint,cooking.cookedMeat.IconTint,cooking.Progress);
        meat.transform.localPosition=new Vector3(0,.65f+Mathf.Sin(animationTime*5)*.055f,0);
        meat.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(animationTime*3)*12);
        for(int i=0;i<steam.Length;i++)
        {
            var line=steam[i];line.enabled=!cooking.Paused;
            line.startColor=new Color(1,.95f,.8f,.6f);line.endColor=new Color(1,.95f,.8f,0);
            for(int j=0;j<8;j++)
                line.SetPosition(j,new Vector3((i==0?-.14f:.14f)+Mathf.Sin(animationTime*3+j*.7f+i)*.04f,.22f+j*.055f,0));
        }
    }
    void OnDisable() { if(meat)meat.gameObject.SetActive(false); }
    void OnDestroy() { if(steamMaterial)Destroy(steamMaterial); }
}
