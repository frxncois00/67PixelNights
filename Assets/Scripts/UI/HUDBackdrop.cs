using UnityEngine;
using UnityEngine.UI;

// Framed backdrop that hugs a dynamic Text: it hides when the text is empty or
// disabled and resizes/positions itself around the visible glyphs every frame.
// Place it as a sibling directly BEFORE the target text so it renders behind it.
[RequireComponent(typeof(RectTransform))]
public class HUDBackdrop : MonoBehaviour
{
    public Text target;
    public float paddingLeft = 24, paddingRight = 24, paddingY = 10;
    public float minWidth = 0;

    RectTransform rect;
    Graphic[] graphics;

    void Awake()
    {
        rect = (RectTransform)transform;
        graphics = GetComponentsInChildren<Graphic>(true);
    }

    void LateUpdate()
    {
        bool show = target && target.isActiveAndEnabled && !string.IsNullOrEmpty(target.text);
        foreach (var g in graphics) if (g) g.enabled = show;
        if (!show) return;

        var t = target.rectTransform;
        Rect r = t.rect;
        float textW = Mathf.Min(target.preferredWidth, r.width);
        float textH = Mathf.Min(target.preferredHeight, Mathf.Max(r.height, target.preferredHeight));

        // Where the glyph block sits inside the text rect, from its alignment.
        float ax, ay;
        switch (target.alignment)
        {
            case TextAnchor.UpperLeft: ax = 0; ay = 1; break;
            case TextAnchor.UpperCenter: ax = .5f; ay = 1; break;
            case TextAnchor.UpperRight: ax = 1; ay = 1; break;
            case TextAnchor.MiddleLeft: ax = 0; ay = .5f; break;
            case TextAnchor.MiddleRight: ax = 1; ay = .5f; break;
            case TextAnchor.LowerLeft: ax = 0; ay = 0; break;
            case TextAnchor.LowerCenter: ax = .5f; ay = 0; break;
            case TextAnchor.LowerRight: ax = 1; ay = 0; break;
            default: ax = .5f; ay = .5f; break;
        }
        float cx = Mathf.Lerp(r.xMin, r.xMax, ax) + (.5f - ax) * textW;
        float cy = Mathf.Lerp(r.yMin, r.yMax, ay) + (.5f - ay) * textH;
        cx += (paddingRight - paddingLeft) * .5f;

        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(Mathf.Max(minWidth, textW + paddingLeft + paddingRight), textH + paddingY * 2);
        rect.position = t.TransformPoint(new Vector3(cx, cy, 0));
    }
}
