using UnityEngine;
using UnityEngine.UI;

public class CookingUI : MonoBehaviour
{
    public CampfireCooking cooking;
    public GameObject panel;
    public Text label;
    public Image progress;
    void Update()
    {
        if (!cooking || !panel || !label || !progress) return;
        bool visible = !PlayerSurvivalController.InputLocked && (cooking.InRange || cooking.IsCooking);
        panel.SetActive(visible);
        if (!visible) return;
        label.text = cooking.IsCooking
            ? (cooking.Paused ? "<color=#FF785A>Cooking Paused — Light Fire</color>" : "Cooking Meat...  <color=#FFD86B>" + Mathf.CeilToInt(cooking.Duration - cooking.Elapsed) + "s</color>")
            : "[C] Cook Meat";
        progress.fillAmount = cooking.IsCooking ? cooking.Progress : 0;
    }
}
