using UnityEngine;
using UnityEngine.SceneManagement;

public enum TripState
{
    PICKUP,
    ENCOUNTER,
    DROPOFF
}

public class TripController : MonoBehaviour
{
    public static TripController Instance { get; private set; }

    [Header("Time costs (seconds)")]
    [SerializeField] private float correctFollowTime = 10f;
    [SerializeField] private float ignoreTime = 16f;
    [SerializeField] private float wrongFollowTime = 26f;

    [Header("Trip shape")]
    [SerializeField] private int encountersPerTrip = 4;

    public TripState CurrentState { get; private set; }
    public int EncounterIndex { get; private set; }

    // UI/audio/reputation hook onto these
    public event System.Action<TripState> OnTripStateChanged;
    public event System.Action<bool> OnOutcomeResolved; // true = correct choice

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public void StartTrip()
    {
        EncounterIndex = 0;
        SetState(TripState.PICKUP);
        // TODO: pull next CustomerData, populate scratchpad keywords here
        SetState(TripState.ENCOUNTER);
    }


    public void ResolveChoice(bool followed, bool tipWasTruth)
    {
        bool correct = followed == tipWasTruth;
        float timeCost = !followed ? ignoreTime : (correct ? correctFollowTime : wrongFollowTime);
        Debug.Log($"Choice resolved — followed:{followed}, correct:{correct}, timeCost:{timeCost}s");

        NightManager.Instance.AddTimePenalty(timeCost);
        OnOutcomeResolved?.Invoke(correct);

        EncounterIndex++;
        if (EncounterIndex >= encountersPerTrip)
        {
            SetState(TripState.DROPOFF);
            ScoreManager.Instance.AddFare();
        }
        // else: stay in ENCOUNTER, present the next tip
    }


    public void ForceEndCurrentFare()
    {
        SetState(TripState.DROPOFF);
    }

    [Header("Debug (read-only)")]
    [SerializeField] private TripState debugState;

    private void SetState(TripState newState)
    {
        CurrentState = newState;
        debugState = newState; // mirrors CurrentState so you can see it in Play mode
        OnTripStateChanged?.Invoke(newState);
    }
}