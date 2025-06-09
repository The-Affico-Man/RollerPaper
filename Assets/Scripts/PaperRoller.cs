using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class PaperRoller : MonoBehaviour
{
    public float WorldSpaceDistancePulled { get; private set; }
    private float startYPosition;

    [Header("Core Components")]
    private SwipeController swipeController;
    [Tooltip("Drag your 'Roller_Visual' object from the scene hierarchy here.")]
    public Roller visualRoller;

    [Header("Movement Settings")]
    public float pullSensitivity = 1.0f;
    public float twoFingerBonus = 1.5f;

    [Header("Glide/Damping Settings")]
    public float glideDamping = 5f;

    // --- NEW POWER-UP CONTROLS ---
    [Header("Power-up Settings")]
    [Tooltip("The multiplier to apply to the speed (e.g., 2.0 for 2x speed).")]
    public float boostMultiplier = 2.0f;
    [Tooltip("How long the speed boost lasts in seconds.")]
    public float boostDuration = 5.0f;
    // The 'speedMultiplier' variable will be controlled by the coroutine
    private float speedMultiplier = 1.0f;
    // ----------------------------

    [Header("Power-up Feedback (Drag from Scene)")]
    public Image boostTimerBar;
    public TextMeshProUGUI boostTimerText;
    public ParticleSystem boostParticles;
    public AudioClip boostStartSound;
    public AudioClip boostEndSound;
    [Tooltip("(Optional) The screen-filling image of anime speed lines.")]
    public Image speedLinesVFX; // New VFX reference
    [Tooltip("How much the speed lines image shakes during the boost.")]
    public float speedLinesShakeAmount = 15f; // New shake control

    private AudioSource audioSource;
    private Vector2 lastSwipeDelta;
    private bool isBoostActive = false;
    private Vector3 initialSpeedLinesPosition; // To store the starting position for shaking

    void Start()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); }

        startYPosition = transform.position.y;
        WorldSpaceDistancePulled = 0f;

        // Hide all feedback UI at the start
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
        // --- NEW: Shake the speed lines if the boost is active ---
        if (isBoostActive && speedLinesVFX != null)
        {
            float xShake = Random.Range(-1f, 1f) * speedLinesShakeAmount;
            float yShake = Random.Range(-1f, 1f) * speedLinesShakeAmount;
            speedLinesVFX.rectTransform.anchoredPosition = initialSpeedLinesPosition + new Vector3(xShake, yShake, 0);
        }
        // --------------------------------------------------------

        if (swipeController == null) return;
        Vector2 currentFrameSwipe;

        if (swipeController.GetActivePawCount() > 0)
        {
            currentFrameSwipe = swipeController.SwipeDelta;
            lastSwipeDelta = currentFrameSwipe;
        }
        else
        {
            lastSwipeDelta = Vector2.Lerp(lastSwipeDelta, Vector2.zero, Time.deltaTime * glideDamping);
            currentFrameSwipe = lastSwipeDelta;
        }

        if (currentFrameSwipe.y < 0)
        {
            float pullAmount = Mathf.Abs(currentFrameSwipe.y);
            float fingerBonus = (swipeController.GetActivePawCount() > 1) ? twoFingerBonus : 1.0f;
            float movementDistance = pullAmount * pullSensitivity * fingerBonus * speedMultiplier * Time.deltaTime;

            transform.position += Vector3.down * movementDistance;

            if (visualRoller != null)
            {
                float spinAmount = movementDistance * 200f;
                visualRoller.SpinRoller(spinAmount);
                float shakeFactor = Mathf.Clamp01(pullAmount / 20f);
                visualRoller.SetShake(shakeFactor);
            }
        }
        else if (visualRoller != null)
        {
            visualRoller.SetShake(0);
        }

        WorldSpaceDistancePulled = startYPosition - transform.position.y;
    }

    /// <summary>
    /// This is the public function your NEW UI button will call.
    /// It uses the values set in the Inspector.
    /// </summary>
    public void ActivateSpeedBoost()
    {
        if (isBoostActive) return;
        StartCoroutine(SpeedBoostCoroutine(boostMultiplier, boostDuration));
    }

    // This private method can still be used by the debug button if needed
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        if (isBoostActive) return;
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        isBoostActive = true;

        // --- START FEEDBACK ---
        speedMultiplier = multiplier;
        if (audioSource != null && boostStartSound != null) audioSource.PlayOneShot(boostStartSound);
        if (boostParticles != null) boostParticles.Play();
        if (speedLinesVFX != null) speedLinesVFX.gameObject.SetActive(true);
        if (boostTimerBar != null) boostTimerBar.gameObject.SetActive(true);
        if (boostTimerText != null) boostTimerText.gameObject.SetActive(true);
        // ----------------------

        float timeLeft = duration;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (boostTimerBar != null) { boostTimerBar.fillAmount = timeLeft / duration; }
            if (boostTimerText != null) { boostTimerText.text = timeLeft.ToString("F1"); }
            yield return null;
        }

        // --- END FEEDBACK ---
        speedMultiplier = 1.0f;
        isBoostActive = false;
        if (audioSource != null && boostEndSound != null) audioSource.PlayOneShot(boostEndSound);
        if (boostParticles != null) boostParticles.Stop();
        if (speedLinesVFX != null)
        {
            speedLinesVFX.rectTransform.anchoredPosition = initialSpeedLinesPosition; // Reset position
            speedLinesVFX.gameObject.SetActive(false);
        }
        if (boostTimerBar != null) boostTimerBar.gameObject.SetActive(false);
        if (boostTimerText != null) boostTimerText.gameObject.SetActive(false);
        // --------------------
    }

    public void ResetPosition()
    {
        transform.position = new Vector3(transform.position.x, startYPosition, transform.position.z);
        lastSwipeDelta = Vector2.zero;
        WorldSpaceDistancePulled = 0f;
    }
}