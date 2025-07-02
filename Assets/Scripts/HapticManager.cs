using UnityEngine;
using Solo.MOST_IN_ONE; // Make sure you have this using statement
using System.Collections; // Required for Coroutines

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Master Switch")]
    [Tooltip("Check this box to enable haptic feedback throughout the game.")]
    public bool hapticsEnabled = true;

    // --- THIS IS THE NEW PART: Define our custom pattern ---
    private Most_HapticFeedback.CustomHapticPattern scrollTickPattern;
    // --------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }

        // --- THIS IS THE NEW PART: Create the pattern data on startup ---
        // For iOS, we fall back to a light preset.
        var iosHaptic = new Most_HapticFeedback.IOS_Haptic(Most_HapticFeedback.HapticTypes.LightImpact, 0);

        // For Android, we define our custom light tap.
        // Vibrate for 5ms at a low intensity of 40.
        var androidHaptic = new Most_HapticFeedback.Android_Haptic(0, 5, 40);

        // Combine them into a single pattern structure.
        scrollTickPattern = new Most_HapticFeedback.CustomHapticPattern(
            new Most_HapticFeedback.IOS_Haptic[] { iosHaptic },
            new Most_HapticFeedback.Android_Haptic[] { androidHaptic }
        );
        // -----------------------------------------------------------------
    }

    // --- THIS IS THE CORRECTED METHOD ---
    public void PlayScrollTickHaptic()
    {
        if (!hapticsEnabled) return;

        // Now, we use the public method provided by the asset to play our custom pattern.
        // We need to play it inside a coroutine.
        StartCoroutine(Most_HapticFeedback.GeneratePattern(scrollTickPattern));
    }

    public void PlaySuccessHaptic()
    {
        if (!hapticsEnabled) return;
        Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Success);
    }

    public void PlayFailureHaptic()
    {
        if (!hapticsEnabled) return;
        Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Failure);
    }
}