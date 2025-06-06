using UnityEngine;

public class Roller : MonoBehaviour
{
    public Transform rollerModel;                // The visible roller cylinder
    public float spinMultiplier = 1.0f;          // Tweak to control spin speed
    public float shakeAmount = 0.01f;            // How much to shake
    public float shakeSpeed = 20f;               // How fast the shake changes

    private SwipeController swipeController;
    private Vector3 originalPosition;

    private void Start()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        if (rollerModel != null)
        {
            originalPosition = rollerModel.localPosition;
        }
    }

    private void Update()
    {
        if (swipeController == null || rollerModel == null)
            return;

        Vector2 swipeDelta = swipeController.SwipeDelta;

        if (swipeDelta.y < 0f)
        {
            // Spin
            float verticalSpeed = swipeDelta.y;
            float spinAmount = verticalSpeed * spinMultiplier * Time.deltaTime;
            rollerModel.Rotate(Vector3.left, spinAmount, Space.Self);

            // Shake
            Vector3 randomOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f,
                0f) * shakeAmount;

            rollerModel.localPosition = originalPosition + randomOffset;
        }
        else
        {
            // Reset to original position when not spinning
            rollerModel.localPosition = originalPosition;
        }
    }
}
