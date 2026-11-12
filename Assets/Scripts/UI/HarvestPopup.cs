using UnityEngine;
using UnityEngine.UI;

/// <summary>One short-lived world-space yield label, independent of the HUD.</summary>
public class HarvestPopup : MonoBehaviour
{
    Text label;
    Vector3 origin;
    float age;
    Color color;
    const float Lifetime = 1.25f;

    public static void Show(Vector3 position, int amount, ResourceType type, Font font)
    {
        if (amount <= 0) return;
        var root = new GameObject("Harvest +" + amount + " " + type, typeof(RectTransform), typeof(Canvas))
        { hideFlags = HideFlags.DontSave };
        root.transform.position = position + Vector3.up * .9f;
        root.transform.localScale = Vector3.one * .01f;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "Foreground";
        canvas.sortingOrder = 100;
        var textObject = new GameObject("Yield", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(root.transform, false);
        var text = textObject.GetComponent<Text>();
        text.font = font ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = "+" + amount + " " + type.ToString().ToUpperInvariant() + "\n<size=14>DROPPED</size>";
        text.rectTransform.sizeDelta = new Vector2(300, 85);
        text.color = type == ResourceType.Wood ? new Color(1, .88f, .48f) : new Color(.65f, .93f, 1);
        var outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(.07f, .09f, .08f, 1);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        var popup = root.AddComponent<HarvestPopup>();
        popup.label = text; popup.origin = root.transform.position; popup.color = text.color;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Lifetime);
        float pop = t < .16f ? Mathf.Lerp(.65f, 1.16f, t / .16f) : Mathf.Lerp(1.16f, 1, Mathf.Clamp01((t - .16f) / .15f));
        transform.localScale = Vector3.one * (.01f * pop);
        transform.position = origin + Vector3.up * (.75f * t);
        color.a = 1 - Mathf.InverseLerp(.65f, 1, t);
        if (label) label.color = color;
        if (t >= 1) Destroy(gameObject);
    }
}
