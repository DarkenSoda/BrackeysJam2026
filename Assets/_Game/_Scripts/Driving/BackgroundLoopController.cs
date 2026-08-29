using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    public string name;                 
    public Transform[] segments;        
    public float speedMultiplier = 1f;  
    public float segmentWidth = 20f;    

    [Tooltip("Tick this on exactly ONE layer (usually Road) — that layer's wrap cycles " +
             "are what TripDrivingBridge counts for the 1-3 cycle driving buffer. " +
             "No string-matching, so there's nothing to mistype.")]
    public bool isDrivingPaceLayer = false;

    [HideInInspector] public float scrollOffset;
}

public class BackgroundLoopController : MonoBehaviour
{
    public static BackgroundLoopController Instance { get; private set; }

    [Header("Parallax layers (order: farthest first)")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Base scroll speed (world units/sec at speed01 = 1)")]
    [SerializeField] private float baseScrollSpeed = 5f;

    [Header("Dawn tint")]
    [SerializeField] private SpriteRenderer skyRenderer;
    [SerializeField] private Color nightColor = new Color(0.09f, 0.09f, 0.22f); 
    [SerializeField] private Color dawnColor = new Color(0.95f, 0.75f, 0.65f);  

    [Header("Dawn progress source")]
    [SerializeField] private GameClock gameClock;

    public event System.Action<bool> OnLayerCycled;

    public float PaceLayerFrameDelta { get; private set; }

    /// <summary>Returns the segment width of the active pace layer so we can calculate exact driving distances.</summary>
    public float PaceLayerSegmentWidth 
    {
        get 
        {
            foreach (var layer in layers) 
            {
                if (layer.isDrivingPaceLayer) return layer.segmentWidth;
            }
            return 20f; // Fallback
        }
    }

    private float currentSpeed01;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (DrivingController.Instance != null)
            DrivingController.Instance.OnSpeedChanged += HandleSpeedChanged;
    }

    private void OnDisable()
    {
        if (DrivingController.Instance != null)
            DrivingController.Instance.OnSpeedChanged -= HandleSpeedChanged;
    }

    private void Start()
    {
        if (DrivingController.Instance != null)
            DrivingController.Instance.OnSpeedChanged += HandleSpeedChanged;
    }

    private void HandleSpeedChanged(float speed01)
    {
        currentSpeed01 = speed01;
    }

    private void Update()
    {
        if (gameClock != null)
        {
            SetDawnProgress(gameClock.NormalizedTime);
        }

        if (currentSpeed01 <= 0f)
        {
            PaceLayerFrameDelta = 0f;
            return;
        }

        float delta = baseScrollSpeed * currentSpeed01 * Time.deltaTime;

        foreach (var layer in layers)
        {
            ScrollLayer(layer, delta * layer.speedMultiplier);
            if (layer.isDrivingPaceLayer)
                PaceLayerFrameDelta = delta * layer.speedMultiplier;
        }
    }

    private void ScrollLayer(ParallaxLayer layer, float delta)
    {
        layer.scrollOffset += delta;

        foreach (var seg in layer.segments)
        {
            seg.position += Vector3.left * delta;
        }

        foreach (var seg in layer.segments)
        {
            if (seg.position.x <= -layer.segmentWidth)
            {
                float rightmostX = GetRightmostX(layer);
                seg.position = new Vector3(rightmostX + layer.segmentWidth, seg.position.y, seg.position.z);
                OnLayerCycled?.Invoke(layer.isDrivingPaceLayer);
            }
        }
    }

    private float GetRightmostX(ParallaxLayer layer)
    {
        float max = float.MinValue;
        foreach (var seg in layer.segments)
            if (seg.position.x > max) max = seg.position.x;
        return max;
    }

    public void SetDawnProgress(float t)
    {
        if (skyRenderer == null) return;
        skyRenderer.color = Color.Lerp(nightColor, dawnColor, Mathf.Clamp01(t));
    }
}