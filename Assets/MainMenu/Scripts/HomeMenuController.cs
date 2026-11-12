using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class HomeMenuController : MonoBehaviour
{
    public string gameplayScene = "SampleScene";
    public Button storyButton, lanButton, optionsButton, quitButton, closeOptionsButton, closeNoticeButton;
    public CanvasGroup intro, fade;
    public GameObject optionsPanel, noticePanel;
    public Text hint, noticeTitle, noticeBody, loadingLabel;
    public Slider music, effects;
    public Toggle fullscreen;
    public Image artwork;
    public AudioSource menuMusic, clickSound;
    public AudioClip clickClip;
    public ForestGuideArrival forestGuidePrefab;
    [Min(.1f)] public float forestFadeOutSeconds = 1.25f;
    Material animatedMaterial;
    float clickAge = 1;
    bool loading;
    GameObject restoreSelection;

    void Awake()
    {
        if (artwork && artwork.material)
        {
            animatedMaterial = new Material(artwork.material);
            artwork.material = animatedMaterial;
        }
    }

    IEnumerator Start()
    {
        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        optionsPanel.SetActive(false); noticePanel.SetActive(false);
        fade.alpha = 1; fade.blocksRaycasts = true;
        intro.alpha = 0; intro.interactable = false;
        storyButton.onClick.AddListener(PlayStory);
        lanButton.onClick.AddListener(ShowLAN);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(Quit);
        closeOptionsButton.onClick.AddListener(() => { ClickFeedback((RectTransform)closeOptionsButton.transform); CloseOptions(); });
        closeNoticeButton.onClick.AddListener(() => { ClickFeedback((RectTransform)closeNoticeButton.transform); CloseNotice(); });
        music.SetValueWithoutNotify(PlayerPrefs.GetFloat("PixelNights.Music", .35f));
        effects.SetValueWithoutNotify(PlayerPrefs.GetFloat("PixelNights.Effects", .7f));
        fullscreen.SetIsOnWithoutNotify(PlayerPrefs.HasKey("PixelNights.Fullscreen") ? PlayerPrefs.GetInt("PixelNights.Fullscreen") == 1 : Screen.fullScreen);
        ApplyAudio();
        if (menuMusic && !menuMusic.isPlaying) menuMusic.Play();
        music.onValueChanged.AddListener(SetMusic);
        effects.onValueChanged.AddListener(SetEffects);
        fullscreen.onValueChanged.AddListener(SetFullscreen);
        if (PlayerPrefs.HasKey("PixelNights.Fullscreen")) ApplyFullscreen(fullscreen.isOn);
        double introStartedAt = Time.realtimeSinceStartupAsDouble;
        while (Time.realtimeSinceStartupAsDouble-introStartedAt < 1)
        {
            float t = (float)(Time.realtimeSinceStartupAsDouble-introStartedAt);
            intro.alpha = Mathf.SmoothStep(0, 1, t);
            fade.alpha = 1 - Mathf.SmoothStep(0, 1, t);
            if (menuMusic) menuMusic.volume = music.value * intro.alpha;
            yield return null;
        }
        intro.alpha = 1; intro.interactable = true;
        if (menuMusic) menuMusic.volume = music.value;
        fade.alpha = 0; fade.blocksRaycasts = false;
        Select(storyButton.gameObject);
    }

    void Update()
    {
        clickAge += Time.unscaledDeltaTime;
        if (animatedMaterial) animatedMaterial.SetFloat("_ClickStrength", Mathf.Max(0, 1-clickAge/.28f));
        if (loading || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (optionsPanel.activeSelf) CloseOptions();
        else if (noticePanel.activeSelf) CloseNotice();
    }
    public void SetHint(string value) { if (hint && !loading) hint.text = value; }
    static void Select(GameObject go) { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(go); }
    void ApplyAudio()
    {
        if (menuMusic) menuMusic.volume = music.value * intro.alpha;
        if (clickSound) clickSound.volume = effects.value * .65f;
        if (AudioManager.Instance)
        {
            AudioManager.Instance.SetMusicVolume(music.value);
            AudioManager.Instance.SetGameplayMusicSuspended(true, 0);
            AudioManager.Instance.SetSFXVolume(effects.value);
        }
    }
    public void ClickFeedback(RectTransform button)
    {
        if (clickSound && clickClip) clickSound.PlayOneShot(clickClip);
        HomeMenuClickBurst.Spawn(button);
        clickAge = 0;
        if (!animatedMaterial) return;
        Vector2 center = button.anchoredPosition + new Vector2(1672/2f,941/2f);
        Vector2 half = button.rect.size*.5f;
        animatedMaterial.SetVector("_ClickRect", new Vector4((center.x-half.x)/1672,(center.y-half.y)/941,(center.x+half.x)/1672,(center.y+half.y)/941));
    }
    void SetMusic(float value) { PlayerPrefs.SetFloat("PixelNights.Music", value); ApplyAudio(); }
    void SetEffects(float value) { PlayerPrefs.SetFloat("PixelNights.Effects", value); ApplyAudio(); }
    static void ApplyFullscreen(bool value)
    {
#if !UNITY_EDITOR
        Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
    }
    void SetFullscreen(bool value) { PlayerPrefs.SetInt("PixelNights.Fullscreen", value ? 1 : 0); ApplyFullscreen(value); }
    public void OpenOptions()
    {
        if (loading) return;
        restoreSelection = optionsButton.gameObject;
        optionsPanel.SetActive(true); intro.interactable = false;
        SetHint(""); Select(music.gameObject);
    }
    public void CloseOptions()
    {
        PlayerPrefs.Save(); optionsPanel.SetActive(false); intro.interactable = true;
        Select(restoreSelection ? restoreSelection : optionsButton.gameObject);
    }
    public void ShowLAN()
    {
        if (loading) return;
        noticeTitle.text = "LAN MULTIPLAYER";
        noticeBody.text = "LAN play is not available in this build.\n\nChoose Story Mode to enter the forest and begin your survival adventure.";
        noticePanel.SetActive(true); intro.interactable = false; SetHint(""); Select(closeNoticeButton.gameObject);
    }
    public void CloseNotice() { noticePanel.SetActive(false); intro.interactable = true; Select(lanButton.gameObject); }
    public void PlayStory()
    {
        if (loading || optionsPanel.activeSelf || noticePanel.activeSelf) return;
        if (!forestGuidePrefab)
        {
            Debug.LogError("Story Mode requires the forest arrival guide.", this);
            return;
        }
        StartCoroutine(EnterStory());
    }
    IEnumerator EnterStory()
    {
        loading = true; intro.interactable = false; fade.blocksRaycasts = true;
        Select(null); PlayerPrefs.Save();
        float duration = Mathf.Max(.1f, forestFadeOutSeconds);
        double fadeStartedAt = Time.realtimeSinceStartupAsDouble;
        while (Time.realtimeSinceStartupAsDouble-fadeStartedAt < duration)
        {
            float t = (float)(Time.realtimeSinceStartupAsDouble-fadeStartedAt);
            fade.alpha = Mathf.SmoothStep(0, 1, t / duration);
            if (menuMusic) menuMusic.volume = music.value * (1-fade.alpha);
            yield return null;
        }
        fade.alpha = 1; loadingLabel.text = "ENTERING THE FOREST…";
        if (menuMusic) { menuMusic.volume = 0; menuMusic.Stop(); }
        yield return null;
        var arrival = Instantiate(forestGuidePrefab);
        arrival.Prepare(gameplayScene, effects.value, music.value);
        var operation = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
        while (!operation.isDone) yield return null;
    }
    public void Quit()
    {
        if (loading) return;
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void OnDestroy()
    {
        if (animatedMaterial) Destroy(animatedMaterial);
    }
}
