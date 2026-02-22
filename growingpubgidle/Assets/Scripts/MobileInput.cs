using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Mobile input hub. References VirtualJoystick and Fire button.
/// PlayerController reads from this component.
/// Works on ALL platforms (editor mouse + mobile touch) via EventSystem.
/// </summary>
public class MobileInput : MonoBehaviour
{
    [Header("Joystick")]
    [SerializeField] private VirtualJoystick moveJoystick;

    [Header("Fire Button")]
    [SerializeField] private GameObject fireButton;

    // ── Public read-only state ──
    public Vector2 MoveDirection => moveJoystick != null ? moveJoystick.Direction : Vector2.zero;
    public bool HasMoveInput => moveJoystick != null && moveJoystick.IsActive;
    public bool HasAimInput => false;
    public Vector2 AimDirection => MoveDirection;
    public bool IsAttacking { get; private set; }
    public bool IsSprinting => false;

    private void Start()
    {
        // Wire fire button EventTrigger at runtime (most reliable)
        if (fireButton != null)
        {
            var trigger = fireButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = fireButton.AddComponent<EventTrigger>();

            // Clear any existing entries to avoid duplicates
            trigger.triggers.Clear();

            // PointerDown → fire start
            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((_) => { IsAttacking = true; });
            trigger.triggers.Add(downEntry);

            // PointerUp → fire stop
            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((_) => { IsAttacking = false; });
            trigger.triggers.Add(upEntry);
        }
    }

    // Fallback public methods (can also be called from Inspector)
    public void OnFireDown() { IsAttacking = true; }
    public void OnFireUp() { IsAttacking = false; }
}
