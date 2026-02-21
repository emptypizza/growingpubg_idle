using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// EventSystem-based virtual joystick. Works on both mobile (touch) and editor (mouse).
/// Attach to the joystick background Image. The handle moves within the background radius.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float deadZone = 0.1f;

    /// <summary> Normalised direction (-1..1 per axis). </summary>
    public Vector2 Direction { get; private set; }
    /// <summary> True while the user is touching/dragging. </summary>
    public bool IsActive { get; private set; }

    private RectTransform baseRect;
    private Canvas canvas;
    private Camera uiCamera;

    private void Awake()
    {
        baseRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            uiCamera = canvas.worldCamera;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;
        OnDrag(eventData); // immediately calculate direction
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                baseRect, eventData.position, uiCamera, out localPoint))
            return;

        // Normalise to -1..1 based on half-size of background
        Vector2 halfSize = baseRect.sizeDelta * 0.5f;
        Vector2 normalised = new Vector2(
            localPoint.x / halfSize.x,
            localPoint.y / halfSize.y);

        // Clamp to unit circle
        if (normalised.magnitude > 1f)
            normalised.Normalize();

        // Apply dead zone
        Direction = normalised.magnitude < deadZone ? Vector2.zero : normalised;
        IsActive = Direction.sqrMagnitude > 0.001f;

        // Move handle visual
        if (handle != null)
            handle.anchoredPosition = normalised * halfSize;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        IsActive = false;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}
