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

    [Tooltip("Seconds to wait after the shift ends before showing the results panel.")]
    [SerializeField] private float delayBeforePanel = 2.5f;

    [Header("Audio")]
    [Tooltip("Background music to play during the driving shift (optional).")]
    [SerializeField] private AudioClip gameplayMusic;

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
            if (gameplayMusic != null) AudioManager.Instance.PlayMusic(gameplayMusic);
            if (ambientLoop != null)   AudioManager.Instance.PlayAmbient(ambientLoop);
        }
    }

    private void OnDestroy()
    {
        if (TripController.Instance != null)
            TripController.Instance.OnTripStateChanged -= HandleTripStateChanged;
    }

    private void HandleTripStateChanged(TripState state)
    {
        // ShiftOver is already true when ForceEndCurrentFare() triggers DROPOFF.
        if (_transitioning) return;
        if (state != TripState.DROPOFF) return;
        if (TripController.Instance == null || !TripController.Instance.ShiftOver) return;

        _transitioning = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShiftEnd();

        StartCoroutine(ShowPanelAfterDelay());
    }

    private IEnumerator ShowPanelAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforePanel);

        if (endShiftPanel != null)
            endShiftPanel.Show();
        else
            Debug.LogError("[GameFlowController] endShiftPanel is null — can't show results.");
    }
}
