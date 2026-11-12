using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>Owns the loading-to-guide handoff and a reversible pause for the first playable frame.</summary>
public sealed class ForestGuideArrival : MonoBehaviour
{
    public HomeGatherTutorial guide;
    public GameObject loadingCover;
    public CanvasGroup appearance;
    public AudioSource clickSource;
    public AudioClip clickClip;
    public CanvasGroup loadingAppearance;
    public AudioSource tutorialMusic;
    [Min(0)] public float minimumLoadingSeconds = 3.6f;
    [Min(.1f)] public float forestRevealSeconds = 1.15f;
    [Min(.1f)] public float tutorialFadeInSeconds = 1.3f;
    [Min(.1f)] public float guideExitSeconds = 1.1f;
    [Min(.1f)] public float gameplayMusicFadeInSeconds = 1.5f;
    [Range(0,1)] public float tutorialMusicLevel = .75f;
    public float EffectsVolume { get; private set; } = .7f;
    public float MusicVolume { get; private set; } = .35f;
    public bool WaitingForGuide => ownsPause;
    public bool GuideReady { get; private set; }
    readonly List<Behaviour> pausedInputs = new List<Behaviour>();
    string forestScene;
    float previousTimeScale;
    bool ownsPause, ownsMusic, finishing;
    double loadingShownAt;
    Coroutine musicFade;

    public void Prepare(string sceneName, float effectsVolume, float musicVolume = -1)
    {
        forestScene = sceneName;
        EffectsVolume = Mathf.Clamp01(effectsVolume);
        MusicVolume = Mathf.Clamp01(musicVolume < 0 ? PlayerPrefs.GetFloat("PixelNights.Music",.35f) : musicVolume);
        ownsMusic = true; GuideReady = false;
        if (AudioManager.Instance)
        {
            AudioManager.Instance.SetMusicVolume(MusicVolume);
            AudioManager.Instance.SetGameplayMusicSuspended(true,0);
        }
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0;
        appearance.alpha = 1;
        loadingShownAt = Time.realtimeSinceStartupAsDouble;
        if (!loadingAppearance) loadingAppearance = loadingCover.GetComponent<CanvasGroup>();
        if (!loadingAppearance) loadingAppearance = loadingCover.AddComponent<CanvasGroup>();
        loadingAppearance.alpha = 1; loadingAppearance.blocksRaycasts = true;
        if (tutorialMusic) { tutorialMusic.volume = 0; tutorialMusic.Stop(); }
        loadingCover.SetActive(true);
        guide.gameObject.SetActive(false);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += Loaded;
    }
    void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != forestScene) return;
        SceneManager.sceneLoaded -= Loaded;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is PlayerMovement || behaviour is EquipmentController || behaviour is InventoryUI ||
                    behaviour is PlayerToolController || behaviour is PlayerMeleeCombat || behaviour is PlayerInteractionController ||
                    behaviour is FoodConsumption || behaviour is ItemDropController || behaviour is BuildingController ||
                    behaviour is DayNightCycle)
                    PauseInput(behaviour);
        StartCoroutine(ShowGuide());
    }
    void PauseInput(Behaviour behaviour)
    {
        if (!behaviour.enabled) return;
        pausedInputs.Add(behaviour);
        behaviour.enabled = false;
    }
    IEnumerator ShowGuide()
    {
        // Let the new scene finish initialization behind the opaque loading cover.
        yield return null;
        float remaining = minimumLoadingSeconds - (float)(Time.realtimeSinceStartupAsDouble-loadingShownAt);
        if (remaining > 0) yield return new WaitForSecondsRealtime(remaining);
        guide.OpenAfterArrival(this);
        if (tutorialMusic && tutorialMusic.clip)
        {
            tutorialMusic.Play();
            musicFade = StartCoroutine(FadeTutorialMusic(MusicVolume*tutorialMusicLevel,tutorialFadeInSeconds));
        }
        float duration = Mathf.Max(.1f,forestRevealSeconds);
        double revealStartedAt = Time.realtimeSinceStartupAsDouble;
        while (Time.realtimeSinceStartupAsDouble-revealStartedAt < duration)
        {
            float t = (float)(Time.realtimeSinceStartupAsDouble-revealStartedAt);
            loadingAppearance.alpha = 1-Mathf.SmoothStep(0,1,t/duration);
            yield return null;
        }
        loadingAppearance.alpha = 0; loadingAppearance.blocksRaycasts = false;
        loadingCover.SetActive(false);
        GuideReady = true;
    }
    IEnumerator FadeTutorialMusic(float target,float duration)
    {
        float from = tutorialMusic.volume;
        double startedAt = Time.realtimeSinceStartupAsDouble;
        while (Time.realtimeSinceStartupAsDouble-startedAt < duration)
        {
            float t = (float)(Time.realtimeSinceStartupAsDouble-startedAt);
            tutorialMusic.volume = Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,t/duration));
            yield return null;
        }
        tutorialMusic.volume = target; musicFade = null;
    }
    public void PlayClick()
    {
        if (!clickSource || !clickClip) return;
        clickSource.volume = EffectsVolume * .65f;
        clickSource.PlayOneShot(clickClip);
    }
    public void FinishGuide()
    {
        if (!GuideReady || !guide.CanStartGame || finishing) return;
        StartCoroutine(BeginExploring());
    }
    public void DismissGuide()
    {
        if (!GuideReady || !guide.IsOpen || finishing) return;
        StartCoroutine(BeginExploring());
    }
    IEnumerator BeginExploring()
    {
        finishing = true;
        GuideReady = false;
        guide.board.interactable = false;
        foreach (var lesson in guide.lessons) lesson.enabled = false;
        if (musicFade != null) { StopCoroutine(musicFade); musicFade = null; }
        float musicFrom = tutorialMusic ? tutorialMusic.volume : 0;
        float duration = Mathf.Max(.1f,guideExitSeconds);
        double exitStartedAt = Time.realtimeSinceStartupAsDouble;
        while (Time.realtimeSinceStartupAsDouble-exitStartedAt < duration)
        {
            float t = (float)(Time.realtimeSinceStartupAsDouble-exitStartedAt);
            float fade = 1-Mathf.SmoothStep(0,1,t/duration);
            appearance.alpha = fade;
            if (tutorialMusic) tutorialMusic.volume = musicFrom*fade;
            yield return null;
        }
        appearance.alpha = 0;
        if (tutorialMusic) { tutorialMusic.volume = 0; tutorialMusic.Stop(); }
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        ReleasePause();
        ResumeGameplayMusic();
        Destroy(gameObject);
    }
    void ReleasePause()
    {
        if (!ownsPause) return;
        ownsPause = false;
        foreach (var behaviour in pausedInputs) if (behaviour) behaviour.enabled = true;
        pausedInputs.Clear();
        Time.timeScale = previousTimeScale;
    }
    void ResumeGameplayMusic()
    {
        if (!ownsMusic) return;
        ownsMusic = false;
        if (!AudioManager.Instance) return;
        AudioManager.Instance.SetMusicVolume(MusicVolume);
        AudioManager.Instance.SetGameplayMusicSuspended(false,gameplayMusicFadeInSeconds);
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= Loaded;
        ReleasePause();
        ResumeGameplayMusic();
    }
}
