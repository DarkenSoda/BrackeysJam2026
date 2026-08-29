using Alchemy.Inspector;
using UnityEngine;

public enum TripState
{
    PICKUP,
    ENCOUNTER,
    DROPOFF
}

public class TripController : MonoBehaviour
{
    public static TripController Instance { get; private set; }

    [Header("Time penalty (real seconds)")]
    [SerializeField] private float wrongChoicePenaltySeconds = 30f;

    [Header("Content")]
    [SerializeField] private TaxiContent content;

    [Header("Trip shape")]
    [SerializeField] private int encountersPerTrip = 4;

    public TripState CurrentState { get; private set; }
    public int EncounterIndex { get; private set; }
    public RolledTrip CurrentTrip { get; private set; }
    public bool ShiftOver { get; private set; }

    public RolledTip CurrentTip
    {
        get
        {
            if (CurrentTrip == null || CurrentTrip.tips == null) return null;
            if (EncounterIndex < 0 || EncounterIndex >= CurrentTrip.tips.Count) return null;
            return CurrentTrip.tips[EncounterIndex];
        }
    }

    public event System.Action<TripState> OnTripStateChanged;
    public event System.Action<bool> OnOutcomeResolved; // true = correct choice

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (content == null)
        {
#if UNITY_EDITOR
            content = UnityEditor.AssetDatabase.LoadAssetAtPath<TaxiContent>(
                "Assets/_Game/ScriptableObjects/TaxiContent.asset");
#endif
            if (content == null)
                content = Resources.Load<TaxiContent>("TaxiContent");
        }

        if (GetComponent<DialogueHud>() == null)
            gameObject.AddComponent<DialogueHud>();
    }

    private void Start()
    {
        StartTrip();
    }

    public void StartTrip()
    {
        if (ShiftOver) return;
        if (content == null)
        {
            Debug.LogWarning("TripController: no TaxiContent assigned.");
            return;
        }

        CurrentTrip = TripGenerator.Roll(content);
        encountersPerTrip = CurrentTrip.tips.Count;
        EncounterIndex = 0;
        SetState(TripState.PICKUP);
    }

    public void BeginEncounters()
    {
        if (CurrentState != TripState.PICKUP) return;
        SetState(TripState.ENCOUNTER);
    }

    public void ResolveChoice(bool followed)
    {
        if (CurrentState != TripState.ENCOUNTER) return;

        RolledTip tip = CurrentTip;
        bool tipWasTruth = tip == null || tip.isTruth;
        bool correct = followed == tipWasTruth;

        LearnFact(tip);

        if (!correct && NightManager.Instance != null)
            NightManager.Instance.AddTimePenalty(wrongChoicePenaltySeconds);

        // Audio is already played instantly by DialogueHud on button click.

        OnOutcomeResolved?.Invoke(correct);

        if (ShiftOver) return;

        EncounterIndex++;
        int remaining = CurrentTrip != null ? CurrentTrip.tips.Count : 0;
        if (EncounterIndex >= remaining)
        {
            SetState(TripState.DROPOFF);
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddFare();
        }
        else
        {
            OnTripStateChanged?.Invoke(TripState.ENCOUNTER);
        }
    }

    public void LearnFact(RolledTip tip)
    {
        if (tip == null || string.IsNullOrEmpty(tip.newFact) || CurrentTrip == null)
            return;
        if (!CurrentTrip.scratchpad.Contains(tip.newFact))
            CurrentTrip.scratchpad.Add(tip.newFact);
    }

    public void TryStartNextFare()
    {
        if (ShiftOver) return;
        StartTrip();
    }

    public void ForceEndCurrentFare()
    {
        ShiftOver = true;
        SetState(TripState.DROPOFF);
    }

    [Header("Debug (read-only)")]
    [SerializeField, ReadOnly] private TripState debugState;

    private void SetState(TripState newState)
    {
        CurrentState = newState;
        debugState = newState;

        if (AudioManager.Instance != null)
        {
            if (newState == TripState.PICKUP) AudioManager.Instance.PlayPickup();
            else if (newState == TripState.DROPOFF) AudioManager.Instance.PlayDropoff();
        }

        OnTripStateChanged?.Invoke(newState);
    }
}
