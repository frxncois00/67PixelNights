using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Sequential lessons displayed on forest arrival while gameplay is paused.</summary>
public sealed class HomeGatherTutorial : MonoBehaviour
{
    public CanvasGroup board;
    public TutorialLessonDemo[] lessons;
    public UnityEngine.UI.Button nextButton, backButton, closeButton, soundButton;
    public TextMeshProUGUI pageLabel, nextLabel, soundLabel;
    public UnityEngine.UI.Image[] pageMarkers;
    ForestGuideArrival arrival;
    Coroutine entrance;
    bool bound, transitioning, soundEnabled = true;
    public int Page { get; private set; }
    public bool IsOpen => gameObject.activeSelf;
    bool NavigationReady => !arrival || arrival.GuideReady;
    public bool CanStartGame => IsOpen && NavigationReady && lessons.Length > 0 && Page == lessons.Length - 1 && !transitioning;

    void Bind()
    {
        if (bound) return;
        bound = true;
        nextButton.onClick.AddListener(Next);
        backButton.onClick.AddListener(Back);
        closeButton.onClick.AddListener(Close);
        soundButton.onClick.AddListener(ToggleSound);
    }
    public void OpenAfterArrival(ForestGuideArrival owner)
    {
        arrival = owner;
        Bind();
        gameObject.SetActive(true);
        SetPage(0);
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }
    void Click(UnityEngine.UI.Button button)
    {
        if (arrival) arrival.PlayClick();
        HomeMenuClickBurst.Spawn((RectTransform)button.transform);
    }
    public void Next()
    {
        if (!IsOpen || !NavigationReady || transitioning) return;
        Click(nextButton);
        if (Page < lessons.Length - 1) SetPage(Page + 1);
        else arrival.FinishGuide();
    }
    public void Back()
    {
        if (!IsOpen || !NavigationReady || transitioning || Page == 0) return;
        Click(backButton);
        SetPage(Page - 1);
    }
    void SetPage(int index)
    {
        Page = index;
        for (int i = 0; i < lessons.Length; i++) lessons[i].gameObject.SetActive(i == index);
        pageLabel.text = $"{index + 1:00} / {lessons.Length:00}   {lessons[index].LessonTitle}";
        nextLabel.text = index < lessons.Length - 1 ? lessons[index + 1].LessonTitle : "START GAME";
        backButton.interactable = index > 0;
        for (int i = 0; i < pageMarkers.Length; i++)
            pageMarkers[i].color = i == index ? new Color(1,.84f,.37f) : new Color(.33f,.55f,.58f);
        if (entrance != null) StopCoroutine(entrance);
        entrance = StartCoroutine(Reveal());
    }
    IEnumerator Reveal()
    {
        transitioning = true;
        board.interactable = false;
        for (float age = 0; age < .22f; age += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0,1,age/.22f);
            board.alpha = t;
            yield return null;
        }
        board.alpha = 1;
        board.interactable = true; transitioning = false; entrance = null;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
    }
    public void Close()
    {
        if (!IsOpen || !NavigationReady) return;
        Click(closeButton);
        if (arrival) arrival.DismissGuide();
    }
    void OnDisable()
    {
        StopAllCoroutines(); entrance = null; transitioning = false;
    }
    void ToggleSound()
    {
        Click(soundButton);
        soundEnabled = !soundEnabled;
        soundLabel.text = soundEnabled ? "DEMO SOUND: ON" : "DEMO SOUND: OFF";
        foreach (var lesson in lessons) lesson.SetSoundEnabled(soundEnabled);
    }
    public float EffectVolume => arrival ? arrival.EffectsVolume * .42f : 0;
}
