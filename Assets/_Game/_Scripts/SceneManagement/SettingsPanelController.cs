using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Audio settings panel — sits inside the MainMenu scene canvas as a
/// hidden panel that opens when the Settings button is pressed.
///
/// Attach to: The root GameObject of your Settings panel.
///
/// Inspector wiring:
///   - musicGauge    → GaugeNeedleDriver for the music volume gauge
///   - sfxGauge      → GaugeNeedleDriver for the SFX volume gauge
///   - closeButton   → "X" or "Back" button to hide the panel
///
/// How it works:
///   1. MainMenuController calls Show() on Settings button click.
///   2. Show() reads saved volumes from PlayerPrefs and calls
///      SetValueWithoutNotify() on each GaugeNeedleDriver (so the needle
///      snaps without firing the callback chain again).
///   3. When the player drags a gauge, its Slider fires onValueChanged →
///      this script saves to PlayerPrefs and updates AudioManager live.
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [Header("Volume Gauges")]
    [Tooltip("GaugeNeedleDriver controlling the SFX volume gauge.")]
    [SerializeField] private GaugeNeedleDriver sfxGauge;

    [Tooltip("GaugeNeedleDriver controlling the music volume gauge.")]
    [SerializeField] private GaugeNeedleDriver musicGauge;


    [Header("Navigation")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        // Panel starts hidden; MainMenuController calls Show().
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // Sync first so the needle positions are correct before listeners fire.
        SyncGaugesToSavedValues();

        // Subscribe AFTER syncing so the initial SetValueWithoutNotify doesn't
        // trigger a redundant PlayerPrefs write.
        HookGauge(musicGauge, OnMusicChanged);
        HookGauge(sfxGauge, OnSFXChanged);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);

        UnhookGauge(musicGauge, OnMusicChanged);
        UnhookGauge(sfxGauge, OnSFXChanged);
    }

    // ── Public API (called by MainMenuController) ─────────────────────────

    public void Show()
    {
        SyncGaugesToSavedValues();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── Value changed callbacks ───────────────────────────────────────────

    private void OnMusicChanged(float value)
    {
        PlayerPrefs.SetFloat(AudioManager.MusicVolumeKey, value);
        PlayerPrefs.Save();
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        PlayerPrefs.SetFloat(AudioManager.SFXVolumeKey, value);
        PlayerPrefs.Save();
        AudioManager.Instance?.SetSFXVolume(value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SyncGaugesToSavedValues()
    {
        // SetValueWithoutNotify moves the needle and sets the slider value
        // without triggering onValueChanged — so no double-write to PlayerPrefs.
        musicGauge?.SetValueWithoutNotify(PlayerPrefs.GetFloat(AudioManager.MusicVolumeKey, 1f));
        sfxGauge?.SetValueWithoutNotify(PlayerPrefs.GetFloat(AudioManager.SFXVolumeKey, 1f));
    }

    // GaugeNeedleDriver wraps a Slider internally; we get it to subscribe.
    private static void HookGauge(GaugeNeedleDriver gauge, UnityAction<float> callback)
    {
        if (gauge == null) return;
        var slider = gauge.GetComponentInChildren<Slider>(includeInactive: true);
        slider?.onValueChanged.AddListener(callback);
    }

    private static void UnhookGauge(GaugeNeedleDriver gauge, UnityAction<float> callback)
    {
        if (gauge == null) return;
        var slider = gauge.GetComponentInChildren<Slider>(includeInactive: true);
        slider?.onValueChanged.RemoveListener(callback);
    }
}
