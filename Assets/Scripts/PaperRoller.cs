using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class PaperRoller : MonoBehaviour
{
    // --- All of your variables and Awake()/Update() methods are unchanged ---
    #region Unchanged Code
    public float WorldSpaceDistancePulled { get; private set; } = 0f;
    private float startYPosition;

    [Header("Core Components")]
    private SwipeController swipeController;
    [Tooltip("Drag your 'Roller_Visual' object from the scene hierarchy here.")]
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
    public ParticleSystem boostParticles;
    public AudioClip boostStartSound;
    public AudioClip boostEndSound;
    public Image speedLinesVFX;
    public float speedLinesShakeAmount = 15f;
    private AudioSource audioSource;
    private float lastPullAmount = 0;
    private bool isBoostActive = false;
    private Vector3 initialSpeedLinesPosition;

    void Awake()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); }
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
        }

        if (currentPullAmount > 0.001f)
        {
            float fingerBonus = (swipeController.ActivePullingFingers > 1) ? twoFingerBonus : 1.0f;
            float finalSensitivity = pullSensitivity;
#if UNITY_EDITOR
            finalSensitivity *= editorMouseSensitivityBonus;
#endif
            float movementDistance = currentPullAmount * finalSensitivity * fingerBonus * speedMultiplier * Time.deltaTime;
            transform.position += Vector3.down * movementDistance;

            if (visualRoller != null)
            {
                float spinAmount = movementDistance * 200f;
                visualRoller.SpinRoller(spinAmount);
                float shakeFactor = Mathf.Clamp01(currentPullAmount * 2f);
                visualRoller.SetShake(shakeFactor);
            }
        }
        else if (visualRoller != null)
        {
            visualRoller.SetShake(0);
        }

        WorldSpaceDistancePulled = startYPosition - transform.position.y;
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetFloat(PlayerPrefsKeys.TotalDistancePulled, WorldSpaceDistancePulled);
    }

    public void LoadProgress()
    {
        float savedDistance = PlayerPrefs.GetFloat(PlayerPrefsKeys.TotalDistancePulled, 0f);
        transform.position = new Vector3(transform.position.x, startYPosition - savedDistance, transform.position.z);
        WorldSpaceDistancePulled = startYPosition - transform.position.y;
        continuousPaperManager?.InitializePaperAtRollerPosition();
    }
    #endregion

    // --- NEW DEBUG METHOD ---
    /// <summary>
    /// A special method for the debug menu to add distance to the roller.
    /// </summary>
    /// <param name="metersToAdd">The number of real-world meters to add.</param>
    public void AddDebugDistance(float metersToAdd)
    {
        if (continuousPaperManager == null || continuousPaperManager.paperTileLength <= 0) return;

        // Convert the real-world meters into Unity's world space units
        float conversionFactor = continuousPaperManager.realWorldMetersPerTile / continuousPaperManager.paperTileLength;
        float worldUnitsToAdd = metersToAdd / conversionFactor;

        // Move the roller down by that much
        transform.position += Vector3.down * worldUnitsToAdd;

        Debug.Log($"Added {metersToAdd} meters to the total distance.");
    }

    // --- Unchanged Power-up methods ---
    #region Unchanged PowerUp Methods
    public void ActivateSpeedBoost() { if (isBoostActive) return; StartCoroutine(SpeedBoostCoroutine(boostMultiplier, boostDuration)); }
    public void ActivateSpeedBoost(float multiplier, float duration) { if (isBoostActive) return; StartCoroutine(SpeedBoostCoroutine(multiplier, duration)); }
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        isBoostActive = true;
        speedMultiplier = multiplier;
        if (audioSource != null && boostStartSound != null) audioSource.PlayOneShot(boostStartSound);
        if (boostParticles != null) boostParticles.Play();
        if (speedLinesVFX != null) speedLinesVFX.gameObject.SetActive(true);
        if (boostTimerBar != null) boostTimerBar.gameObject.SetActive(true);
        if (boostTimerText != null) boostTimerText.gameObject.SetActive(true);
        float timeLeft = duration;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (boostTimerBar != null) { boostTimerBar.fillAmount = timeLeft / duration; }
            if (boostTimerText != null) { boostTimerText.text = timeLeft.ToString("F1"); }
            yield return null;
        }
        speedMultiplier = 1.0f;
        isBoostActive = false;
        if (audioSource != null && boostEndSound != null) audioSource.PlayOneShot(boostEndSound);
        if (boostParticles != null) boostParticles.Stop();
        if (speedLinesVFX != null)
        {
            speedLinesVFX.rectTransform.anchoredPosition = initialSpeedLinesPosition;
            speedLinesVFX.gameObject.SetActive(false);
        }
        if (boostTimerBar != null) boostTimerBar.gameObject.SetActive(false);
        if (boostTimerText != null) boostTimerText.gameObject.SetActive(false);
    }
    #endregion
}