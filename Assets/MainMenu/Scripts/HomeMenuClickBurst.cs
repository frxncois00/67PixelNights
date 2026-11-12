using UnityEngine;
using UnityEngine.UI;

public sealed class HomeMenuClickBurst : MonoBehaviour
{
    readonly RectTransform[] pieces = new RectTransform[16];
    readonly Image[] images = new Image[16];
    float age;
    public static void Spawn(RectTransform button)
    {
        var go = new GameObject("Menu Click Sparks", typeof(RectTransform), typeof(HomeMenuClickBurst));
        var rect = (RectTransform)go.transform;
        rect.SetParent(button.parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.anchoredPosition = button.anchoredPosition;
        var burst = go.GetComponent<HomeMenuClickBurst>();
        for (int i = 0; i < 16; i++)
        {
            var piece = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            var p = (RectTransform)piece.transform; p.SetParent(rect,false);
            p.sizeDelta = Vector2.one * (i % 3 + 3);
            burst.pieces[i] = p; burst.images[i] = piece.GetComponent<Image>();
            burst.images[i].color = i % 2 == 0 ? new Color(1,.87f,.44f) : new Color(1,.65f,.20f);
            burst.images[i].raycastTarget = false;
        }
    }
    void Update()
    {
        age += Time.unscaledDeltaTime;
        for (int i=0;i<pieces.Length;i++)
        {
            float angle = i * Mathf.PI * 2 / pieces.Length;
            pieces[i].anchoredPosition = new Vector2(Mathf.Cos(angle)*1.8f, Mathf.Sin(angle)) * (20 + age*170);
            var c = images[i].color; c.a = Mathf.Clamp01(1-age/.55f); images[i].color = c;
        }
        if (age >= .55f) Destroy(gameObject);
    }
}
