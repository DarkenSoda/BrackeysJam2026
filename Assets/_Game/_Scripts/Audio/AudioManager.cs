using UnityEngine;

/// <summary>
/// Night Shift Taxi - AudioManager
///     HOW TO USE THIS:
///     AudioManager.Instance.PlayCorrectFollow();
///     AudioManager.Instance.PlayMusic(mainMenuTheme);
///     AudioManager.Instance.PlayAmbient(nightStreetLoop);
///     AudioManager.Instance.PlayNPC(drunkVoiceClip);
///
///     SETTINGS PANEL:
///     AudioManager.Instance.SetMusicVolume(0.8f);   // also saves to PlayerPrefs
///     AudioManager.Instance.SetSFXVolume(0.5f);
///     AudioManager.Instance.SetAmbientVolume(0.4f);
///
///     READ CURRENT VALUES (for syncing sliders):
///     AudioManager.Instance.MusicVolume / SFXVolume / AmbientVolume
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── PlayerPrefs keys shared with SettingsPanelController ─────────────
    public const string MusicVolumeKey = "Audio_MusicVolume";
    public const string SFXVolumeKey = "Audio_SFXVolume";
    public const string AmbientVolumeKey = "Audio_AmbientVolume";

    private AudioSource musicSource;
    private AudioSource musicSource2; // For gapless intro->loop transition
    private AudioSource sfxSource;
    private AudioSource ambientSource;

    [Header("Default Volumes (overridden by PlayerPrefs once player changes them)")]
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float defaultSFXVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float defaultAmbientVolume = 0.6f;

    [Header("SFX Pitch Variation")]
    [Tooltip("Every PlaySFX call picks a random pitch in this range so repeated sounds feel less robotic.")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Night Shift Taxi - Gameplay Clips")]
    [SerializeField] private AudioClip correctChoiceClip;
    [SerializeField] private AudioClip wrongChoiceClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip dropoffClip;

    [Header("UI SFX")]
    [SerializeField] private AudioClip menuClickClip;
    [SerializeField] private AudioClip backClickClip;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusicIntro;
    [SerializeField] private AudioClip gameplayMusicLoop;
    [SerializeField] private AudioClip outroMusic;
    [SerializeField] private float gameplayMusicDelay = 2f;
    private Coroutine gameplayMusicCoroutine;

    [Header("End Shift SFX")]
    [SerializeField] private AudioClip stampFirstClip;
    [SerializeField] private AudioClip stampSecondClip;
    [SerializeField] private AudioClip scoreCountingClip;

    [Header("Music Ducking")]
    [Tooltip("Multiplier applied to music volume when a dialogue happens.")]
    [Range(0f, 1f)][SerializeField] private float dialogueMusicMultiplier = 0.7f;
    private bool isDialogueDucking;

    [Header("Motor Sound")]
    [SerializeField] private AudioClip motorClip;
    [SerializeField] private float motorMinPitch = 0.5f;
    [SerializeField] private float motorMaxPitch = 1.5f;
    [SerializeField] private float motorMinVolume = 0.1f;
    [SerializeField] private float motorMaxVolume = 0.8f;
    [Tooltip("Overall volume multiplier for motor sound")]
    [Range(0f, 1f)][SerializeField] private float motorVolumeMultiplier = 0.8f;
    private AudioSource motorSource;

    // ── Runtime volumes (loaded from PlayerPrefs) ─────────────────────────
    private float musicVolume;
    private float sfxVolume;
    private float ambientVolume;

    // ── Read-only getters (for syncing Settings sliders when panel opens) ─
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;
    public float AmbientVolume => ambientVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (transform.parent != null)
            transform.SetParent(null);

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load persisted volumes; fall back to inspector defaults on first launch.
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, defaultSFXVolume);
        ambientVolume = PlayerPrefs.GetFloat(AmbientVolumeKey, defaultAmbientVolume);

        musicSource = CreateSource("MusicSource", loop: true);
        musicSource2 = CreateSource("MusicSource2", loop: true);
        sfxSource = CreateSource("SFXSource", loop: false);
        ambientSource = CreateSource("AmbientSource", loop: true);

        motorSource = CreateSource("MotorSource", loop: true);
        motorSource.clip = motorClip;
        motorSource.volume = 0f;
        if (motorClip != null) motorSource.Play();
    }

    private AudioSource CreateSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        return src;
    }

    // ── Volume setters (called by SettingsPanelController) ────────────────

    /// <summary>Sets music volume and persists it to PlayerPrefs immediately.</summary>
    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        UpdateMusicVolume();
        PlayerPrefs.Save();
    }

    public void SetDialogueDucking(bool isDucking)
    {
        isDialogueDucking = isDucking;
        UpdateMusicVolume();
    }

    private void UpdateMusicVolume()
    {
        float targetVol = musicVolume;
        if (isDialogueDucking)
            targetVol *= dialogueMusicMultiplier;

        if (musicSource != null) musicSource.volume = targetVol;
        if (musicSource2 != null) musicSource2.volume = targetVol;
    }

    /// <summary>Sets SFX volume and persists it to PlayerPrefs immediately.</summary>
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        // sfxSource uses PlayOneShot with the volume arg, so no live update needed.
        PlayerPrefs.SetFloat(SFXVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>Sets ambient volume and persists it to PlayerPrefs immediately.</summary>
    public void SetAmbientVolume(float value)
    {
        ambientVolume = Mathf.Clamp01(value);
        if (ambientSource != null) ambientSource.volume = ambientVolume;
        PlayerPrefs.SetFloat(AmbientVolumeKey, ambientVolume);
        PlayerPrefs.Save();
    }

    // ==================== Music ====================

    /// <summary>Plays looping music. No-ops if this exact clip is already playing.</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (gameplayMusicCoroutine != null)
        {
            StopCoroutine(gameplayMusicCoroutine);
            gameplayMusicCoroutine = null;
        }

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.Stop();
        if (musicSource2 != null) musicSource2.Stop();

        musicSource.clip = clip;

        float targetVol = musicVolume;
        if (isDialogueDucking) targetVol *= dialogueMusicMultiplier;
        musicSource.volume = targetVol;

        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (gameplayMusicCoroutine != null)
        {
            StopCoroutine(gameplayMusicCoroutine);
            gameplayMusicCoroutine = null;
        }
        if (musicSource != null) musicSource.Stop();
        if (musicSource2 != null) musicSource2.Stop();
    }

    public void PlayMainMenuMusic() => PlayMusic(mainMenuMusic);

    public void PlayGameplayMusic()
    {
        if (gameplayMusicCoroutine != null)
        {
            StopCoroutine(gameplayMusicCoroutine);
            gameplayMusicCoroutine = null;
        }

        gameplayMusicCoroutine = StartCoroutine(PlayMusicSequence(gameplayMusicIntro, gameplayMusicLoop, gameplayMusicDelay));
    }

    private System.Collections.IEnumerator PlayMusicSequence(AudioClip intro, AudioClip loop, float delay)
    {
        StopMusic();

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float targetVol = musicVolume;
        if (isDialogueDucking) targetVol *= dialogueMusicMultiplier;

        if (intro != null && loop != null)
        {
            // Setup first clip
            musicSource.clip = intro;
            musicSource.volume = targetVol;
            musicSource.loop = false;

            // Setup second clip
            musicSource2.clip = loop;
            musicSource2.volume = targetVol;
            musicSource2.loop = true;

            // Use DSP time to guarantee sample-accurate gapless transition
            double startTime = AudioSettings.dspTime + 0.1;
            musicSource.PlayScheduled(startTime);
            musicSource2.PlayScheduled(startTime + intro.length);
        }
        else if (intro != null)
        {
            PlayMusic(intro);
        }
        else if (loop != null)
        {
            PlayMusic(loop);
        }

        gameplayMusicCoroutine = null;
    }

    public void PlayOutroMusic() => PlayMusic(outroMusic);


    // ==================== SFX / Stingers ====================

    /// <summary>Plays a 2D one-shot SFX with a small random pitch variation.</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // ── Night Shift Taxi helpers ─────────────────────────────────────────
    public void PlayCorrectChoice() => PlaySFX(correctChoiceClip);
    public void PlayWrongChoice() => PlaySFX(wrongChoiceClip);
    public void PlayPickup() => PlaySFX(pickupClip);
    public void PlayDropoff() => PlaySFX(dropoffClip);

    public void PlayMenuClick() => PlaySFX(menuClickClip);
    public void PlayBackClick() => PlaySFX(backClickClip);

    public void PlayNPC(AudioClip clip, float volume)
    {
        if (clip != null) PlaySFX(clip, volume);
    }

    public void PlayScoreCounting()
    {
        if (scoreCountingClip != null && sfxSource != null)
        {
            // Usually score counting wants a looping source or a rapid fire SFX.
            // A simple PlayOneShot might not be enough if it needs to loop continuously.
            sfxSource.PlayOneShot(scoreCountingClip, sfxVolume);
        }
    }

    public void PlayStampDoubleSFX()
    {
        if (stampFirstClip != null) PlaySFX(stampFirstClip);
        if (stampSecondClip != null)
            StartCoroutine(PlayClipAfterDelay(stampSecondClip, stampFirstClip != null ? stampFirstClip.length : 0f));
    }

    private System.Collections.IEnumerator PlayClipAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySFX(clip);
    }

    // ==================== Motor ====================
    public void SetMotorSpeed(float speed01)
    {
        if (motorSource == null || motorClip == null) return;

        // Ensure motor is playing if not already
        if (!motorSource.isPlaying) motorSource.Play();

        motorSource.pitch = Mathf.Lerp(motorMinPitch, motorMaxPitch, speed01);

        // Scale motor volume by SFX volume so settings still apply, and apply multiplier
        float targetVol = Mathf.Lerp(motorMinVolume, motorMaxVolume, speed01);
        motorSource.volume = targetVol * sfxVolume * motorVolumeMultiplier;
    }

    public void StopMotor()
    {
        if (motorSource != null) motorSource.Stop();
    }

    // ==================== Ambient ====================

    /// <summary>Plays a looping ambient clip. No-ops if already playing.</summary>
    public void PlayAmbient(AudioClip clip)
    {
        if (ambientSource == null || clip == null) return;
        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        ambientSource.Stop();
        ambientSource.clip = clip;
        ambientSource.volume = ambientVolume;
        ambientSource.Play();
    }

    public void StopAmbient() => ambientSource?.Stop();
}