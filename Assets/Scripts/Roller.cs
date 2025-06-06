using UnityEngine;

public class Roller : MonoBehaviour
{
    [Header("Components")]
    public Transform rollerModel;                // The visible roller cylinder

    [Header("Spin Settings")]
    [Tooltip("Tweak to control spin speed relative to pull speed.")]
    public float spinMultiplier = 1.0f;

    [Header("Shake Settings (Tune These)")]
    [Tooltip("The maximum distance the roller will shake when pulling at full speed.")]
    public float maxShakeAmount = 0.02f;
    [Tooltip("The swipe speed (in pixels per frame) at which the shake reaches its maximum intensity.")]
    public float speedForMaxShake = 50f;

    [Tooltip("The wobble frequency at low speeds.")]
    public float minShakeSpeed = 10f;
    [Tooltip("The vibration frequency at high speeds.")]
    public float maxShakeSpeed = 80f;

    [Tooltip("How quickly the roller returns to its original position when not being pulled.")]
    public float returnSmoothness = 10f;


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

        // We only care about the downward (negative Y) pull speed.
        float pullSpeed = 0f;
        if (swipeController.SwipeDelta.y < 0)
        {
            pullSpeed = Mathf.Abs(swipeController.SwipeDelta.y);
        }


        // If the player is pulling the paper...
        if (pullSpeed > 0.1f) // Use a small threshold to prevent tiny jitters
        {
            // --- 1. Spin the roller ---
            // The rotation is proportional to the pull speed.
            float spinAmount = pullSpeed * spinMultiplier * Time.deltaTime;
            rollerModel.Rotate(Vector3.right, spinAmount, Space.Self);

            // --- 2. Calculate Dynamic Shake ---
            // This is the core of the fix. We calculate a factor from 0.0 to 1.0.
            // 0.0 = no pull, 1.0 = pulling at 'speedForMaxShake' or faster.
            float shakeFactor = Mathf.Clamp01(pullSpeed / speedForMaxShake);

            // Use the factor to determine the CURRENT shake amount and speed for this frame.
            float currentShakeAmount = maxShakeAmount * shakeFactor;
            float currentShakeSpeed = Mathf.Lerp(minShakeSpeed, maxShakeSpeed, shakeFactor);

            // Generate the shake offset using our new dynamic values.
            Vector3 randomOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * currentShakeSpeed, 0f) * 2f - 1f, // Range -1 to 1
                Mathf.PerlinNoise(0f, Time.time * currentShakeSpeed) * 2f - 1f, // Range -1 to 1
                0f) * currentShakeAmount;

            // Apply the shake to the roller's position.
            rollerModel.localPosition = originalPosition + randomOffset;
        }
        else
        {
            // --- 3. Smoothly return to original position when not pulling ---
            // This feels much more polished than snapping back instantly.
            rollerModel.localPosition = Vector3.Lerp(rollerModel.localPosition, originalPosition, Time.deltaTime * returnSmoothness);
        }
    }
}