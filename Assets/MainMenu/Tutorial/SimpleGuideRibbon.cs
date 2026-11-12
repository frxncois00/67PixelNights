using UnityEngine;

/// <summary>A compact cloth label with a single inward notch, without folded ribbon tails.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SimpleGuideRibbon : UnityEngine.UI.MaskableGraphic
{
    [Min(1)] public float outline = 2.2f;
    [Min(1)] public float notch = 13;
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        var rect=rectTransform.rect;
        AddStrip(vh,rect,notch,new Color(.055f,.13f,.15f),new Color(.055f,.13f,.15f));
        rect.xMin+=outline;rect.xMax-=outline;rect.yMin+=outline;rect.yMax-=outline;
        AddStrip(vh,rect,Mathf.Max(2,notch-outline),new Color(.20f,.57f,.63f),new Color(.12f,.40f,.46f));
        // A fine inner stitch/light edge gives the flat label depth without extra tails.
        int index=vh.currentVertCount;
        var tint=new Color(.49f,.79f,.78f,.7f);
        vh.AddVert(new Vector3(rect.xMin+3,rect.yMax-1),tint,Vector2.zero);
        vh.AddVert(new Vector3(rect.xMax-3,rect.yMax-1),tint,Vector2.zero);
        vh.AddVert(new Vector3(rect.xMax-4,rect.yMax-2),tint,Vector2.zero);
        vh.AddVert(new Vector3(rect.xMin+3,rect.yMax-2),tint,Vector2.zero);
        vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
    }
    static void AddStrip(UnityEngine.UI.VertexHelper vh,Rect r,float cut,Color top,Color bottom)
    {
        var points=new[]{new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax-cut,r.center.y),new Vector2(r.xMax,r.yMin),new Vector2(r.xMin,r.yMin)};
        int first=vh.currentVertCount;
        vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
        foreach(var p in points)vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);
        for(int i=0;i<points.Length;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%points.Length);
    }
}
