using UnityEngine;

/// <summary>UI equivalent of the game's two eight-point cooking steam trails.</summary>
public sealed class TutorialCookingSteam : UnityEngine.UI.MaskableGraphic
{
    public float pixelsPerWorldUnit = 80;
    float elapsed;
    public void SetTime(float value) { elapsed = value; SetVerticesDirty(); }
    Vector2 Point(int trail,int point) => new Vector2(
        (trail == 0 ? -.14f : .14f) + Mathf.Sin(elapsed*3 + point*.7f + trail)*.04f,
        .22f + point*.055f) * pixelsPerWorldUnit;
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        for(int trail=0;trail<2;trail++)
            for(int j=0;j<7;j++)
            {
                Vector2 a=Point(trail,j),b=Point(trail,j+1);
                Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized;
                float wa=Mathf.Lerp(.025f,.008f,j/7f)*pixelsPerWorldUnit*.5f;
                float wb=Mathf.Lerp(.025f,.008f,(j+1)/7f)*pixelsPerWorldUnit*.5f;
                var ca=new Color(1,.95f,.8f,.6f*(1-j/7f));
                var cb=new Color(1,.95f,.8f,.6f*(1-(j+1)/7f));
                int start=vh.currentVertCount;
                vh.AddVert(a-normal*wa,ca,Vector2.zero);vh.AddVert(a+normal*wa,ca,Vector2.zero);
                vh.AddVert(b+normal*wb,cb,Vector2.zero);vh.AddVert(b-normal*wb,cb,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
    }
}
