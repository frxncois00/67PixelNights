using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HomeMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public CanvasGroup highlight;
    public HomeMenuController menu;
    public string hint;
    bool pointer, selected;
    void Awake() { GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Clicked); }
    void Clicked() { menu.ClickFeedback((RectTransform)transform); }
    public void OnPointerEnter(PointerEventData e) { pointer = true; menu.SetHint(hint); }
    public void OnPointerExit(PointerEventData e) { pointer = false; menu.SetHint(""); }
    public void OnSelect(BaseEventData e) { selected = true; menu.SetHint(hint); }
    public void OnDeselect(BaseEventData e) { selected = false; }
    void Update()
    {
        float target = pointer || selected ? .75f + .15f * Mathf.Sin(Time.unscaledTime * 3) : 0;
        highlight.alpha = Mathf.MoveTowards(highlight.alpha, target, Time.unscaledDeltaTime * 6);
    }
    void OnDisable() { pointer = selected = false; if (highlight) highlight.alpha = 0; }
}
