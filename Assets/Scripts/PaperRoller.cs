using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Solo.MOST_IN_ONE;

public class PaperRoller : MonoBehaviour
{
    public float WorldSpaceDistancePulled { get; private set; } = 0f;
    private float startYPosition;

    [Header("Core Components")]
    private SwipeController swipeController;
    public Roller visualRoller;
    private ContinuousPaperManager continuousPaperManager;

    [Header("Movement Settings")]
    public float pullSensitivity = 30f;
    public float twoFingerBonus = 1.5f;
    public float editorMouseSensitivityBonus = 6.0f;
    [Header("Glide/Damping Settings")]
    public float glideDamping = 5f;
    [Header("Power-up Settings")]
    public float boostMultiplier = 2.0f;
    public float boostDuration = 5.0f;
    private float speedMultiplier = 1.0f;
    [Header("Power-up Feedback (Drag from Scene)")]
    public Image boostTimerBar;
    public TextMeshProUGUI boostTimerText;
    public GameObject speedBoostButton;
    public ParticleSystem boostParticles;
    [Header("Haptic Feedback")]
    [Tooltip("How many world units the paper must scroll to trigger one haptic tick.")]
    public float distancePerHapticTick = 0.1f;
    private float distanceScrolledSinceLastTick = 0f;

    public Image speedLinesVFX;
    public float speedLinesShakeAmount = 15f;
    private float lastPullAmount = 0;
    public bool isBoostActive = false;
    private Vector3 initialSpeedLinesPosition;

    void Awake()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();
       
        startYPosition = transform.position.y;
        if (boostTimerBar != null) boostTimerBar.gameObject.SetActive(false);
        if (boostTimerText != null) boostTimerText.gameObject.SetActive(false);
        if (speedLinesVFX != null)
        {
            initialSpeedLinesPosition = speedLinesVFX.rectTransform.anchoredPosition;
            speedLinesVFX.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isBoostActive && speedLinesVFX != null)
        {
            float xShake = Random.Range(-1f, 1f) * speedLinesShakeAmount;
            float yShake = Random.Range(-1f, 1f) * speedLinesShakeAmount;
            speedLinesVFX.rectTransform.anchoredPosition = initialSpeedLinesPosition + new Vector3(xShake, yShake, 0);
        }

        if (swipeController == null) return;

        float currentPullAmount;
        if (swipeController.GetActivePawCount() > 0)
        {
            currentPullAmount = swipeController.NormalizedPullAmount;
            lastPullAmount = currentPullAmount;
        }
        else
        {
            lastPullAmount = Mathf.Lerp(lastPullAmount, 0f, Time.deltaTime * glideDamping);
            currentPullAmount = lastPullAmount;
            SoundManager.Instance?.StopPaperRollingSound();
        }

        if (currentPullAmount > 0.001f)
        {
            SoundManager.Instance?.StartPaperRollingSound();
            SoundManager.Instance?.PlayRandomMeow();
            float fingerBonus = (swipeController.ActivePullingFingers > 1) ? twoFingerBonus : 1.0f;
            float finalSensitivity = pullSensitivity;
#if UNITY_EDITOR
            finalSensitivity *= editorMouseSensitivityBonus;
#endif
            float movementDistance = currentPullAmount * finalSensitivity * fingerBonus * speedMultiplier * Time.deltaTime;
            transform.position += Vector3.down * movementDistance;
            distanceScrolledSinceLastTick += movementDistance;
            if (distanceScrolledSinceLastTick >= distancePerHapticTick)
            {
                // 3. Play the tick and reset the tracker.
                HapticManager.Instance?.PlayScrollTickHaptic();

                distanceScrolledSinceLastTick = 0f;
            }
            if (continuousPaperManager != null && continuousPaperManager.paperTileLength > 0)
            {
                float metersScrolledThisFrame = (movementDistance * continuousPaperManager.realWorldMetersPerTile) / continuousPaperManager.paperTileLength;
                ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.ScrollTotalDistance, metersScrolledThisFrame);
                ChallengeManager.Instance?.UpdateSessionScroll(metersScrolledThisFrame, Time.deltaTime);
                if (swipeController.ActivePullingFingers >= 2)
                {
                    ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.UseMultipleFingers, metersScrolledThisFrame);
                }
            }
            ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.ScrollTotalTime, Time.deltaTime);

            if (visualRoller != null)
            {
                float spinAmount = movementDistance * 800f;
                visualRoller.SpinRoller(spinAmount);
                float shakeFactor = Mathf.Clamp01(currentPullAmount * 2f);
                visualRoller.SetShake(shakeFactor);
            }
        }
        else
        {
            
            ChallengeManager.Instance?.OnScrollStopped();
            if (visualRoller != null)
            {
                visualRoller.SetShake(0);
            }
        }
        WorldSpaceDistancePulled = startYPosition - transform.position.y;
        
    }

    public void SaveProgress() { PlayerPrefs.SetFloat(PlayerPrefsKeys.TotalDistancePulled, WorldSpaceDistancePulled); }
    public void LoadProgress() { float savedDistance = PlayerPrefs.GetFloat(PlayerPrefsKeys.TotalDistancePulled, 0f); transform.position = new Vector3(transform.position.x, startYPosition - savedDistance, transform.position.z); WorldSpaceDistancePulled = startYPosition - transform.position.y; continuousPaperManager?.InitializePaperAtRollerPosition(); }
    public void AddDebugDistance(float metersToAdd) { if (continuousPaperManager == null || continuousPaperManager.paperTileLength <= 0) return; float conversionFactor = continuousPaperManager.realWorldMetersPerTile / continuousPaperManager.paperTileLength; float worldUnitsToAdd = metersToAdd / conversionFactor; transform.position += Vector3.down * worldUnitsToAdd; Debug.Log($"Added {metersToAdd} meters to the total distance."); }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        // Safety check: Don't start a new boost if one is already running.
        if (isBoostActive) return;

        // Start the coroutine that handles the actual gameplay effects.
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    // Overload method for simple button clicks that use default values.
    public void ActivateSpeedBoost()
    {
        ActivateSpeedBoost(boostMultiplier, boostDuration);
    }

    // This coroutine now ONLY handles gameplay effects (speed, particles, sound).
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        // --- Challenge System Logic ---
        ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.UseAnyBoost);
        ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.UseSpecificBoost, 1, "TurboPaws");

        // --- Set Gameplay State ---
        isBoostActive = true;
        speedMultiplier = multiplier;

        // --- IMPORTANT: Notify the UI that the boost has STARTED ---
        BoosterManager.Instance?.NotifyDataChanged();

        // --- Activate Gameplay Feedback (Sound, Particles, etc.) ---
        if (speedBoostButton != null) speedBoostButton.SetActive(false);
        SoundManager.Instance?.StartSpeedBoostMusic();
        if (boostParticles != null) boostParticles.Play();
        if (speedLinesVFX != null) speedLinesVFX.gameObject.SetActive(true);

        // --- Wait for the boost duration to pass ---
        yield return new WaitForSeconds(duration);

        // --- Reset Gameplay State ---
        speedMultiplier = 1.0f;
        isBoostActive = false;

        // --- Deactivate Gameplay Feedback ---
        SoundManager.Instance?.StopSpeedBoostMusic();
        if (boostParticles != null) boostParticles.Stop();
        if (speedLinesVFX != null)
        {
            speedLinesVFX.rectTransform.anchoredPosition = initialSpeedLinesPosition;
            speedLinesVFX.gameObject.SetActive(false);
        }
        if (speedBoostButton != null) speedBoostButton.SetActive(true);

        // --- IMPORTANT: Notify the UI that the boost has ENDED ---
        BoosterManager.Instance?.NotifyDataChanged();
    }
}