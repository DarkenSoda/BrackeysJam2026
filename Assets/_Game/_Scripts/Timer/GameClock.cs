using UnityEngine;

public class GameClock : MonoBehaviour
{
    [SerializeField] private ShiftConfig shiftConfig;

    [SerializeField] private float currentRealSeconds = 0f;
    private int currentInGameSeconds = 0;
    private const int SECONDS_PER_DAY = 86400;
    private int startSeconds;
    private int endSeconds;

    private bool shiftEnded = false;

    public float NormalizedTime => Mathf.Clamp01(currentRealSeconds / shiftConfig.RealShiftSeconds);

    public event System.Action OnShiftEnd;

    private void Start()
    {
        startSeconds = (shiftConfig.StartHour * 3600) + (shiftConfig.StartMinute * 60);
        endSeconds = (shiftConfig.EndHour * 3600) + (shiftConfig.EndMinute * 60);
    }

    private void Update()
    {
        if (shiftEnded) return;

        if (currentRealSeconds >= shiftConfig.RealShiftSeconds)
        {
            shiftEnded = true;
            OnShiftEnd?.Invoke();
            return;
        }

        AdvanceTime(Time.deltaTime);

        int durationSec = (endSeconds <= startSeconds)
            ? (endSeconds + SECONDS_PER_DAY - startSeconds)
            : (endSeconds - startSeconds);

        currentInGameSeconds = (startSeconds + (int)(NormalizedTime * durationSec)) % SECONDS_PER_DAY;
    }

    public void AdvanceTime(float realSeconds)
    {
        currentRealSeconds = Mathf.Min(currentRealSeconds + realSeconds, shiftConfig.RealShiftSeconds);
    }
}