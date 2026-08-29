using UnityEngine;

public class NpcWorldSpawner : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] private Transform taxiTransform;
    [Tooltip("Y position for the NPC (world space) — X is computed from the taxi + offsets.")]
    [SerializeField] private float yPosition = 0f;

    [Header("Landing Spot")]
    [SerializeField] private float landingOffsetX = 4f;  

    [Header("Sprite scale for the NPC")]
    [SerializeField] private Vector3 npcScale = Vector3.one;

    [Header("Rendering (auto-created if left empty)")]
    [SerializeField] private SpriteRenderer npcRenderer;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    private bool isScrollingIn;
    private bool hasSpawnedForCurrentEncounter;
    private TripState? pendingRevealState;

    private void Awake()
    {
        if (npcRenderer == null)
        {
            var go = new GameObject("NpcSprite");
            go.transform.SetParent(transform, false);
            npcRenderer = go.AddComponent<SpriteRenderer>();
            npcRenderer.sortingLayerName = sortingLayerName;
            npcRenderer.sortingOrder = sortingOrder;
        }

        npcRenderer.transform.localScale = npcScale;
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged += HandleTripStateChanged;
            TripController.Instance.OnOutcomeResolved += HandleOutcomeResolved;
        }

        if (DrivingController.Instance != null)
            DrivingController.Instance.OnArrivedAtStop += HandleCarArrived;

        if (TripDrivingBridge.Instance != null)
            TripDrivingBridge.Instance.OnEncounterDrivingStarted += HandleEncounterDrivingStarted;

        if (TripDrivingBridge.Instance != null)
            TripDrivingBridge.Instance.OnEncounterAboutToStop += HandleEncounterAboutToStop;
    }

    private void Start()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleTripStateChanged;
            TripController.Instance.OnTripStateChanged += HandleTripStateChanged;
            TripController.Instance.OnOutcomeResolved -= HandleOutcomeResolved;
            TripController.Instance.OnOutcomeResolved += HandleOutcomeResolved;

            HandleTripStateChanged(TripController.Instance.CurrentState);
        }

        if (DrivingController.Instance != null)
        {
            DrivingController.Instance.OnArrivedAtStop -= HandleCarArrived;
            DrivingController.Instance.OnArrivedAtStop += HandleCarArrived;
        }

        if (TripDrivingBridge.Instance != null)
        {
            TripDrivingBridge.Instance.OnEncounterDrivingStarted -= HandleEncounterDrivingStarted;
            TripDrivingBridge.Instance.OnEncounterDrivingStarted += HandleEncounterDrivingStarted;
            TripDrivingBridge.Instance.OnEncounterAboutToStop -= HandleEncounterAboutToStop;
            TripDrivingBridge.Instance.OnEncounterAboutToStop += HandleEncounterAboutToStop;
        }
        else
        {
            Debug.LogWarning("[NpcWorldSpawner] TripDrivingBridge.Instance is null at Start(). NPC won't spawn.");
        }
    }

    private void OnDisable()
    {
        if (TripController.Instance != null)
        {
            TripController.Instance.OnTripStateChanged -= HandleTripStateChanged;
            TripController.Instance.OnOutcomeResolved -= HandleOutcomeResolved;
        }

        if (DrivingController.Instance != null)
            DrivingController.Instance.OnArrivedAtStop -= HandleCarArrived;

        if (TripDrivingBridge.Instance != null)
        {
            TripDrivingBridge.Instance.OnEncounterDrivingStarted -= HandleEncounterDrivingStarted;
            TripDrivingBridge.Instance.OnEncounterAboutToStop -= HandleEncounterAboutToStop;
        }
    }

    private void Update()
    {
        if (!isScrollingIn) return;
        if (taxiTransform == null) return;
        if (BackgroundLoopController.Instance == null) return;

        float delta = BackgroundLoopController.Instance.PaceLayerFrameDelta;
        if (delta <= 0f) return;

        Vector3 pos = transform.position;
        pos.x -= delta;
        transform.position = pos;

        // This naturally stops the NPC exactly at the landing spot without snapping.
        float landingX = taxiTransform.position.x + landingOffsetX;
        if (pos.x <= landingX)
        {
            pos.x = landingX;
            transform.position = pos;
            isScrollingIn = false; 
        }
    }

    private void HandleTripStateChanged(TripState newState)
    {
        // If there is currently an NPC standing at the window when the state changes, 
        // they get left behind as the car drives away.
        if (npcRenderer != null && npcRenderer.enabled)
        {
            LeaveCurrentNpcBehind();
        }

        switch (newState)
        {
            case TripState.ENCOUNTER:
                isScrollingIn = false;
                pendingRevealState = newState;
                hasSpawnedForCurrentEncounter = false;
                break;

            case TripState.PICKUP:
            case TripState.DROPOFF:
                isScrollingIn = false;
                pendingRevealState = null;
                hasSpawnedForCurrentEncounter = false;
                break;
        }
    }

private void LeaveCurrentNpcBehind()
    {
        // Create a disposable clone of the current NPC to scroll away off-screen left
        GameObject departing = new GameObject("DepartingNpc");
        departing.transform.position = npcRenderer.transform.position; // Use the exact sprite position
        
        // 1. Grab the correct custom scale from the sprite renderer
        Vector3 finalScale = npcRenderer.transform.localScale;
        
        // 2. Flip the X-axis so the character faces left as the car drives away
        finalScale.x *= -1f; 
        
        departing.transform.localScale = finalScale;
        
        SpriteRenderer sr = departing.AddComponent<SpriteRenderer>();
        sr.sprite = npcRenderer.sprite;
        sr.sortingLayerName = npcRenderer.sortingLayerName;
        sr.sortingOrder = npcRenderer.sortingOrder;
        
        // Just in case your original sprite uses the flip toggles, copy those too
        sr.flipX = npcRenderer.flipX;
        sr.flipY = npcRenderer.flipY;
        
        departing.AddComponent<DepartingNpcMover>();
        
        // Hide the main spawner so it's ready to place the next encounter off-screen right
        SetVisible(false);
    }

    private void HandleEncounterDrivingStarted()
    {
        if (hasSpawnedForCurrentEncounter) return;
        hasSpawnedForCurrentEncounter = true;
        SpawnOffscreen();
    }

    private void SpawnOffscreen()
    {
        if (taxiTransform == null) return;

        RolledTip tip = TripController.Instance != null ? TripController.Instance.CurrentTip : null;
        Sprite sprite = (tip != null && tip.giver != null) ? tip.giver.truthSprite : null;
        if (npcRenderer == null || sprite == null) return;

        npcRenderer.sprite = sprite;
        SetVisible(true);

        // Calculate exact spawn position based on driving distance
        int cycles = TripDrivingBridge.Instance != null ? TripDrivingBridge.Instance.CyclesNeededForCurrentStop : 1;
        float segmentWidth = BackgroundLoopController.Instance != null ? BackgroundLoopController.Instance.PaceLayerSegmentWidth : 20f;
        float totalDrivingDistance = cycles * segmentWidth;

        Vector3 spawnPos = new Vector3(taxiTransform.position.x + landingOffsetX + totalDrivingDistance, yPosition, transform.position.z);
        transform.position = spawnPos;
        isScrollingIn = true;
    }

    private void HandleEncounterAboutToStop()
    {
        // Left intentionally empty. 
        // We no longer snap the position here so the NPC can finish scrolling smoothly.
    }

    private void HandleCarArrived()
    {
        // Left intentionally empty. 
    }

    private void HandleOutcomeResolved(bool correct)
    {
        if (TripController.Instance == null) return;
        if (TripController.Instance.CurrentState != TripState.ENCOUNTER) return;

        RolledTip tip = TripController.Instance.CurrentTip;
        if (npcRenderer != null && tip != null && !tip.isTruth && tip.giver != null && tip.giver.lieSprite != null)
        {
            npcRenderer.sprite = tip.giver.lieSprite;
        }
    }

    private void SetVisible(bool visible)
    {
        if (npcRenderer != null)
            npcRenderer.enabled = visible;
    }
}

/// <summary>
/// A tiny helper component attached to the clone NPC when the car leaves.
/// It scrolls them left in perfect lockstep with the parallax road, then destroys them.
/// </summary>
public class DepartingNpcMover : MonoBehaviour
{
    private float leftBound = -30f; 

    private void Start()
    {
        // Automatically determine when they are safely off the left side of the screen
        if (Camera.main != null) 
        {
            leftBound = Camera.main.transform.position.x - (Camera.main.orthographicSize * Camera.main.aspect) - 5f;
        }
    }

    private void Update()
    {
        if (BackgroundLoopController.Instance == null) return;
        
        float delta = BackgroundLoopController.Instance.PaceLayerFrameDelta;
        if (delta > 0f)
        {
            transform.position += Vector3.left * delta;
            if (transform.position.x < leftBound)
            {
                Destroy(gameObject);
            }
        }
    }
}