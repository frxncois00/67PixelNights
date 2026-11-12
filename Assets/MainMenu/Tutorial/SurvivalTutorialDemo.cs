using TMPro;
using UnityEngine;

/// <summary>UI-only crafting and eating previews, animated while the forest is paused.</summary>
public sealed class SurvivalTutorialDemo : TutorialLessonDemo
{
    public HomeGatherTutorial tutorial;
    public bool eating;
    public UnityEngine.UI.Image actor, actionIcon, result, selection, hungerFill, fire;
    public UnityEngine.UI.Image cookingFill, cookingStepFire, inventoryFood;
    public UnityEngine.UI.Image fireBase, cookingStepBase, roastingMeat, cookingStepMeat;
    public TutorialCookingSteam cookingSteam;
    public Sprite[] emberFrames;
    public float fireFramesPerSecond = 8, cookingVisualScale = 80, cookingStepScale = 30;
    public GameObject cookingMeter;
    public TextMeshProUGUI cookingLabel;
    public Vector2 cookingPosition;
    public Color rawMeatTint = Color.white, cookedMeatTint = Color.white;
    public float cookingSeconds = 5;
    public UnityEngine.UI.Image[] stepFrames;
    public RectTransform[] stepIcons;
    public Sprite[] idleFrames, actionFrames, fireFrames;
    public RectTransform pointer;
    public Vector2 pointerActionOffset;
    public CanvasGroup previewPanel;
    public TextMeshProUGUI statusLabel, hintLabel, quantityLabel, meterLabel;
    public AudioSource audioSource;
    public AudioClip actionSound, successSound, selectSound;
    public float hungerBefore = 40, hungerRestored = 35;
    public override string LessonTitle => eating ? "EATING FOOD" : "CRAFTING ITEMS";
    public int Step { get; private set; }
    public int CueCount { get; private set; }
    public bool SoundEnabled => soundEnabled;

    Vector2 actorOrigin, actionOrigin, resultOrigin, pointerOrigin;
    double started;
    int previousCycle = -1, previousCue = -1;
    bool initialized, soundEnabled = true;

    void Initialize()
    {
        if (initialized) return;
        actorOrigin = actor.rectTransform.anchoredPosition;
        actionOrigin = actionIcon.rectTransform.anchoredPosition;
        resultOrigin = result.rectTransform.anchoredPosition;
        pointerOrigin = pointer.anchoredPosition;
        initialized = true;
    }
    void OnEnable()
    {
        Initialize();
        started = Time.unscaledTimeAsDouble;
        previousCycle = previousCue = -1;
        CueCount = 0;
    }
    public override void SetSoundEnabled(bool value)
    {
        soundEnabled = value;
        if (!value && audioSource) audioSource.Stop();
    }
    void Play(AudioClip clip)
    {
        if (!soundEnabled || !clip || !audioSource || !tutorial) return;
        audioSource.volume = tutorial.EffectVolume;
        audioSource.PlayOneShot(clip);
    }
    void Update()
    {
        double elapsed = Time.unscaledTimeAsDouble - started;
        float duration = eating ? 16 : 12;
        int cycle = (int)(elapsed / duration);
        float t = (float)(elapsed % duration);
        if (cycle != previousCycle) { previousCycle = cycle; previousCue = -1; }
        Step = eating
            ? (t < 2.2f ? 0 : t < 7.7f ? 1 : t < 10.3f ? 2 : 3)
            : (t < 2.2f ? 0 : t < 4.5f ? 1 : t < 7 ? 2 : 3);
        for (int i = 0; i < stepFrames.Length; i++)
        {
            stepFrames[i].color = i == Step ? new Color(1,.78f,.26f,1) : new Color(.20f,.37f,.39f,.30f);
            stepIcons[i].localScale = Vector3.one * (i == Step ? 1 + .045f * Mathf.Sin(t*5) : 1);
        }
        previewPanel.alpha = Step == 0 ? .76f : 1;
        selection.enabled = Step >= 2;
        selection.color = new Color(1,.82f,.30f,.6f + .2f * Mathf.Sin(t*6));
        pointer.gameObject.SetActive(Step >= 2 && t < (eating ? 12.1f : 8.8f));
        pointer.anchoredPosition = pointerOrigin + (Step == 3 ? pointerActionOffset : Vector2.zero) + Vector2.up * (3 * Mathf.Sin(t*5));
        float clickAt = eating ? 10.3f : 7;
        pointer.localScale = Vector3.one * (t >= clickAt && t < clickAt + .25f ? .85f : 1);
        actor.sprite = idleFrames[(int)(elapsed*8 % idleFrames.Length)];
        actor.rectTransform.anchoredPosition = actorOrigin;
        actionIcon.rectTransform.anchoredPosition = actionOrigin;
        actionIcon.rectTransform.localRotation = Quaternion.identity;
        actionIcon.rectTransform.localScale = Vector3.one;
        result.rectTransform.anchoredPosition = resultOrigin;
        result.rectTransform.localScale = Vector3.one;
        if (fire && fireFrames.Length > 0)
            fire.sprite = fireFrames[(int)(elapsed*fireFramesPerSecond % fireFrames.Length)];
        if (cookingStepFire && fireFrames.Length > 0)
            cookingStepFire.sprite = fireFrames[(int)(elapsed*fireFramesPerSecond % fireFrames.Length)];
        if (emberFrames != null && emberFrames.Length > 0)
        {
            var ember = emberFrames[(int)(elapsed*fireFramesPerSecond % emberFrames.Length)];
            if (fireBase) fireBase.sprite = ember;
            if (cookingStepBase) cookingStepBase.sprite = ember;
        }

        if (eating) AnimateEating(t);
        else AnimateCrafting(t);

        int cue = eating
            ? (t >= 12.15f ? 3 : t >= 10.65f ? 2 : t >= 7.7f ? 1 : t >= 2.3f ? 0 : -1)
            : (t >= 8.85f ? 3 : t >= 7.35f ? 2 : t >= 4.5f ? 1 : t >= 2.2f ? 0 : -1);
        if (cue > previousCue)
        {
            previousCue = cue;
            CueCount++;
            Play(cue == 3 ? successSound : cue == 2 ? actionSound : selectSound);
        }
    }
    void AnimateCrafting(float t)
    {
        // Crafting happens through the recipe UI; it does not use a mining/tool swing.
        actionIcon.enabled = false;
        bool crafted = t >= 8.8f;
        result.enabled = crafted;
        if (crafted)
        {
            float pop = Mathf.Clamp01((t-8.8f)/.35f);
            result.rectTransform.localScale = Vector3.one * Mathf.Lerp(.3f,1,Mathf.Sin(pop*Mathf.PI*.5f));
            result.rectTransform.anchoredPosition = resultOrigin + Vector2.up * (4*Mathf.Sin(t*5));
        }
        statusLabel.text = Step == 0 ? "GATHER RESOURCES" : Step == 1 ? "[E] OPEN WORKBENCH" : Step == 2 ? "SELECT WOODEN FENCE" : crafted ? "+1 WOODEN FENCE" : "CLICK THE RECIPE";
        hintLabel.text = crafted ? "Crafted item goes into your inventory." : "Example: 3 Wood Logs = 1 Wooden Fence";
        quantityLabel.text = crafted ? "3 Wood used" : "3 Wood Logs";
    }
    void AnimateEating(float t)
    {
        float roast = Mathf.Clamp01((t-2.3f)/Mathf.Max(.1f,cookingSeconds));
        float progress = Mathf.Clamp01((t-10.4f)/1.65f);
        bool roasting = Step == 1;
        bool biting = t >= 10.4f && t < 12.1f;
        actionIcon.enabled = biting;
        actionIcon.color = cookedMeatTint;
        float cookingTime = Mathf.Max(0,t-2.3f);
        Color meatTint = Color.Lerp(rawMeatTint,cookedMeatTint,roast);
        if (roastingMeat)
        {
            roastingMeat.enabled = roasting;
            roastingMeat.color = meatTint;
            roastingMeat.rectTransform.anchoredPosition = Vector2.up * (.65f + Mathf.Sin(cookingTime*5)*.055f) * cookingVisualScale;
            roastingMeat.rectTransform.localRotation = Quaternion.Euler(0,0,Mathf.Sin(cookingTime*3)*12);
        }
        if (cookingStepMeat)
        {
            cookingStepMeat.color = meatTint;
            cookingStepMeat.rectTransform.anchoredPosition = Vector2.up * (.65f + (roasting ? Mathf.Sin(cookingTime*5)*.055f : 0)) * cookingStepScale;
            cookingStepMeat.rectTransform.localRotation = Quaternion.Euler(0,0,roasting ? Mathf.Sin(cookingTime*3)*12 : 0);
        }
        if (cookingSteam)
        {
            cookingSteam.gameObject.SetActive(roasting);
            if (roasting) cookingSteam.SetTime(cookingTime);
        }
        if (cookingMeter) cookingMeter.SetActive(roasting);
        if (cookingFill) cookingFill.fillAmount = roast;
        if (inventoryFood) inventoryFood.color = Color.Lerp(rawMeatTint,cookedMeatTint,roast);
        if (cookingLabel)
            cookingLabel.text = Step == 0 ? "COOK MEAT BEFORE EATING" : roasting
                ? (roast < 1 ? "[C] COOK AT A BURNING CAMPFIRE\n" + Mathf.CeilToInt(cookingSeconds*(1-roast)) + "s remaining" : "COOKED MEAT READY")
                : "Berries can be eaten without cooking.";
        if (biting)
        {
            float bite = Mathf.Abs(Mathf.Sin(progress*Mathf.PI*3));
            actionIcon.rectTransform.anchoredPosition = actionOrigin + new Vector2(-4*progress,bite*10);
            actionIcon.rectTransform.localRotation = Quaternion.Euler(0,0,Mathf.Sin(progress*Mathf.PI*6)*12);
            actionIcon.rectTransform.localScale = Vector3.one * Mathf.Lerp(1,.18f,progress);
            actor.rectTransform.anchoredPosition = actorOrigin + Vector2.up * bite*2;
        }
        result.enabled = Step == 0;
        result.rectTransform.anchoredPosition = resultOrigin + Vector2.up * (3*Mathf.Sin(t*4));
        hungerFill.fillAmount = Mathf.Clamp01((hungerBefore + hungerRestored*progress)/100);
        meterLabel.text = "HUNGER  " + Mathf.RoundToInt(hungerBefore+hungerRestored*progress) + " / 100";
        quantityLabel.text = (roast < 1 ? "Raw Meat" : "Cooked Meat") + "  x" + (t >= 12.1f ? "2" : "3");
        statusLabel.text = Step == 0 ? "FIND RAW MEAT OR BERRIES" : Step == 1 ? "[C] COOK THE MEAT FIRST" : Step == 2 ? "[TAB / I] SELECT COOKED MEAT" : t < 12.1f ? "CLICK USE FOOD" : "+" + hungerRestored.ToString("0") + " HUNGER";
        hintLabel.text = Step == 0 ? "Cook meat first. Berries need no cooking." : Step == 1 ? "Stay near a burning campfire for 5 seconds." : Step == 2 ? "Open inventory, then click the cooked meat." : t >= 12.1f ? "One food item consumed. Hunger restored." : "Hotbar food: select 1–5, then press F.";
    }
    void OnDisable() { if (audioSource) audioSource.Stop(); }
}
