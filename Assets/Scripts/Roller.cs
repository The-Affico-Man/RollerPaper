using UnityEngine;

public class Roller : MonoBehaviour
{
    public Transform rollerModel;                // The visible roller cylinder
    public float spinMultiplier = 1.0f;          // Tweak to control spin speed

    private SwipeController swipeController;

    private void Start()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
    }

    private void Update()
    {
        if (swipeController == null || rollerModel == null)
            return;

        Vector2 swipeDelta = swipeController.SwipeDelta;

        if (swipeDelta != Vector2.zero)
        {
            // Use vertical swipe speed to spin the roller on X axis
            float verticalSpeed = swipeDelta.y;

            // Calculate rotation amount
            float spinAmount = verticalSpeed * spinMultiplier * Time.deltaTime;

            // Rotate the roller on the X axis
            rollerModel.Rotate(Vector3.right, spinAmount, Space.Self);
        }
    }
}
