using UnityEngine;

/// <summary>A presentation-only lesson. Never invokes the player's gameplay actions.</summary>
public abstract class TutorialLessonDemo : MonoBehaviour
{
    public abstract string LessonTitle { get; }
    public abstract void SetSoundEnabled(bool value);
}
