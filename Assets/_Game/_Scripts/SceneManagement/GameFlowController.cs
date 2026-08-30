using UnityEngine;
using System.Collections;

/// <summary>
/// Watches TripController for shift-end and shows the End Shift overlay panel
/// directly inside the Driving scene — no separate scene needed.
///
/// Attach to: Any persistent GameObject in the Driving scene
///            (same one as TripController works fine).
///
/// Inspector wiring:
///   - endShiftPanel       → The EndShiftPanelController in this scene's canvas
///   - delayBeforePanel    → Seconds to wait after "shift over" DROPOFF fires
///                           so the final "that's dawn, park it." line can land
/// </summary>
public class GameFlowController : MonoBehaviour
{
    [Header("End Shift")]
    [Tooltip("The EndShiftPanelController inside this scene's Canvas.")]
    [SerializeField] private EndShiftPanelController endShiftPanel;

    // [Tooltip("Seconds to wait after the shift ends before showing the results panel.")]
    // [SerializeField] private float delayBeforePanel = 2.5f;

    [Header("Audio")]
    [Tooltip("Ambient loop to play during the driving shift (optional).")]
    [SerializeField] private AudioClip ambientLoop;

    private bool _transitioning;

    private void Start()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleTripStateChanged;
        }
        else
        {
            Debug.LogWarning("[GameFlowController] TripController.Instance is null at Start(). " +
                             "Check script execution order.");
        }

        if (endShiftPanel == null)
            Debug.LogWarning("[GameFlowController] endShiftPanel is not assigned. " +
                             "Drag the EndShiftPanelController from the Canvas into this field.");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
            if (ambientLoop != null) AudioManager.Instance.PlayAmbient(ambientLoop);
        }
    }

    private void OnDestroy()
    {
        if (TripController.Instance != null)
            TripController.Instance.OnTripStateChanged -= HandleTripStateChanged;
    }

    private void HandleTripStateChanged(TripState state)
    {
        // We no longer trigger the panel here based on time.
        // DialogueHud will call TriggerEndShift() when the player clicks Continue on the final dropoff dialogue.
    }

    public void TriggerEndShift()
    {
        if (_transitioning) return;
        _transitioning = true;

        if (endShiftPanel != null)
            endShiftPanel.Show();
        else
            Debug.LogError("[GameFlowController] endShiftPanel is null — can't show results.");
    }
}
