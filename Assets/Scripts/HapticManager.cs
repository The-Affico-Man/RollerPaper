using UnityEngine;

/// <summary>
/// A simple, universal haptic feedback manager that uses the built-in
/// Handheld.Vibrate() for cross-platform compatibility on iOS and Android.
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Tooltip("A master switch to easily enable or disable all haptic feedback.")]
    public bool hapticsEnabled = true;

    // --- THIS IS THE NEW, TWEAKABLE VARIABLE ---
    [Header("Tick Cooldown")]
    [Tooltip("The minimum time (in seconds) that must pass between each paper scroll 'tick'. A higher value makes the haptics feel weaker and less aggressive.")]
    [Range(0.01f, 0.2f)]
    public float timeBetweenTicks = 0.05f;
    // ------------------------------------------

    private float cooldownTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void Vibrate()
    {
        if (hapticsEnabled && SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
    }

    // --- THIS METHOD IS NOW BACK TO ITS SIMPLER FORM ---
    /// <summary>
    /// A short, sharp "tick" - perfect for the paper texture feel.
    /// This includes a cooldown to prevent overwhelming the device's motor.
    /// </summary>
    public void PlayTick()
    {
        // Only play a tick if the cooldown has finished.
        if (cooldownTimer <= 0)
        {
            // Reset the cooldown using our new public variable.
            cooldownTimer = timeBetweenTicks;
            Vibrate();
        }
    }

    // --- All other methods are unchanged ---
    public void PlaySuccess() { Vibrate(); }
    public void PlayFailure() { Vibrate(); }
}