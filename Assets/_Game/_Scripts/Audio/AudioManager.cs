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
    [SerializeField] private AudioClip dialogueMuffleClip;
    [SerializeField] private AudioClip shiftEndClip;

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
        sfxSource = CreateSource("SFXSource", loop: false);
        ambientSource = CreateSource("AmbientSource", loop: true);
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
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
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
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource?.Stop();

    // ==================== SFX / Stingers ====================

    /// <summary>Plays a 2D one-shot SFX with a small random pitch variation.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ── Night Shift Taxi helpers ─────────────────────────────────────────
    public void PlayCorrectChoice() => PlaySFX(correctChoiceClip);
    public void PlayWrongChoice() => PlaySFX(wrongChoiceClip);
    public void PlayPickup() => PlaySFX(pickupClip);
    public void PlayDropoff() => PlaySFX(dropoffClip);
    public void PlayDialogueMuffle() => PlaySFX(dialogueMuffleClip);
    public void PlayShiftEnd() => PlaySFX(shiftEndClip);

    // ==================== NPC ====================
    public void PlayNPC(AudioClip clip) => PlaySFX(clip);

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