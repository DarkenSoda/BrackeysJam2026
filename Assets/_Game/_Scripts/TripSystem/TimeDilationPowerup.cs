using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the "Green Light" timing powerup.
/// Listens to driving/trip events to find idle cycles (the car is moving but no
/// NPC interaction is pending), then spawns the <see cref="PowerupPromptUI"/> prompt.
/// When the player nails the timing the GameClock is slowed for a configurable
/// duration. After it expires the clock snaps back to normal speed.
///
/// Attach to: Any persistent GameObject in the Driving scene (same object as
///            GameFlowController or NightManager works fine).
///
/// Inspector wiring:
///   prompt          → PowerupPromptUI in the UI canvas (starts inactive)
/// </summary>
public class TimeDilationPowerup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("The PowerupPromptUI component (its root object should start inactive).")]
    [SerializeField] private PowerupPromptUI prompt;

    [Header("Trigger key")]
    [Tooltip("The keyboard key the player must press.")]
    [SerializeField] private KeyCode activationKey = KeyCode.Space;

    [Header("Time dilation (Perfect hit)")]
    [Tooltip("Clock speed multiplier while the powerup is active. " +
             "0.33 = clock runs 3× slower (2 in-game seconds take 6 real seconds).")]
    [SerializeField] [Range(0.05f, 1f)] private float slowTimeScale = 0.33f;

    [Tooltip("How long (real seconds) the slow effect lasts on a Perfect hit.")]
    [SerializeField] private float perfectDuration = 8f;

    [Header("Time dilation (Good hit)")]
    [Tooltip("Clock speed on a Good (edge-of-zone) hit.")]
    [SerializeField] [Range(0.05f, 1f)] private float goodTimeScale = 0.55f;

    [Tooltip("How long (real seconds) the slow effect lasts on a Good hit.")]
    [SerializeField] private float goodDuration = 4f;

    [Header("Prompt animation speed")]
    [Tooltip("How fast the bar moves. Higher = harder. Scales with the time remaining " +
             "in the shift if 'scaleWithDifficulty' is on.")]
    [SerializeField] private float baseSliderSpeed = 0.7f;

    [Tooltip("If true, the slider speeds up as the shift progresses (harder late-game).")]
    [SerializeField] private bool scaleWithDifficulty = true;

    [Tooltip("Maximum slider speed multiplier at end of shift.")]
    [SerializeField] private float maxDifficultyMultiplier = 2.5f;

    [Header("Spawning")]
    [Tooltip("Minimum number of idle parallax cycles between two powerup opportunities.")]
    [SerializeField] private int cooldownCycles = 2;

    [Tooltip("Chance (0–1) that an eligible idle cycle will actually show the prompt. " +
             "1 = always show it, 0.5 = show it half the time.")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.65f;

    [Tooltip("The prompt will only appear when there are at least this many cycles " +
             "remaining in the current driving leg (avoids showing it on the last cycle " +
             "right before an NPC spawn).")]
    [SerializeField] private int minCyclesRemainingToSpawn = 2;

    // ── Private state ─────────────────────────────────────────────────────────
    private int _cyclesSincePowerup = 999; // start ready
    private bool _promptVisible;
    private bool _dilationActive;
    private Coroutine _dilationRoutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (TripDrivingBridge.Instance != null)
            TripDrivingBridge.Instance.OnEncounterDrivingStarted += OnDrivingStarted;

        if (BackgroundLoopController.Instance != null)
            BackgroundLoopController.Instance.OnLayerCycled += OnLayerCycled;

        if (prompt != null)
            prompt.OnResult += OnPromptResult;
    }

    private void OnDisable()
    {
        if (TripDrivingBridge.Instance != null)
            TripDrivingBridge.Instance.OnEncounterDrivingStarted -= OnDrivingStarted;

        if (BackgroundLoopController.Instance != null)
            BackgroundLoopController.Instance.OnLayerCycled -= OnLayerCycled;

        if (prompt != null)
            prompt.OnResult -= OnPromptResult;
    }

    private void Update()
    {
        if (_promptVisible && Input.GetKeyDown(activationKey))
        {
            prompt?.TryHit();
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnDrivingStarted()
    {
        // New leg started — increment cooldown counter
        _cyclesSincePowerup++;
    }

    private void OnLayerCycled(bool isPaceLayer)
    {
        if (!isPaceLayer) return;
        if (_promptVisible) return;          // prompt already showing
        if (_dilationActive) return;         // effect still running
        if (_cyclesSincePowerup < cooldownCycles) return;

        // Check how many cycles remain before the car stops for the next NPC
        int cyclesNeeded = TripDrivingBridge.Instance != null
            ? TripDrivingBridge.Instance.CyclesNeededForCurrentStop
            : 99;
        int cyclesToGo = cyclesNeeded - 1; // TripDrivingBridge tracks this internally

        if (cyclesToGo < minCyclesRemainingToSpawn) return;
        if (Random.value > spawnChance) return;

        ShowPrompt();
    }

    private void OnPromptResult(PowerupPromptUI.HitQuality quality)
    {
        _promptVisible = false;

        switch (quality)
        {
            case PowerupPromptUI.HitQuality.Perfect:
                ActivateDilation(slowTimeScale, perfectDuration);
                break;

            case PowerupPromptUI.HitQuality.Good:
                ActivateDilation(goodTimeScale, goodDuration);
                break;

            case PowerupPromptUI.HitQuality.Miss:
                // No effect — reset cooldown so the player isn't immediately
                // re-offered the powerup
                _cyclesSincePowerup = 0;
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowPrompt()
    {
        _cyclesSincePowerup = 0;
        _promptVisible = true;

        float speed = baseSliderSpeed;
        if (scaleWithDifficulty && GameClock.Instance != null)
        {
            float t = GameClock.Instance.NormalizedTime;
            speed *= Mathf.Lerp(1f, maxDifficultyMultiplier, t);
        }

        prompt?.Show(activationKey.ToString(), speed);
    }

    private void ActivateDilation(float scale, float duration)
    {
        if (_dilationRoutine != null)
            StopCoroutine(_dilationRoutine);

        _dilationRoutine = StartCoroutine(DilationRoutine(scale, duration));
    }

    private IEnumerator DilationRoutine(float scale, float duration)
    {
        _dilationActive = true;
        GameClock.Instance?.SetTimeScale(scale);

        // Optional: broadcast so AudioManager can pitch-shift or mute SFX
        // AudioManager.Instance?.SetMotorSpeed(DrivingController.Instance?.CurrentSpeed01 ?? 0f);

        yield return new WaitForSeconds(duration);

        GameClock.Instance?.SetTimeScale(1f);
        _dilationActive = false;
        _dilationRoutine = null;
    }
}
