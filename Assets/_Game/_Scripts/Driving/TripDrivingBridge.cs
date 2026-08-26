using UnityEngine;

/// <summary>
/// Bridges TripController's trip events to DrivingController, so neither script needs
/// to know about the other directly. Put this on the same GameObject as DrivingController
/// (or anywhere in the scene — it just needs both singletons to exist by Start()).
///
/// Why this exists instead of editing TripController:
/// TripController fires OnOutcomeResolved (per encounter) and OnTripStateChanged
/// (PICKUP/ENCOUNTER/DROPOFF). Neither alone tells us "drive to the next junction now."
/// This script combines them into StartDriving() / ArriveAtStop() calls.
/// </summary>
public class TripDrivingBridge : MonoBehaviour
{
    private void Start()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleTripStateChanged;
            TripController.Instance.OnOutcomeResolved += HandleOutcomeResolved;
        }
        else
        {
            Debug.LogWarning("TripDrivingBridge: TripController.Instance is null at Start(). Check script execution order.");
        }
    }

    private void OnDestroy()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleTripStateChanged;
            TripController.Instance.OnOutcomeResolved -= HandleOutcomeResolved;
        }
    }

    private void HandleTripStateChanged(TripState newState)
    {
        switch (newState)
        {
            case TripState.PICKUP:
                // Customer just got in — car should already be stopped here.
                DrivingController.Instance?.ArriveAtStop();
                break;

            case TripState.ENCOUNTER:
                // First encounter of the trip starting right after pickup -> pull away.
                DrivingController.Instance?.StartDriving();
                break;

            case TripState.DROPOFF:
                // Trip finished (either naturally or via ForceEndCurrentFare) -> stop.
                DrivingController.Instance?.ArriveAtStop();
                break;
        }
    }

    private void HandleOutcomeResolved(bool correct)
    {
        // Fires after every Follow/Ignore choice, BEFORE TripController decides whether
        // the trip is over. If it's the last encounter, OnTripStateChanged(DROPOFF) fires
        // immediately after this in the same call stack and will override us into a stop —
        // see TripController.ResolveChoice(): OnOutcomeResolved is invoked before SetState.
        // So it's safe to always drive here; DROPOFF will correct it right after if needed.
        DrivingController.Instance?.StartDriving();
    }
}