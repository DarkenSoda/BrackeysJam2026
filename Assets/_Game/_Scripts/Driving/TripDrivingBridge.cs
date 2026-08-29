using UnityEngine;

public class TripDrivingBridge : MonoBehaviour
{
    public static TripDrivingBridge Instance { get; private set; }

    [Header("Visual buffer: how many pace-layer parallax cycles to drive before stopping for the next encounter")]
    [SerializeField] private int minCyclesBeforeStop = 1;
    [SerializeField] private int maxCyclesBeforeStop = 3;

    private TripState? waitingToStopForState;
    private int cyclesSeen;
    private int cyclesNeeded;

    /// <summary>The total number of pace-layer cycles the car will drive before stopping for the current encounter.</summary>
    public int CyclesNeededForCurrentStop => cyclesNeeded;

    public event System.Action OnEncounterDrivingStarted;
    public event System.Action OnEncounterAboutToStop;

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
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleTripStateChanged;
            HandleTripStateChanged(TripController.Instance.CurrentState);
        }
        else
        {
            Debug.LogWarning("TripDrivingBridge: TripController.Instance is null at Start(). Check script execution order.");
        }

        if (BackgroundLoopController.Instance != null)
        {
            BackgroundLoopController.Instance.OnLayerCycled += HandleLayerCycled;
        }
        else
        {
            Debug.LogWarning("TripDrivingBridge: BackgroundLoopController.Instance is null at Start(). Check script execution order.");
        }
    }

    private void OnDestroy()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleTripStateChanged;
        }

        if (BackgroundLoopController.Instance != null)
        {
            BackgroundLoopController.Instance.OnLayerCycled -= HandleLayerCycled;
        }
    }

    private void HandleTripStateChanged(TripState newState)
    {
        switch (newState)
        {
            case TripState.PICKUP:
            case TripState.ENCOUNTER:
                BeginDrivingBuffer(newState);
                break;

            case TripState.DROPOFF:
                waitingToStopForState = null;
                DrivingController.Instance?.ArriveAtStop();
                break;
        }
    }

    private void BeginDrivingBuffer(TripState forState)
    {
        waitingToStopForState = null;
        cyclesSeen = 0;
        cyclesNeeded = Random.Range(minCyclesBeforeStop, maxCyclesBeforeStop + 1); 
        waitingToStopForState = forState;
        DrivingController.Instance?.StartDriving();

        if (forState == TripState.ENCOUNTER)
            OnEncounterDrivingStarted?.Invoke();
    }

    private void HandleLayerCycled(bool isPaceLayer)
    {
        if (waitingToStopForState == null) return;
        if (!isPaceLayer) return;

        cyclesSeen++;
        if (cyclesSeen < cyclesNeeded) return;

        TripState waitedFor = waitingToStopForState.Value;
        waitingToStopForState = null;

        if (TripController.Instance == null) return;

        TripState current = TripController.Instance.CurrentState;
        if (current == waitedFor || current == TripState.DROPOFF)
        {
            if (waitedFor == TripState.ENCOUNTER)
                OnEncounterAboutToStop?.Invoke();

            DrivingController.Instance?.ArriveAtStop();
        }
    }
}