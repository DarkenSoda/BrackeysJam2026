using UnityEngine;

public class GameClock : MonoBehaviour
{
    [SerializeField] private ShiftConfig shiftConfig;

    private float currentRealSeconds = 0f;
    private int currentInGameSeconds = 0;
    private const int SECONDS_PER_DAY = 86400;
    private int startSeconds;
    private int endSeconds;

    public float NormalizedTime => Mathf.Clamp01(currentRealSeconds / shiftConfig.RealShiftSeconds);

    public event System.Action OnShiftEnd;

    private void Start()
    {
        startSeconds = (shiftConfig.StartHour * 3600) + (shiftConfig.StartMinute * 60);
        endSeconds = (shiftConfig.EndHour * 3600) + (shiftConfig.EndMinute * 60);
    }

    private void Update()
    {
        if (currentRealSeconds >= shiftConfig.RealShiftSeconds)
        {
            OnShiftEnd?.Invoke();
            return;
        }

        AdvanceTime(Time.deltaTime);

        // Handle crossing midnight (e.g., 20:00 -> 06:00)
        int durationSec = (endSeconds <= startSeconds)
            ? (endSeconds + SECONDS_PER_DAY - startSeconds)
            : (endSeconds - startSeconds);

        // Calculate current in-game second of the day
        currentInGameSeconds = (startSeconds + (int)(NormalizedTime * durationSec)) % SECONDS_PER_DAY;

        // Extract Digital Time
        // int hours = (int)(currentInGameSeconds / 3600);
        // int minutes = (int)((currentInGameSeconds % 3600) / 60);

        // int display12Hour = (hours % 12 == 0) ? 12 : hours % 12;
    }

    public void AdvanceTime(float realSeconds)
    {
        currentRealSeconds = Mathf.Min(currentRealSeconds + realSeconds, shiftConfig.RealShiftSeconds);
    }
}