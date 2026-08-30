using System;
using System.Collections;
using LitMotion;
using TMPro;
using UGSLeaderboardKit;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-shift results overlay — lives INSIDE the Driving scene canvas.
/// GameFlowController calls Show() after the shift ends.
///
/// Sequence on Show():
///   1. Score  counts up  (LitMotion tween)
///   2. Fares  counts up
///   3. Good Reads counts up
///   4. Wrong Turns counts up
///   5. Stamp slams down
///   Score is submitted silently in the background while all this plays.
///
/// Inspector wiring:
///   Stamp
///     stampTransform   → the stamp RectTransform
///     stampStartScale  → scale it flies in from  (e.g. 3.0)
///     stampEndScale    → resting scale            (e.g. 1.0)
///     stampDuration    → seconds to slam down     (e.g. 0.35)
///     stampCurve       → AnimationCurve for the slam feel
///
///   Counter Tween
///     countDuration    → seconds each number counts up  (e.g. 1.2)
///     countEase        → LitMotion Ease for the counter (e.g. OutQuad)
///
///   Stats Labels (TMP)
///     shiftScoreLabel  → SHIFT SCORE  (shown first)
///     faresLabel       → NIGHT FARES  (shown second)
///     goodReadsLabel   → GOOD READS   (shown third)
///     wrongTurnsLabel  → WRONG TURNS  (shown fourth)
///
///   Buttons
///     playAgainButton  → reloads the Driving scene
///     mainMenuButton   → goes to Main Menu
/// </summary>
public class EndShiftPanelController : MonoBehaviour
{
    // ── UGS ──────────────────────────────────────────────────────────────
    [Header("UGS")]
    [SerializeField] private string leaderboardId = "NightShiftTaxi";

    // ── Stamp animation ───────────────────────────────────────────────────
    [Header("Stamp Animation")]
    [Tooltip("Stamp shown when the player passes the shift.")]
    [SerializeField] private RectTransform passStampTransform;

    [Tooltip("Stamp shown when the player fails the shift.")]
    [SerializeField] private RectTransform failStampTransform;

    [Tooltip("Minimum fares completed to show the PASSED stamp. Below this → FAILED.")]
    [SerializeField] private int minFaresToPass = 3;

    [Tooltip("Uniform scale the stamp starts at before slamming down.")]
    [SerializeField] private float stampStartScale = 3f;

    [Tooltip("Uniform scale the stamp settles at after landing.")]
    [SerializeField] private float stampEndScale = 1f;

    [Tooltip("Seconds the slam animation takes.")]
    [SerializeField] private float stampDuration = 0.35f;

    [Tooltip("Curve shaping the stamp slam. EaseIn = fast slam, satisfying thud.")]
    [SerializeField] private AnimationCurve stampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ── Counter tween ─────────────────────────────────────────────────────
    [Header("Counter Tween")]
    [Tooltip("Seconds each number takes to count up from 0 to its value.")]
    [SerializeField] private float countDuration = 1.2f;

    [Tooltip("Easing applied to each counter tween.")]
    [SerializeField] private Ease countEase = Ease.OutQuad;

    // ── Stats labels ──────────────────────────────────────────────────────
    [Header("Stats Labels")]
    [Tooltip("SHIFT SCORE — tweens first.")]
    [SerializeField] private TMP_Text shiftScoreLabel;

    [Tooltip("NIGHT FARES — tweens second.")]
    [SerializeField] private TMP_Text faresLabel;

    [Tooltip("GOOD READS — tweens third.")]
    [SerializeField] private TMP_Text goodReadsLabel;

    [Tooltip("WRONG TURNS — tweens fourth.")]
    [SerializeField] private TMP_Text wrongTurnsLabel;

    // ── Buttons ───────────────────────────────────────────────────────────
    [Header("Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    // ── Internal ──────────────────────────────────────────────────────────
    private Coroutine    _sequenceCoroutine;
    private RectTransform _activeStamp;   // whichever stamp is used this run

    private void Awake() => gameObject.SetActive(false);

    private void Start()
    {
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (mainMenuButton  != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDestroy()
    {
        if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        if (mainMenuButton  != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Show()
    {
        // Grab stats before activating (ScoreManager may reset on scene load).
        int fares      = ScoreManager.Instance != null ? ScoreManager.Instance.Fares      : 0;
        int goodReads  = ScoreManager.Instance != null ? ScoreManager.Instance.GoodReads  : 0;
        int wrongTurns = ScoreManager.Instance != null ? ScoreManager.Instance.WrongTurns : 0;
        int shiftScore = Mathf.Max(0, (fares * 100) + (goodReads * 25) - (wrongTurns * 10));

        // Reset all labels to "0" before showing.
        SetLabel(shiftScoreLabel, 0);
        SetLabel(faresLabel,      0);
        SetLabel(goodReadsLabel,  0);
        SetLabel(wrongTurnsLabel, 0);

        // Choose which stamp to show and hide the other.
        bool passed = fares >= minFaresToPass;
        _activeStamp = passed ? passStampTransform : failStampTransform;

        // Hide both, then reset the active one to start scale.
        if (passStampTransform != null) passStampTransform.gameObject.SetActive(false);
        if (failStampTransform != null) failStampTransform.gameObject.SetActive(false);
        if (_activeStamp != null)
        {
            // Keep it hidden during the number counting phase
            _activeStamp.gameObject.SetActive(false);
            _activeStamp.localScale = Vector3.one * stampStartScale;
        }

        gameObject.SetActive(true);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayOutroMusic();

        // Submit score silently in the background while the sequence plays.
        SubmitScoreAsync(fares);

        // Start the counter → stamp sequence.
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(ShowSequence(shiftScore, fares, goodReads, wrongTurns));
    }

    // ── Sequence ──────────────────────────────────────────────────────────

    private IEnumerator ShowSequence(int shiftScore, int fares, int goodReads, int wrongTurns)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayScoreCounting();

        float durationPerLabel = countDuration / 4f;

        // 1. Score
        yield return TweenLabel(shiftScoreLabel, shiftScore, durationPerLabel);
        // 2. Fares
        yield return TweenLabel(faresLabel, fares, durationPerLabel);
        // 3. Good Reads
        yield return TweenLabel(goodReadsLabel, goodReads, durationPerLabel);
        // 4. Wrong Turns
        yield return TweenLabel(wrongTurnsLabel, wrongTurns, durationPerLabel);

        // 5. Stamp slams down.
        yield return PlayStampAnimation();

        _sequenceCoroutine = null;
    }

    /// <summary>
    /// Tweens a TMP label from 0 to <paramref name="target"/> using LitMotion,
    /// then yields until the tween is complete before returning control to the sequence.
    /// </summary>
    private IEnumerator TweenLabel(TMP_Text label, int target, float duration)
    {
        if (label == null) yield break;

        label.text = "0";

        if (target == 0) yield break;

        bool done = false;

        LMotion.Create(0, target, duration)
            .WithEase(countEase)
            .WithOnComplete(() => done = true)
            .Bind(v =>
            {
                if (label != null) label.text = v.ToString();
            });

        yield return new WaitUntil(() => done);
    }

    // ── Stamp animation ───────────────────────────────────────────────────

    private IEnumerator PlayStampAnimation()
    {
        if (_activeStamp == null) yield break;

        _activeStamp.localScale = Vector3.one * stampStartScale;
        _activeStamp.gameObject.SetActive(true);

        // Fade in using CanvasGroup
        CanvasGroup cg = _activeStamp.GetComponent<CanvasGroup>();
        if (cg == null) cg = _activeStamp.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float fadeDuration = 0.2f;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(fadeElapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;

        // Scale down (Slam)
        float elapsed = 0f;
        while (elapsed < stampDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / stampDuration);
            float curveT = stampCurve.Evaluate(t);
            float scale  = Mathf.LerpUnclamped(stampStartScale, stampEndScale, curveT);
            _activeStamp.localScale = Vector3.one * scale;
            yield return null;
        }

        _activeStamp.localScale = Vector3.one * stampEndScale;
        
        // Play double stamp SFX on impact
        if (AudioManager.Instance != null) AudioManager.Instance.PlayStampDoubleSFX();
    }

    // ── Silent score submit ───────────────────────────────────────────────

    private async void SubmitScoreAsync(int fares)
    {
        try
        {
            await UGSBootstrap.InitializeAsync();
            await LeaderboardManager.AddScoreAsync(leaderboardId, fares);
            Debug.Log("[EndShiftPanelController] Score submitted silently.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[EndShiftPanelController] Silent submit failed: " + e.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void SetLabel(TMP_Text label, int value)
    {
        if (label != null) label.text = value.ToString();
    }

    // ── Buttons ───────────────────────────────────────────────────────────

    private void OnPlayAgainClicked() => SceneLoader.LoadGameplay();
    private void OnMainMenuClicked()  => SceneLoader.LoadMainMenu();
}
