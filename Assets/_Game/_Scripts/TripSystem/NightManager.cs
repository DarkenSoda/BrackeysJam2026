using UnityEngine;

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

    public void AddTimePenalty(float penaltySeconds)
    {
        gameClock.AdvanceTime(penaltySeconds);
    }

    private void HandleShiftEnd()
    {
        Debug.Log("Shift has ended. Transitioning to night mode.");
    }
}
