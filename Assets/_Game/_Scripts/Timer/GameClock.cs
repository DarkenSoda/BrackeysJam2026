using System;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    private const int SECONDS_PER_DAY = 86400;

    [SerializeField] private ShiftConfig shiftConfig;

    private float currentRealSeconds;
    private int currentInGameSeconds;
    private int startSeconds;
    private int endSeconds;
    private bool shiftEnded;
    private float timeScale = 1f; // 1 = normal, <1 = slower

    public static GameClock Instance { get; private set; }
    public float NormalizedTime => Mathf.Clamp01(currentRealSeconds / shiftConfig.RealShiftSeconds);
    public bool ShiftEnded => shiftEnded;
    public string ClockLabel => FormatClock();
    
    public string ClockHours
    {
        get
        {
            if (shiftConfig == null) return "--";
            int hours = currentInGameSeconds / 3600;
            return ((hours % 12 == 0) ? 12 : (hours % 12)).ToString();
        }
    }

    public string ClockMinutes
    {
        get
        {
            if (shiftConfig == null) return "--";
            int minutes = (currentInGameSeconds % 3600) / 60;
            return $"{minutes:00}";
        }
    }

    public string ClockAmPm
    {
        get
        {
            if (shiftConfig == null) return "--";
            int hours = currentInGameSeconds / 3600;
            return (hours < 12) ? "AM" : "PM";
        }
    }

    public event Action OnShiftEnd;
    public event Action OnTimeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (shiftConfig == null) return;
        startSeconds = (shiftConfig.StartHour * 3600) + (shiftConfig.StartMinute * 60);
        endSeconds = (shiftConfig.EndHour * 3600) + (shiftConfig.EndMinute * 60);
        UpdateInGameTime();
    }

    private void Update()
    {
        if (shiftEnded) return;
        AdvanceTime(Time.deltaTime);
    }

    public void AdvanceTime(float realSeconds)
    {
        if (shiftEnded || shiftConfig == null) return;

        // Apply powerup time scale — <1.0 makes the clock tick slower
        currentRealSeconds = Mathf.Min(currentRealSeconds + realSeconds * timeScale, shiftConfig.RealShiftSeconds);
        UpdateInGameTime();
        OnTimeChanged?.Invoke();

        if (currentRealSeconds >= shiftConfig.RealShiftSeconds)
        {
            shiftEnded = true;
            OnShiftEnd?.Invoke();
        }
    }

    /// <summary>Set a time scale multiplier. 1 = normal, 0.33 = clock runs at 1/3 speed.</summary>
    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Clamp(scale, 0f, 10f);
    }

    private void UpdateInGameTime()
    {
        if (shiftConfig == null) return;

        int durationSec = (endSeconds <= startSeconds)
            ? (endSeconds + SECONDS_PER_DAY - startSeconds)
            : (endSeconds - startSeconds);

        currentInGameSeconds = (startSeconds + (int)(NormalizedTime * durationSec)) % SECONDS_PER_DAY;
    }

    private string FormatClock()
    {
        if (shiftConfig == null) return "--:--";

        int hours = currentInGameSeconds / 3600;
        int minutes = (currentInGameSeconds % 3600) / 60;
        int display12Hour = (hours % 12 == 0) ? 12 : (hours % 12);
        string ampm = (hours < 12) ? "AM" : "PM";

        return $"{display12Hour}:{minutes:00} {ampm}";
    }
}

