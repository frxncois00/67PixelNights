using UnityEngine;

/// <summary>Fits a fixed illustrated layout into the modal's responsive safe area.</summary>
[ExecuteAlways]
public sealed class TutorialReferenceFit : MonoBehaviour
{
    public Vector2 referenceSize = new Vector2(1460,850);
    [Range(.5f,1)] public float displayScale = 1;
    void LateUpdate()
    {
        var rect=(RectTransform)transform;
        var parent=rect.parent as RectTransform;
        if(!parent)return;
        rect.sizeDelta=referenceSize;
        float scale=Mathf.Min(parent.rect.width/referenceSize.x,parent.rect.height/referenceSize.y);
        // Only the visual alpha is animated; this component owns the responsive scale.
        rect.localScale=Vector3.one*Mathf.Max(.01f,scale*displayScale);
    }
}
