using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeController : MonoBehaviour
{
    private GameControls controls;
    private Vector2 swipeDelta;
    public Vector2 SwipeDelta => swipeDelta;

    private Vector2 lastMousePosition;
    private float totalSwipeDistance = 0f; // Accumulated swipe distance in Unity units
    public float TotalSwipeDistance => totalSwipeDistance;
    private void Awake()
    {
        controls = new GameControls();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
        controls.Gameplay.Swipe.performed += ctx => swipeDelta = ctx.ReadValue<Vector2>();
        controls.Gameplay.Swipe.canceled += _ => swipeDelta = Vector2.zero;
    }

    private void OnDisable()
    {
        controls.Gameplay.Swipe.performed -= ctx => swipeDelta = ctx.ReadValue<Vector2>();
        controls.Gameplay.Swipe.canceled -= _ => swipeDelta = Vector2.zero;
        controls.Gameplay.Disable();
    }

    private void Update()
    {
        if (SwipeDelta.y < 0f) // Only down swipes
        {
            float swipeAmount = -SwipeDelta.y * Time.deltaTime; // Make positive
            totalSwipeDistance += swipeAmount;
        }
#if UNITY_EDITOR
        // Emulate swipe with mouse drag
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();

            if (lastMousePosition != Vector2.zero)
                swipeDelta = currentMousePosition - lastMousePosition;

            lastMousePosition = currentMousePosition;
        }
        else
        {
            swipeDelta = Vector2.zero;
            lastMousePosition = Vector2.zero;
        }
#endif
    }
}
