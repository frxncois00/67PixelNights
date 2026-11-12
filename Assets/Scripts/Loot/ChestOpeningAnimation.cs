using System.Collections;
using UnityEngine;

/// <summary>One-shot lid movement and pixel sparkles. Loot stays owned by LootChest.</summary>
public class ChestOpeningAnimation : MonoBehaviour
{
    public SpriteRenderer body, lidVisual, interior;
    public Sprite closedBodySprite, openBodySprite;
    public bool hideLidWhenClosed;
    public Vector3 closedLidPosition;
    public Vector3 openLidOffset = new Vector3(0, .14f, 0);
    public float closedLidAngle, openLidAngle = -8;
    [Min(.1f)] public float duration = .65f;
    public SpriteRenderer[] sparkles;
    public Vector3 sparkleOrigin = new Vector3(0, .2f, 0);
    public Color sparkleColor = new Color(1, .82f, .25f);

    Coroutine opening;
    bool opened, empty;
    public bool IsPlaying => opening != null;

    public void SetState(bool isOpen, bool isEmpty)
    {
        StopAnimation();
        opened = isOpen;
        empty = isEmpty;
        ApplyPose(opened ? 1 : 0);
    }

    public void PlayOpen(bool isEmpty)
    {
        empty = isEmpty;
        if (opened) return;
        opened = true;
        if (!isActiveAndEnabled) { ApplyPose(1); return; }
        opening = StartCoroutine(AnimateOpening());
    }

    public void SetEmpty(bool isEmpty)
    {
        empty = isEmpty;
        if (!IsPlaying) ApplyPose(opened ? 1 : 0);
    }

    IEnumerator AnimateOpening()
    {
        float elapsed = 0;
        float length = Mathf.Max(.1f, duration);
        while (elapsed < length)
        {
            float t = elapsed / length;
            float eased = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .7f));
            ApplyPose(eased);
            if (lidVisual)
                lidVisual.transform.localPosition += Vector3.up * (.035f * Mathf.Sin(t * Mathf.PI));
            if (sparkles != null)
                for (int i = 0; i < sparkles.Length; i++)
                {
                    var spark = sparkles[i];
                    if (!spark) continue;
                    float progress = Mathf.Clamp01((t - .15f) / .85f);
                    float spread = (i - (sparkles.Length - 1) * .5f) * .12f;
                    spark.enabled = t > .15f;
                    spark.transform.localPosition = sparkleOrigin + new Vector3(
                        spread * progress, progress * (.4f + (i % 2) * .12f), 0);
                    var color = sparkleColor;
                    color.a = Mathf.Sin(progress * Mathf.PI);
                    spark.color = color;
                }
            elapsed += Time.deltaTime;
            yield return null;
        }
        opening = null;
        HideSparkles();
        ApplyPose(1);
    }

    void ApplyPose(float amount)
    {
        var tint = empty && amount >= 1 ? new Color(.55f, .55f, .55f) : Color.white;
        if (body)
        {
            var sprite = opened ? openBodySprite : closedBodySprite;
            if (sprite) body.sprite = sprite;
            body.color = tint;
        }
        if (lidVisual)
        {
            lidVisual.enabled = opened || !hideLidWhenClosed;
            lidVisual.color = tint;
            lidVisual.transform.localPosition = closedLidPosition + openLidOffset * amount;
            lidVisual.transform.localRotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(closedLidAngle, openLidAngle, amount));
        }
        if (interior) interior.enabled = opened;
    }

    void HideSparkles()
    {
        if (sparkles != null)
            foreach (var spark in sparkles) if (spark) spark.enabled = false;
    }

    void StopAnimation()
    {
        if (opening != null) StopCoroutine(opening);
        opening = null;
        HideSparkles();
    }

    void OnDisable()
    {
        StopAnimation();
        ApplyPose(opened ? 1 : 0);
    }
}
