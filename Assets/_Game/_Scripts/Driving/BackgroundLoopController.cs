using UnityEngine;

/// <summary>
/// One parallax layer (sky, skyline, mid buildings, road...). Each layer has its own
/// speed multiplier — farther layers should use smaller multipliers.
/// Attach one of these per layer GameObject; assign 2 tiling copies of the sprite as children,
/// OR use a single Renderer with a material that supports texture offset (simpler for a jam).
/// </summary>
[System.Serializable]
public class ParallaxLayer
{
    public string name;                 // just for inspector readability
    public Transform[] segments;        // 2+ copies placed end-to-end for seamless looping
    public float speedMultiplier = 1f;  // relative scroll speed for this layer
    public float segmentWidth = 20f;    // world-space width of one segment, for wrap-around

    [HideInInspector] public float scrollOffset;
}

/// <summary>
/// Scrolls parallax layers based on DrivingController's current speed (event-driven,
/// no direct reference needed) and tints the sky toward dawn based on the night timer.
///
/// Setup:
///   - Add one ParallaxLayer entry per visual layer (sky/skyline/buildings/road).
///   - Each layer needs 2+ segment Transforms placed side by side so they can wrap.
///   - Assign skyRenderer (or a SpriteRenderer/Material) for the dawn tint lerp.
/// </summary>
public class BackgroundLoopController : MonoBehaviour
{
    [Header("Parallax layers (order: farthest first)")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Base scroll speed (world units/sec at speed01 = 1)")]
    [SerializeField] private float baseScrollSpeed = 5f;

    [Header("Dawn tint")]
    [SerializeField] private SpriteRenderer skyRenderer;
    [SerializeField] private Color nightColor = new Color(0.09f, 0.09f, 0.22f); // deep blue/purple
    [SerializeField] private Color dawnColor = new Color(0.95f, 0.75f, 0.65f);  // pale dawn wash

    [Header("Dawn progress source")]
    [Tooltip("Same GameClock instance NightManager uses. Drag the scene's GameClock object here " +
             "(NightManager's reference is private, so we need our own).")]
    [SerializeField] private GameClock gameClock;

    private float currentSpeed01;

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
        // In case DrivingController.Instance wasn't set yet in OnEnable (script order),
        // try subscribing again here.
        if (DrivingController.Instance != null)
            DrivingController.Instance.OnSpeedChanged += HandleSpeedChanged;
    }

    private void HandleSpeedChanged(float speed01)
    {
        currentSpeed01 = speed01;
    }

    private void Update()
    {
        // Dawn tint tracks the shift clock directly — independent of whether the car
        // is currently moving, since time still passes while stopped at an encounter.
        if (gameClock != null)
        {
            SetDawnProgress(gameClock.NormalizedTime);
        }

        if (currentSpeed01 <= 0f) return;

        float delta = baseScrollSpeed * currentSpeed01 * Time.deltaTime;

        foreach (var layer in layers)
        {
            ScrollLayer(layer, delta * layer.speedMultiplier);
        }
    }

    private void ScrollLayer(ParallaxLayer layer, float delta)
    {
        layer.scrollOffset += delta;

        foreach (var seg in layer.segments)
        {
            seg.position += Vector3.left * delta;
        }

        // Wrap: any segment fully off the left edge gets moved to the right end of the chain.
        foreach (var seg in layer.segments)
        {
            if (seg.position.x <= -layer.segmentWidth)
            {
                float rightmostX = GetRightmostX(layer);
                seg.position = new Vector3(rightmostX + layer.segmentWidth, seg.position.y, seg.position.z);
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

    /// <summary>
    /// Call every frame (or subscribe to a GameClock progress event, whichever Mahmoud exposes)
    /// with 0 = start of night, 1 = dawn, to lerp the sky color.
    /// </summary>
    public void SetDawnProgress(float t)
    {
        if (skyRenderer == null) return;
        skyRenderer.color = Color.Lerp(nightColor, dawnColor, Mathf.Clamp01(t));
    }
}