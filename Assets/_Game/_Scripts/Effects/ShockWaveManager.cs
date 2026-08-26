using System.Collections;
using UnityEngine;

/// <summary>
/// Night Shift Taxi - ShockWaveManager
/// HOW TO USE THIS (same pattern as AudioManager):
///     ShockWaveManager.Instance.PlayCorrectVFX();
///     ShockWaveManager.Instance.PlayWrongVFX();
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShockWaveManager : MonoBehaviour
{
    public static ShockWaveManager Instance { get; private set; }

    [Header("Timing")]
    [Tooltip("How long one shockwave ripple takes to fully play out.")]
    [SerializeField] private float shockWaveTime = 0.75f;

    [Header("Wave Distance")]
    [Tooltip("_WaveDistanceFromCenter value at the start of the ripple.")]
    [SerializeField] private float startDistance = -0.1f;
    [Tooltip("_WaveDistanceFromCenter value once the ripple has fully expanded.")]
    [SerializeField] private float endDistance = 1f;

    [Header("Materials")]
    [Tooltip("Shockwave material variant used on a correct Follow / correct call (e.g. green-tinted).")]
    [SerializeField] private Material correctMaterial;
    [Tooltip("Shockwave material variant used on a wrong Follow / wrong call (e.g. red-tinted).")]
    [SerializeField] private Material wrongMaterial;

    private SpriteRenderer _spriteRenderer;
    private Material _correctMaterialInstance;
    private Material _wrongMaterialInstance;
    private Material _activeMaterial;
    private Coroutine _shockWaveCoroutine;

    private static readonly int WaveDistanceFromCenterID = Shader.PropertyToID("_WaveDistanceFromCenter");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (correctMaterial != null) _correctMaterialInstance = new Material(correctMaterial);
        if (wrongMaterial != null) _wrongMaterialInstance = new Material(wrongMaterial);
    }

    public void PlayCorrectVFX() => Play(_correctMaterialInstance);

    public void PlayWrongVFX() => Play(_wrongMaterialInstance);

    private void Play(Material material)
    {
        if (material == null || _spriteRenderer == null) return;

        _activeMaterial = material;
        _spriteRenderer.material = material;

        if (_shockWaveCoroutine != null)
        {
            StopCoroutine(_shockWaveCoroutine);
        }
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(startDistance, endDistance));
    }

    private IEnumerator ShockWaveAction(float startPos, float endPos)
    {
        Material material = _activeMaterial;
        material.SetFloat(WaveDistanceFromCenterID, startPos);

        float elapsedTime = 0f;
        while (elapsedTime < shockWaveTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedAmount = Mathf.Lerp(startPos, endPos, elapsedTime / shockWaveTime);
            material.SetFloat(WaveDistanceFromCenterID, lerpedAmount);
            yield return null;
        }

        // Make sure we land exactly on the end value even if the last
        // frame overshot shockWaveTime slightly.
        material.SetFloat(WaveDistanceFromCenterID, endPos);
        _shockWaveCoroutine = null;
    }
}