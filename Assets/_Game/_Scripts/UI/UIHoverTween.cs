using UnityEngine;
using UnityEngine.EventSystems;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// A simple component to add hover scale tweening to UI elements (like Buttons).
/// Attach this to any UI element to make it pop on mouse hover.
/// </summary>
public class UIHoverTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Target scale when hovered.")]
    [SerializeField] private float hoverScale = 1.05f;
    
    [Tooltip("Duration of the scale animation in seconds.")]
    [SerializeField] private float tweenDuration = 0.15f;

    private Vector3 originalScale;
    private MotionHandle currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (currentTween.IsActive())
            currentTween.Cancel();
        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentTween.IsActive()) currentTween.Cancel();
        
        currentTween = LMotion.Create(transform.localScale, originalScale * hoverScale, tweenDuration)
            .WithEase(Ease.OutQuad)
            .BindToLocalScale(transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentTween.IsActive()) currentTween.Cancel();
        
        currentTween = LMotion.Create(transform.localScale, originalScale, tweenDuration)
            .WithEase(Ease.OutQuad)
            .BindToLocalScale(transform);
    }
}
