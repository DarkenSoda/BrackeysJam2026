using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the sliding-bar "Green Light" powerup prompt.
///
/// Visual structure expected in the Canvas hierarchy:
///   PowerupPromptRoot  (this component lives here)
///     ├─ TrackImage      (the full horizontal bar background)
///     ├─ SweetZoneImage  (the highlighted "hit zone" inside the track)
///     └─ SliderImage     (the moving indicator that slides left→right)
///
/// Attach to: A child GameObject under the main Driving UI canvas.
/// The whole root starts inactive — TimeDilationPowerup activates it.
/// </summary>
public class PowerupPromptUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Rect References")]
    [Tooltip("The RectTransform the slider moves across (the 'track').")]
    [SerializeField] private RectTransform track;

    [Tooltip("The moving indicator.")]
    [SerializeField] private RectTransform slider;

    [Tooltip("Highlight image that shows the 'sweet zone'.")]
    [SerializeField] private RectTransform sweetZone;

    [Header("Prompt key display (optional)")]
    [Tooltip("Assign a TMP_Text or Image that shows which key to press.")]
    [SerializeField] private TMPro.TextMeshProUGUI keyLabel;

    [Header("Timing")]
    [Tooltip("How many seconds the bar takes to travel from left edge to right edge.")]
    [SerializeField] private float travelDuration = 1.4f;

    [Header("Sweet zone (0–1 of track width, 0 = left edge)")]
    [SerializeField] [Range(0f, 1f)] private float sweetZoneCenter = 0.5f;
    [SerializeField] [Range(0f, 0.5f)] private float sweetZoneHalfWidth = 0.15f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color perfectColor = new Color(0.3f, 1f, 0.4f, 1f);
    [SerializeField] private Color missColor = new Color(1f, 0.25f, 0.2f, 1f);

    // ── Events ────────────────────────────────────────────────────────────────
    public enum HitQuality { Perfect, Good, Miss }
    public event System.Action<HitQuality> OnResult;

    // ── Private state ─────────────────────────────────────────────────────────
    private float _elapsed;
    private bool _active;
    private bool _resolved;
    private Image _sliderImage;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show the prompt and start the animation. Call from TimeDilationPowerup.</summary>
    public void Show(string keyName, float speed)
    {
        travelDuration = 1f / speed;
        _elapsed = 0f;
        _active = true;
        _resolved = false;

        if (keyLabel != null) keyLabel.text = keyName;
        if (_sliderImage == null && slider != null) _sliderImage = slider.GetComponent<Image>();
        if (_sliderImage != null) _sliderImage.color = normalColor;

        // Position sweet zone on the track
        if (track != null && sweetZone != null)
        {
            float trackWidth = track.rect.width;
            float zoneWidth = sweetZoneHalfWidth * 2f * trackWidth;
            sweetZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, zoneWidth);
            float centerOffset = (sweetZoneCenter - 0.5f) * trackWidth;
            sweetZone.anchoredPosition = new Vector2(centerOffset, sweetZone.anchoredPosition.y);
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_active || _resolved) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / travelDuration);

        // Move slider from left edge to right edge of track
        if (track != null && slider != null)
        {
            float halfTrack = track.rect.width * 0.5f;
            slider.anchoredPosition = new Vector2(Mathf.Lerp(-halfTrack, halfTrack, t), slider.anchoredPosition.y);
        }

        // Auto-miss when bar exits without input
        if (t >= 1f)
            Resolve(HitQuality.Miss);
    }

    // Called by TimeDilationPowerup when the input key is pressed
    public void TryHit()
    {
        if (!_active || _resolved) return;

        float t = Mathf.Clamp01(_elapsed / travelDuration);
        float distance = Mathf.Abs(t - sweetZoneCenter);

        HitQuality quality;
        if (distance <= sweetZoneHalfWidth * 0.4f)
            quality = HitQuality.Perfect;
        else if (distance <= sweetZoneHalfWidth)
            quality = HitQuality.Good;
        else
            quality = HitQuality.Miss;

        Resolve(quality);
    }

    private void Resolve(HitQuality quality)
    {
        _resolved = true;
        _active = false;

        if (_sliderImage != null)
            _sliderImage.color = quality == HitQuality.Miss ? missColor : perfectColor;

        OnResult?.Invoke(quality);
        StartCoroutine(HideAfterDelay(0.4f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
