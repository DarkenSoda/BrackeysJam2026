using Alchemy.Inspector;
using UnityEngine;

public enum DrivingState
{
    Idle,       // stopped, waiting (pickup / at a stop for dialogue)
    Accelerating,
    Cruising,
    Braking
}

/// <summary>
/// Auto-drive state machine for the cutaway taxi. The car does NOT take player input —
/// it drives itself between stops. This script owns the taxi's Animator state and
/// broadcasts speed changes via OnSpeedChanged so BackgroundLoopController (and anything
/// else, e.g. AudioManager) can react without a direct reference.
///
/// Hook points from TripController (call these from a small listener component, or
/// subscribe directly in Start() below):
///   - TripController.OnTripStateChanged(ENCOUNTER)  -> StartDriving()
///   - TripController.OnTripStateChanged(DROPOFF)    -> ArriveAtStop()
///   - TripController.OnOutcomeResolved(bool correct) -> StartDriving() (next encounter)
/// </summary>
public class DrivingController : MonoBehaviour
{
    public static DrivingController Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Animator carAnimator;

    [Header("Speed curve (units are arbitrary — BackgroundLoop multiplies by this)")]
    [SerializeField] private float cruiseSpeed = 1f;
    [SerializeField] private float accelDuration = 0.6f;   // time to ramp Idle -> Cruise
    [SerializeField] private float brakeDuration = 0.5f;   // time to ramp Cruise -> Idle

    [Header("Animator trigger names")]
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string accelTrigger = "Accel";
    [SerializeField] private string brakeTrigger = "Brake";

    public DrivingState CurrentState { get; private set; } = DrivingState.Idle;
    public float CurrentSpeed01 { get; private set; } // 0 = stopped, 1 = full cruise speed

    // Anything (BackgroundLoopController, AudioManager...) subscribes to this.
    public event System.Action<float> OnSpeedChanged;      // 0..1, every frame it changes
    public event System.Action<DrivingState> OnStateChanged;
    public event System.Action OnArrivedAtStop;             // fired when braking finishes

    private float speedTarget;
    private float speedVelocity; // for SmoothDamp

    [Header("Debug (read-only)")]
    [SerializeField, ReadOnly] private DrivingState debugState;
    [SerializeField, ReadOnly] private float debugSpeed01;
    [SerializeField, ReadOnly] private bool debugIsMoving;

    /// <summary>True whenever the car is actually in motion (Accelerating or Cruising).
    /// Use this instead of checking CurrentState directly if you just need a yes/no answer.</summary>
    public bool IsMoving => CurrentState == DrivingState.Accelerating || CurrentState == DrivingState.Cruising;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (CurrentState == DrivingState.Accelerating || CurrentState == DrivingState.Braking)
        {
            float smoothTime = CurrentState == DrivingState.Accelerating ? accelDuration : brakeDuration;
            CurrentSpeed01 = Mathf.SmoothDamp(CurrentSpeed01, speedTarget, ref speedVelocity, smoothTime);
            debugSpeed01 = CurrentSpeed01;
            OnSpeedChanged?.Invoke(CurrentSpeed01);

            if (CurrentState == DrivingState.Accelerating && CurrentSpeed01 >= speedTarget - 0.01f)
            {
                SetState(DrivingState.Cruising);
            }
            else if (CurrentState == DrivingState.Braking && CurrentSpeed01 <= speedTarget + 0.01f)
            {
                CurrentSpeed01 = 0f;
                OnSpeedChanged?.Invoke(0f);
                SetState(DrivingState.Idle);
                OnArrivedAtStop?.Invoke();
            }
        }
    }

    /// <summary>Call when a new encounter/leg of the trip begins (car pulls away from a stop).</summary>
    public void StartDriving()
    {
        if (CurrentState == DrivingState.Cruising || CurrentState == DrivingState.Accelerating)
            return; // already going

        speedTarget = cruiseSpeed;
        SetState(DrivingState.Accelerating);
    }

    /// <summary>Call when the car should slow down and stop (arriving at a junction/dropoff).</summary>
    public void ArriveAtStop()
    {
        if (CurrentState == DrivingState.Idle || CurrentState == DrivingState.Braking)
            return; // already stopped, or already stopping — ignore redundant calls

        speedTarget = 0f;
        SetState(DrivingState.Braking);
    }

    private void SetState(DrivingState newState)
    {
        CurrentState = newState;
        debugState = newState;
        debugIsMoving = IsMoving;
        Debug.Log($"[DrivingController] State -> {newState} (IsMoving: {IsMoving})");

        switch (newState)
        {
            case DrivingState.Idle:
                carAnimator?.SetTrigger(idleTrigger);
                break;
            case DrivingState.Accelerating:
                carAnimator?.SetTrigger(accelTrigger);
                break;
            case DrivingState.Cruising:
                // usually same clip as Accel's held frame — no trigger needed,
                // but keeping it as its own state makes the speed logic clean.
                break;
            case DrivingState.Braking:
                carAnimator?.SetTrigger(brakeTrigger);
                break;
        }

        OnStateChanged?.Invoke(newState);
    }
}