using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class HomeMenuSpriteLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 9, phase;
    public bool flameBreathing;
    Image visual;
    void Awake() => visual = GetComponent<Image>();
    void Update()
    {
        double t = Time.unscaledTimeAsDouble + phase;
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[(int)(t * framesPerSecond % frames.Length)];
        if (flameBreathing)
            transform.localScale = new Vector3(1 + .045f*Mathf.Sin((float)t*7),1 + .09f*Mathf.Sin((float)t*9.3f),1);
    }
}
