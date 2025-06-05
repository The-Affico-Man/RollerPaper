using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeController : MonoBehaviour
{
    private GameControls controls;
    private Vector2 swipeDelta;
    public Vector2 SwipeDelta => swipeDelta;

    private Vector2 lastMousePosition;

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
