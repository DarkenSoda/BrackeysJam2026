using UnityEngine;

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [SerializeField] private ShiftConfig shiftConfig;

    [SerializeField] private float hoursElapsed;

    private int shiftHours = 12;
    private bool shiftEnded;

    public float NormalizedTime => shiftHours <= 0 ? 1f : Mathf.Clamp01(hoursElapsed / shiftHours);
    public bool ShiftEnded => shiftEnded;
    public string ClockLabel => FormatClock();

    public event System.Action OnShiftEnd;
    public event System.Action OnTimeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RecalcShiftHours();
    }

    private void RecalcShiftHours()
    {
        if (shiftConfig == null)
        {
            shiftHours = 12;
            return;
        }

        int start = shiftConfig.StartHour * 60 + shiftConfig.StartMinute;
        int end = shiftConfig.EndHour * 60 + shiftConfig.EndMinute;
        int durationMin = end <= start ? (end + 24 * 60 - start) : (end - start);
        shiftHours = Mathf.Max(1, durationMin / 60);
    }

    public void AdvanceTime(float hours)
    {
        if (shiftEnded) return;

        RecalcShiftHours();
        hoursElapsed = Mathf.Min(hoursElapsed + hours, shiftHours);
        OnTimeChanged?.Invoke();

        if (hoursElapsed >= shiftHours)
        {
            shiftEnded = true;
            OnShiftEnd?.Invoke();
        }
    }

    string FormatClock()
    {
        if (shiftConfig == null)
            return "--:--";

        int startMin = shiftConfig.StartHour * 60 + shiftConfig.StartMinute;
        int nowMin = (startMin + Mathf.RoundToInt(hoursElapsed * 60)) % (24 * 60);
        int h24 = nowMin / 60;
        int m = nowMin % 60;
        int h12 = h24 % 12;
        if (h12 == 0) h12 = 12;
        string ampm = h24 < 12 ? "AM" : "PM";
        return $"{h12}:{m:00} {ampm}";
    }
}
