using UnityEngine;
using UnityEngine.SceneManagement;

// Starts automatically; an optional scene instance can override the default clips.
[DefaultExecutionOrder(-200)]
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [Header("Looping music")]
    [SerializeField] AudioClip dayMusic, nightMusic;
    [SerializeField, Min(.01f)] float crossfadeSeconds = 2.5f;
    [Header("Hit sounds")]
    [SerializeField] AudioClip woodHit, stoneHit, enemyHit;
    [Header("Optional sounds")]
    [SerializeField] AudioClip playerHurt, pickup;
    [Header("Campfire sounds")]
    [SerializeField] AudioClip campfireRefuel, campfireUpgrade;
    [Header("Volumes")]
    [SerializeField, Range(0, 1)] float musicVolume = .35f;
    [SerializeField, Range(0, 1)] float sfxVolume = .7f;
    AudioSource daySource, nightSource, sfxSource;
    DayNightCycle cycle;
    float blend, targetBlend;
    bool started;
    bool musicSuspended;
    float musicGain = 1, gainFrom = 1, gainTarget = 1, gainElapsed, gainDuration;
    double gainStartedAt;
    public bool GameplayMusicSuspended => musicSuspended;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance) return;
        new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Real bundled clips make empty Inspector fields usable immediately.
        if (!dayMusic) dayMusic = Resources.Load<AudioClip>("Audio/DayMusic");
        if (!nightMusic) nightMusic = Resources.Load<AudioClip>("Audio/NightMusic");
        if (!woodHit) woodHit = Resources.Load<AudioClip>("Audio/WoodHit");
        if (!stoneHit) stoneHit = Resources.Load<AudioClip>("Audio/StoneHit");
        if (!enemyHit) enemyHit = Resources.Load<AudioClip>("Audio/EnemyHit");
        if (!playerHurt) playerHurt = Resources.Load<AudioClip>("Audio/PlayerHurt");
        if (!pickup) pickup = Resources.Load<AudioClip>("Audio/Pickup");
        if (!campfireRefuel) campfireRefuel = Resources.Load<AudioClip>("Audio/CampfireRefuel");
        if (!campfireUpgrade) campfireUpgrade = Resources.Load<AudioClip>("Audio/CampfireUpgrade");
        daySource = CreateSource(true);
        nightSource = CreateSource(true);
        sfxSource = CreateSource(false);
        daySource.clip = dayMusic;
        nightSource.clip = nightMusic;
    }

    AudioSource CreateSource(bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0;
        source.volume = 0;
        return source;
    }

    void OnEnable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded += SceneLoaded;
        BindCycle();
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode) => BindCycle();
    void BindCycle()
    {
        UnbindCycle();
        cycle = FindAnyObjectByType<DayNightCycle>();
        if (cycle) cycle.OnPhaseChanged += SyncPhase;
        if (cycle) SyncPhase();
        else StopMusicSources();
    }
    void UnbindCycle()
    {
        if (cycle) cycle.OnPhaseChanged -= SyncPhase;
        cycle = null;
    }
    void SyncPhase()
    {
        if (cycle && cycle.IsNighttime) PlayNightMusic(); else PlayDayMusic();
    }
    public void PlayDayMusic() => SelectMusic(false);
    public void PlayNightMusic() => SelectMusic(true);
    void SelectMusic(bool night)
    {
        targetBlend = night ? 1 : 0;
        if (started || musicSuspended || !cycle) return;
        started = true;
        blend = targetBlend;
        // Both loops keep their position; changing phase only changes their mix.
        ApplyVolumes();
        if (daySource.clip) daySource.Play();
        if (nightSource.clip) nightSource.Play();
    }
    void Update()
    {
        // Reversing a transition continues from the current mix without a jump.
        blend = Mathf.MoveTowards(blend, targetBlend, Time.unscaledDeltaTime / Mathf.Max(.01f, crossfadeSeconds));
        if (gainElapsed < gainDuration)
        {
            gainElapsed = Mathf.Min(gainDuration,(float)(Time.realtimeSinceStartupAsDouble-gainStartedAt));
            musicGain = Mathf.Lerp(gainFrom, gainTarget, Mathf.SmoothStep(0,1,gainElapsed/gainDuration));
        }
        if (musicSuspended && musicGain <= .0001f && started) StopMusicSources();
        ApplyVolumes();
    }
    void ApplyVolumes()
    {
        daySource.volume = (1 - blend) * musicVolume * musicGain;
        nightSource.volume = blend * musicVolume * musicGain;
        sfxSource.volume = sfxVolume;
    }
    public void SetMusicVolume(float value) { musicVolume = Mathf.Clamp01(value); ApplyVolumes(); }
    public void SetGameplayMusicSuspended(bool suspended, float fadeSeconds = 1.2f)
    {
        musicSuspended = suspended;
        gainFrom = musicGain; gainTarget = suspended ? 0 : 1;
        gainDuration = Mathf.Max(0,fadeSeconds); gainElapsed = 0;
        gainStartedAt = Time.realtimeSinceStartupAsDouble;
        if (gainDuration == 0) musicGain = gainTarget;
        if (suspended && musicGain <= .0001f) StopMusicSources();
        if (!suspended && cycle) SyncPhase();
        ApplyVolumes();
    }
    void StopMusicSources()
    {
        if (daySource) daySource.Stop();
        if (nightSource) nightSource.Stop();
        started = false;
    }
    public void SetSFXVolume(float value) { sfxVolume = Mathf.Clamp01(value); ApplyVolumes(); }
    public void PlaySFX(AudioClip clip)
    {
        if (!clip || !sfxSource || AudioListener.pause) return;
        sfxSource.pitch = Random.Range(.9f, 1.1f);
        sfxSource.PlayOneShot(clip);
    }
    public void PlayWoodHit() => PlaySFX(woodHit);
    public void PlayStoneHit() => PlaySFX(stoneHit);
    public void PlayEnemyHit() => PlaySFX(enemyHit);
    public void PlayPlayerHurt() => PlaySFX(playerHurt);
    public void PlayPickup() => PlaySFX(pickup);
    public void PlayCampfireRefuel() => PlaySFX(campfireRefuel);
    public void PlayCampfireUpgrade() => PlaySFX(campfireUpgrade);
    void OnDisable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded;
        UnbindCycle();
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
