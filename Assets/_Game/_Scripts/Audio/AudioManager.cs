using UnityEngine;

/// <summary>
/// Night Shift Taxi - AudioManager
///     HOW TO USE THIS:
///     AudioManager.Instance.PlayCorrectFollow();
///     AudioManager.Instance.PlayMusic(mainMenuTheme);
///     AudioManager.Instance.PlayAmbient(nightStreetLoop);
///     AudioManager.Instance.PlayNPC(drunkVoiceClip);
///
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource ambientSource;

    [Header("Volumes")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.6f;

    [Header("SFX Pitch Variation")]
    [Tooltip("Every PlaySFX call picks a random pitch in this range so repeated sounds (like UI clicks) feel less robotic.")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Night Shift Taxi - Gameplay Clips")]
    [SerializeField] private AudioClip correctFollowClip;
    [SerializeField] private AudioClip wrongFollowClip;
    [SerializeField] private AudioClip ignoreSafeClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip dropoffClip;
    [SerializeField] private AudioClip obstacleHitClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        musicSource = CreateSource("MusicSource", loop: true);
        sfxSource = CreateSource("SFXSource", loop: false);
        ambientSource = CreateSource("AmbientSource", loop: true);
    }

    private AudioSource CreateSource(string name, bool loop)
    {
        GameObject sourceObject = new GameObject(name);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        return source;
    }

    // ==================== Music ====================
    /// Plays looping music. Stops whatever was playing first. If this exact
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource?.Stop();
    }

    // ==================== SFX / Stingers ====================
    /// Plays a 2D one-shot sound effect with a small random pitch variation.
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;

        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ==================== Night Shift Taxi helpers ====================

    public void PlayCorrectFollow() => PlaySFX(correctFollowClip);
    public void PlayWrongFollow() => PlaySFX(wrongFollowClip);
    public void PlayIgnoreSafe() => PlaySFX(ignoreSafeClip);
    public void PlayPickup() => PlaySFX(pickupClip);
    public void PlayDropoff() => PlaySFX(dropoffClip);
    public void PlayObstacleHit() => PlaySFX(obstacleHitClip);

    // ==================== NPC ====================
    public void PlayNPC(AudioClip clip)
    {
        PlaySFX(clip);
    }

    // ==================== Ambient ====================
    /// Plays a looping ambient background clip. If this exact clip is
    public void PlayAmbient(AudioClip clip)
    {
        if (ambientSource == null || clip == null) return;
        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        ambientSource.Stop();
        ambientSource.clip = clip;
        ambientSource.volume = ambientVolume;
        ambientSource.Play();
    }

    public void StopAmbient()
    {
        ambientSource?.Stop();
    }
}