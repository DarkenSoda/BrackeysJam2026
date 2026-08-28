using UnityEngine;

[CreateAssetMenu(fileName = "ShiftConfig", menuName = "Taxi/Shift Config")]
public class ShiftConfig : ScriptableObject
{
    [field: Header("Shift Duration (Real Seconds)")]
    [field: SerializeField]
    public float RealShiftSeconds { get; private set; } = 300f;

    [field: Header("In-Game Shift Hours (24h format)")]
    [field: Range(0, 23)]
    [field: SerializeField]
    public int StartHour { get; private set; } = 20;

    [field: Range(0, 59)]
    [field: SerializeField]
    public int StartMinute { get; private set; }

    [field: Range(0, 23)]
    [field: SerializeField]
    public int EndHour { get; private set; } = 8;

    [field: Range(0, 59)]
    [field: SerializeField]
    public int EndMinute { get; private set; }
}

