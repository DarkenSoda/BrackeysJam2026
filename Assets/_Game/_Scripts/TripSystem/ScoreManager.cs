using Alchemy.Inspector;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ── Stats ─────────────────────────────────────────────────────────────
    public int Fares      { get; private set; }  // trips completed
    public int GoodReads  { get; private set; }  // correct choices (trusted truth / ignored lie)
    public int WrongTurns { get; private set; }  // wrong choices  (trusted lie / ignored truth)

    public event System.Action<int> OnFareCompleted;

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
        // Subscribe to TripController outcome events to track right/wrong choices.
        if (TripController.Instance != null)
            TripController.Instance.OnOutcomeResolved += HandleOutcome;
        else
            Debug.LogWarning("[ScoreManager] TripController.Instance is null at Start(). " +
                             "Good/wrong choice tracking won't work.");
    }

    private void OnDestroy()
    {
        if (TripController.Instance != null)
            TripController.Instance.OnOutcomeResolved -= HandleOutcome;
    }

    // ── Debug inspector ───────────────────────────────────────────────────
    [Header("Debug (read-only)")]
    [SerializeField, ReadOnly] private int debugFares;
    [SerializeField, ReadOnly] private int debugGoodReads;
    [SerializeField, ReadOnly] private int debugWrongTurns;

    // ── Public API ────────────────────────────────────────────────────────

    public void AddFare()
    {
        Fares++;
        debugFares = Fares;
        Debug.Log($"[ScoreManager] Fare completed — total: {Fares}");
        OnFareCompleted?.Invoke(Fares);
    }

    public void ResetShift()
    {
        Fares      = 0;
        GoodReads  = 0;
        WrongTurns = 0;
        debugFares      = 0;
        debugGoodReads  = 0;
        debugWrongTurns = 0;
    }

    // ── Private ───────────────────────────────────────────────────────────

    private void HandleOutcome(bool correct)
    {
        if (correct)
        {
            GoodReads++;
            debugGoodReads = GoodReads;
        }
        else
        {
            WrongTurns++;
            debugWrongTurns = WrongTurns;
        }
    }
}
