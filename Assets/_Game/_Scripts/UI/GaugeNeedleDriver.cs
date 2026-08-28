using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a gauge needle based on a Unity UI Slider value.
///
/// The Slider itself is kept fully transparent so the player only sees
/// the gauge art + needle — but the Slider's invisible rect is still
/// interactable and catches touch/mouse input normally.
///
/// Attach to: Any GameObject in the same Canvas — one per gauge.
///
/// Inspector wiring:
///   - slider    → the transparent Slider component
///   - needle    → the needle's Transform (pivot must already be set
///                 to the needle's rotation centre in the Rect Transform)
///   - minAngle  → Z rotation (degrees) when slider value = 0  (default  52)
///   - maxAngle  → Z rotation (degrees) when slider value = 1  (default -128)
///
/// Usage from another script (e.g. SettingsPanelController):
///   gaugeNeedleDriver.Value = 0.75f;   // sets slider + moves needle
///   float v = gaugeNeedleDriver.Value; // reads current slider value
///
/// Reuse:
///   Drop one GaugeNeedleDriver on each gauge. Configure minAngle/maxAngle
///   independently — every gauge can have a different arc.
/// </summary>
[ExecuteAlways]
public class GaugeNeedleDriver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transparent-but-interactable Slider that captures input.")]
    [SerializeField] private Slider slider;

    [SerializeField] private Image needleFill;

    [Tooltip("The needle Transform whose Z rotation will be driven.")]
    [SerializeField] private Transform needle;

    [Header("Needle Arc")]
    [Tooltip("Z rotation (degrees) when the slider value is 0.")]
    [SerializeField] private float minAngle = 52f;

    [Tooltip("Z rotation (degrees) when the slider value is 1.")]
    [SerializeField] private float maxAngle = -128f;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the slider value (0–1).
    /// Setting this also immediately moves the needle and updates the Slider.
    /// </summary>
    public float Value
    {
        get => slider != null ? slider.value : 0f;
        set
        {
            if (slider != null)
                slider.value = Mathf.Clamp01(value); // triggers OnValueChanged → needle update
        }
    }

    /// <summary>
    /// Sets the slider and needle position WITHOUT firing onValueChanged.
    /// Use this when syncing saved values into the gauge at panel-open time.
    /// </summary>
    public void SetValueWithoutNotify(float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        ApplyNeedleRotation(value);
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        if (slider == null)
            Debug.LogWarning($"[GaugeNeedleDriver] ({name}) — 'slider' is not assigned.", this);

        if (needle == null)
            Debug.LogWarning($"[GaugeNeedleDriver] ({name}) — 'needle' is not assigned.", this);
    }

    private void OnEnable()
    {
        if (slider == null) return;
        slider.onValueChanged.AddListener(OnSliderValueChanged);

        // Snap needle to the current slider value immediately on enable.
        ApplyNeedleRotation(slider.value);
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void OnSliderValueChanged(float value)
    {
        ApplyNeedleRotation(value);
    }

    /// <summary>
    /// Linearly interpolates between minAngle and maxAngle and applies the
    /// result as a local Z rotation on the needle Transform.
    /// </summary>
    // Internal so SettingsPanelController (and OnValidate) can call it directly.
    internal void ApplyNeedleRotation(float value)
    {
        if (needle == null) return;

        float angle = Mathf.Lerp(minAngle, maxAngle, value);
        needle.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (needleFill != null)
            needleFill.fillAmount = value;
    }

#if UNITY_EDITOR
    // ── Editor preview ────────────────────────────────────────────────────
    // Update runs continuously in the Editor thanks to [ExecuteAlways],
    // so dragging the Slider component's value instantly updates the needle.
    private void OnValidate()
    {
        if (!Application.isPlaying && slider != null)
        {
            ApplyNeedleRotation(slider.value);
        }
    }
#endif
}
