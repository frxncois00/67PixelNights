using TMPro;
using UnityEngine;

/// <summary>Looping UI demonstration. Does not instantiate resources or a real player.</summary>
public sealed class GatherTutorialDemo : TutorialLessonDemo
{
    public override string LessonTitle => mining ? "MINING STONES" : "CHOPPING TREES";
    public HomeGatherTutorial tutorial;
    public UnityEngine.UI.Image actor, target, stump, reward, equippedIcon;
    public UnityEngine.UI.Image[] stepFrames, chips;
    public RectTransform[] stepIcons;
    public Sprite[] idleFrames, walkFrames, hitFrames;
    public TextMeshProUGUI statusLabel, hintLabel;
    public AudioSource audioSource;
    public AudioClip hitSound, pickupSound;
    public bool mining;
    Vector2 actorOrigin, targetOrigin, rewardOrigin;
    double started;
    int previousImpact = -1, previousCycle = -1;
    bool pickedUp, soundEnabled = true;
    bool initialized;
    public int Step { get; private set; }
    public int ImpactCount { get; private set; }

    void Awake() => Initialize();
    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        actorOrigin = actor.rectTransform.anchoredPosition;
        targetOrigin = target.rectTransform.anchoredPosition;
        rewardOrigin = reward.rectTransform.anchoredPosition;
    }
    void OnEnable()
    {
        Initialize(); started = Time.unscaledTimeAsDouble;
        previousCycle = previousImpact = -1; pickedUp = false; ImpactCount = 0;
    }
    public override void SetSoundEnabled(bool value)
    {
        soundEnabled = value;
        if (!value && audioSource) audioSource.Stop();
    }
    void Sound(AudioClip clip)
    {
        if (soundEnabled && clip && tutorial && audioSource)
        {
            audioSource.volume = tutorial.EffectVolume;
            audioSource.PlayOneShot(clip);
        }
    }
    void Update()
    {
        double elapsed = Time.unscaledTimeAsDouble - started;
        const float duration = 11.5f;
        int cycle = (int)(elapsed / duration);
        float t = (float)(elapsed % duration);
        if (cycle != previousCycle)
        {
            previousCycle = cycle; previousImpact = -1; pickedUp = false;
        }
        Step = t < 1.8f ? 0 : t < 3.5f ? 1 : t < 7.3f ? 2 : 3;
        for (int i=0;i<stepFrames.Length;i++)
        {
            stepFrames[i].color = i == Step ? new Color(1,.78f,.26f,1) : new Color(.20f,.37f,.39f,.30f);
            stepIcons[i].localScale = Vector3.one * (i == Step ? 1+.045f*Mathf.Sin(t*5) : 1);
        }
        float approach = Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,3.4f,t));
        actor.rectTransform.anchoredPosition = actorOrigin + Vector2.right * (approach*82);
        var frames = Step == 1 ? walkFrames : idleFrames;
        float hitProgress = 0;
        int hit = -1;
        if (t >= 3.5f && t < 7.1f)
        {
            float sequence = t - 3.5f;
            hit = Mathf.Min(2,(int)(sequence / 1.2f));
            hitProgress = sequence % 1.2f;
            if (hitProgress < .74f)
            {
                frames = hitFrames;
                actor.sprite = frames[Mathf.Min(frames.Length-1,(int)(hitProgress/.74f*frames.Length))];
            }
            else frames = idleFrames;
            if (hitProgress >= .43f && hit != previousImpact)
            {
                previousImpact = hit; ImpactCount++; Sound(hitSound);
            }
        }
        if (Step != 2 || hitProgress >= .74f)
            actor.sprite = frames[(int)(elapsed*8 % frames.Length)];
        float impactAge = hit >= 0 ? hitProgress-.43f : 10;
        float kick = impactAge >= 0 && impactAge < .30f ? Mathf.Sin(impactAge*65)*Mathf.Exp(-impactAge*12) : 0;
        bool broken = t >= 6.43f;
        target.enabled = !broken;
        target.rectTransform.anchoredPosition = targetOrigin + Vector2.right * kick*5;
        target.rectTransform.localRotation = Quaternion.Euler(0,0,mining ? 0 : -kick*1.5f);
        target.color = Color.Lerp(Color.white,new Color(1,1,.72f),Mathf.Abs(kick)*.38f);
        if (stump) stump.enabled = broken && !mining;
        bool collecting = t >= 8.3f;
        reward.enabled = broken && t < 9.2f;
        float collect = Mathf.Clamp01((t-8.3f)/.9f);
        Vector2 destination = actor.rectTransform.anchoredPosition + Vector2.up*45;
        reward.rectTransform.anchoredPosition = collecting
            ? Vector2.Lerp(rewardOrigin,destination,collect)+Vector2.up*Mathf.Sin(collect*Mathf.PI)*32
            : rewardOrigin+Vector2.up*(4*Mathf.Sin(t*5));
        reward.rectTransform.localScale = Vector3.one*(collecting ? Mathf.Lerp(1,.15f,collect) : 1);
        if (t >= 9.2f && !pickedUp) { pickedUp=true; Sound(pickupSound); }
        for (int i=0;i<chips.Length;i++)
        {
            float age = impactAge;
            chips[i].enabled = age >= 0 && age < .43f;
            if (!chips[i].enabled) continue;
            var direction = new Vector2((i-2)*35,70+i%3*20);
            chips[i].rectTransform.anchoredPosition = targetOrigin+new Vector2(-48,70)+direction*age+Vector2.down*200*age*age;
            chips[i].color = new Color(mining?.66f:.90f,mining?.79f:.70f,mining?.84f:.34f,1-age/.43f);
        }
        equippedIcon.rectTransform.localScale = Vector3.one*(Step==0?1+.10f*Mathf.Sin(t*6):1);
        string verb = mining ? "MINE" : "CHOP";
        string key = mining ? "[R]" : "[SPACE / LEFT CLICK]";
        if (Step==0) { statusLabel.text = mining ? "EQUIP YOUR PICKAXE" : "EQUIP YOUR AXE"; hintLabel.text = "Choose its hotbar slot with keys 1–5."; }
        else if (Step==1) { statusLabel.text = "MOVE CLOSE & FACE THE "+(mining?"STONE":"TRUNK"); hintLabel.text = "Use WASD or the arrow keys to approach."; }
        else if (!broken) { statusLabel.text = key+"  "+verb+"  "+Mathf.Clamp(previousImpact+1,0,3)+" / 3"; hintLabel.text = "Repeat the action until the resource breaks."; }
        else { statusLabel.text = t<9.2f?"[E]  COLLECT "+(mining?"STONE":"WOOD"):"+ "+(mining?"STONE":"WOOD")+" ADDED TO INVENTORY"; hintLabel.text="Stand near the drop and press E to pick it up."; }
    }
    void OnDisable() { if(audioSource) audioSource.Stop(); }
}
