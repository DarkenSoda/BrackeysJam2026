using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftConfig", menuName = "Taxi/Shift Config")]
public class ShiftConfig : ScriptableObject
{
    [field: Header("Real-World Timing")]

    [field: SerializeField]
    public float RealShiftSeconds { get; private set; } = 300f; // 5 minutes

    [field: Header("In-Game Shift Hours (24h format)")]

    [field: Range(0, 23)]
    [field: SerializeField]
    public int StartHour { get; private set; } = 20; // 8 PM

    [field: Range(0, 59)]
    [field: SerializeField]
    public int StartMinute { get; private set; }

    [field: Range(0, 23)]
    [field: SerializeField]
    public int EndHour { get; private set; } = 6;    // 6 AM

    [field: Range(0, 59)]
    [field: SerializeField]
    public int EndMinute { get; private set; }
}
