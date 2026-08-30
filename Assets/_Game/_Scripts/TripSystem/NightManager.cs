using UnityEngine;
using System;

public class NightManager : MonoBehaviour
{
    public static NightManager Instance { get; private set; }

    [SerializeField] private GameClock gameClock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        gameClock.OnShiftEnd += HandleShiftEnd;
    }

    private void OnDestroy()
    {
        gameClock.OnShiftEnd -= HandleShiftEnd;
    }

    /// <summary>Fired whenever a time penalty is applied. Arg = real seconds consumed.</summary>
    public static event Action<float> OnPenaltyApplied;

    public void AddTimePenalty(float penaltySeconds)
    {
        gameClock.AdvanceTime(penaltySeconds);
        OnPenaltyApplied?.Invoke(penaltySeconds);
    }

    private void HandleShiftEnd()
    {
        Debug.Log("Shift has ended. Transitioning to night mode.");
        TripController.Instance.ForceEndCurrentFare();
    }
}
