using UnityEngine;

/// <summary>
/// Bridges TripController's trip events to DrivingController, so neither script needs
/// to know about the other directly. TripController and DialogueHud are NOT editable —
/// this script only controls the CAR's visual pacing around their existing events.
///
/// Key design: TripController pre-rolls an entire trip's worth of tips up front
/// (see TripGenerator.Roll). There is no "generate one more encounter" concept to hook —
/// OnTripStateChanged(ENCOUNTER) fires the moment DialogueHud is about to show the next
/// tip's dialogue. So instead of stopping the car immediately on that event, we keep
/// driving for a random 1-3 parallax (Road layer) cycles first, THEN stop — giving the
/// "character spawns after a bit of driving" feel the GDD/task asks for, without touching
/// TripController or DialogueHud at all.
/// </summary>
public class TripDrivingBridge : MonoBehaviour
{
    [Header("Visual buffer: how many pace-layer parallax cycles to drive before stopping for the next encounter")]
    [SerializeField] private int minCyclesBeforeStop = 1;
    [SerializeField] private int maxCyclesBeforeStop = 3;

    private TripState? waitingToStopForState;
    private int cyclesSeen;
    private int cyclesNeeded;

    private void Start()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleTripStateChanged;
        }
        else
        {
            Debug.LogWarning("TripDrivingBridge: TripController.Instance is null at Start(). Check script execution order.");
        }

        if (BackgroundLoopController.Instance != null)
        {
            BackgroundLoopController.Instance.OnLayerCycled += HandleLayerCycled;
            Debug.Log("[TripDrivingBridge] Subscribed to BackgroundLoopController.OnLayerCycled successfully.");
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
                // Both a new pickup and a new tip mean "someone is about to appear."
                // Per spec: car should never show a stopped character instantly — it
                // should drive a random 1-3 pace-layer cycles first, THEN stop, and only
                // then does DialogueHud (via OnArrivedAtStop) reveal the content.
                BeginDrivingBuffer(newState);
                break;

            case TripState.DROPOFF:
                // Trip finished (either naturally or via ForceEndCurrentFare) -> stop now,
                // no buffer — this is an arrival, not a "someone new is coming" moment.
                waitingToStopForState = null;
                DrivingController.Instance?.ArriveAtStop();
                break;
        }
    }

    private void BeginDrivingBuffer(TripState forState)
    {
        // Always start a clean wait — if a previous wait was still in-flight (e.g. dialogue
        // was resolved faster than the car finished its cycles), this fully replaces it
        // rather than letting stale cycle counts bleed into the new one.
        waitingToStopForState = null;
        cyclesSeen = 0;
        cyclesNeeded = Random.Range(minCyclesBeforeStop, maxCyclesBeforeStop + 1); // max exclusive for ints
        waitingToStopForState = forState;
        DrivingController.Instance?.StartDriving();
    }

    private void HandleLayerCycled(bool isPaceLayer)
    {
        if (waitingToStopForState == null) return;
        if (!isPaceLayer) return;

        cyclesSeen++;
        if (cyclesSeen < cyclesNeeded) return;

        TripState waitedFor = waitingToStopForState.Value;
        waitingToStopForState = null;

        // Don't trust that the trip is still in the same state we started waiting for.
        // ResolveChoice/TripController can change state while we were counting cycles.
        // Only stop if TripController is still in that same state (or has since moved
        // to DROPOFF, which also wants the car stopped).
        if (TripController.Instance == null) return;

        TripState current = TripController.Instance.CurrentState;
        if (current == waitedFor || current == TripState.DROPOFF)
        {
            DrivingController.Instance?.ArriveAtStop();
        }
    }
}