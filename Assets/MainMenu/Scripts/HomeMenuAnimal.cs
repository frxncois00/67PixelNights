using UnityEngine;
using UnityEngine.UI;

/// <summary>A decorative UI animal; independent of wildlife, damage and navigation.</summary>
[RequireComponent(typeof(Image))]
public sealed class HomeMenuAnimal : MonoBehaviour
{
    public Sprite[] idleFrames, walkFrames;
    public Vector2 origin;
    public float distance = 45, walkSeconds = 4, restSeconds = 2.5f, phase;
    public bool artworkFacesLeft;
    public float framesPerSecond = 8;
    Image visual;
    RectTransform rect;
    public bool Walking { get; private set; }

    void Awake() { visual = GetComponent<Image>(); rect = (RectTransform)transform; }
    void Update()
    {
        double time = Time.unscaledTimeAsDouble + phase;
        float halfCycle = walkSeconds + restSeconds;
        float cycle = (float)(time % (halfCycle * 2));
        bool movingRight = cycle < halfCycle;
        float segment = movingRight ? cycle : cycle - halfCycle;
        Walking = segment < walkSeconds;
        float travel = Mathf.Clamp01(segment / Mathf.Max(.1f,walkSeconds));
        float x = Mathf.Lerp(-distance*.5f,distance*.5f,movingRight ? travel : 1-travel);
        rect.anchoredPosition = origin + new Vector2(Mathf.Round(x),0);
        rect.localScale = new Vector3(movingRight == artworkFacesLeft ? -1 : 1,1,1);
        var frames = Walking ? walkFrames : idleFrames;
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[(int)(time * framesPerSecond % frames.Length)];
    }
}
