using System.Collections;
using TMPro;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Displays a red "-X hr" floating popup near the clock whenever the player
/// makes a wrong choice and a time penalty is applied.
///
/// Attach to: A child of your HUD Canvas.
///
/// Inspector wiring:
///   - penaltyText   → A TMP Text in your Canvas, positioned near the clock.
///                     It should start with alpha = 0 (invisible).
///   - shiftConfig   → The ShiftConfig ScriptableObject (to compute in-game hours lost).
///   - floatDistance → How many pixels the text floats upward.
///   - animDuration  → Total animation time in seconds.
/// </summary>
public class PenaltyPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI penaltyText;

    [Header("Shift Config (for real→in-game conversion)")]
    [SerializeField] private ShiftConfig shiftConfig;

    [Header("Animation")]
    [Tooltip("How far upward the text floats (UI units / pixels).")]
    [SerializeField] private float floatDistance = 40f;

    [Tooltip("Total duration of the fade-in + hold + float-up animation.")]
    [SerializeField] private float animDuration = 1.6f;

    [Tooltip("Color of the penalty text.")]
    [SerializeField] private Color penaltyColor = new Color(1f, 0.22f, 0.22f, 1f);

    private RectTransform rectTransform;
    private Vector2 anchoredOrigin;
    private MotionHandle posHandle;
    private MotionHandle alphaHandle;
    private Coroutine activeRoutine;

    private void Awake()
    {
        if (penaltyText == null)
        {
            Debug.LogWarning("[PenaltyPopupUI] penaltyText is not assigned.", this);
            return;
        }

        rectTransform = penaltyText.GetComponent<RectTransform>();
        anchoredOrigin = rectTransform.anchoredPosition;

        penaltyText.color = new Color(penaltyColor.r, penaltyColor.g, penaltyColor.b, 0f);
        penaltyText.text = "";
    }

    private void OnEnable()
    {
        NightManager.OnPenaltyApplied += HandlePenalty;
    }

    private void OnDisable()
    {
        NightManager.OnPenaltyApplied -= HandlePenalty;
    }

    private void HandlePenalty(float realSeconds)
    {
        if (penaltyText == null || shiftConfig == null) return;

        // Convert real seconds → in-game hours lost
        // ShiftConfig defines how many real seconds map to the full in-game shift span
        float inGameShiftHours = GetInGameShiftHours();
        float realShiftSeconds = shiftConfig.RealShiftSeconds;
        float inGameHoursLost = (realSeconds / realShiftSeconds) * inGameShiftHours;

        // Round to nearest 0.5 for clean display (e.g. "-1 hr", "-0.5 hr")
        // Or just show whole/half hours
        string label;
        float rounded = Mathf.Round(inGameHoursLost * 2f) / 2f;
        if (rounded < 0.25f)
            label = $"-{Mathf.RoundToInt(realSeconds / 60)} min"; // fallback to minutes if very small
        else
            label = rounded == Mathf.Floor(rounded)
                ? $"-{Mathf.RoundToInt(rounded)} hr"
                : $"-{rounded:0.#} hr";

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayAnimation(label));
    }

    private IEnumerator PlayAnimation(string label)
    {
        // Cancel any ongoing tweens
        if (posHandle.IsActive()) posHandle.Cancel();
        if (alphaHandle.IsActive()) alphaHandle.Cancel();

        // Reset position
        rectTransform.anchoredPosition = anchoredOrigin;
        penaltyText.text = label;

        float fadeInTime = animDuration * 0.2f;
        float holdTime   = animDuration * 0.35f;
        float fadeOutTime = animDuration * 0.45f;

        // --- Fade In ---
        alphaHandle = LMotion.Create(0f, 1f, fadeInTime)
            .WithEase(Ease.OutQuad)
            .Bind(v =>
            {
                var c = penaltyText.color;
                penaltyText.color = new Color(c.r, c.g, c.b, v);
            });

        yield return new WaitForSeconds(fadeInTime);

        // --- Float Up + Fade Out ---
        Vector2 targetPos = anchoredOrigin + Vector2.up * floatDistance;

        posHandle = LMotion.Create(anchoredOrigin, targetPos, fadeOutTime + holdTime)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(rectTransform);

        yield return new WaitForSeconds(holdTime);

        alphaHandle = LMotion.Create(1f, 0f, fadeOutTime)
            .WithEase(Ease.InQuad)
            .Bind(v =>
            {
                var c = penaltyText.color;
                penaltyText.color = new Color(c.r, c.g, c.b, v);
            });

        yield return new WaitForSeconds(fadeOutTime);

        penaltyText.text = "";
        rectTransform.anchoredPosition = anchoredOrigin;
    }

    private float GetInGameShiftHours()
    {
        if (shiftConfig == null) return 8f;

        int startSec = shiftConfig.StartHour * 3600 + shiftConfig.StartMinute * 60;
        int endSec   = shiftConfig.EndHour   * 3600 + shiftConfig.EndMinute   * 60;
        int durationSec = (endSec <= startSec)
            ? (endSec + 86400 - startSec)
            : (endSec - startSec);

        return durationSec / 3600f;
    }
}
