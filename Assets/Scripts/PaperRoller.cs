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
    [Tooltip("The main sensitivity control. This needs to be a larger number now (e.g., 30) because it's based on normalized screen height, not raw pixels.")]
    public float pullSensitivity = 30f;
    [Tooltip("How much faster the paper pulls with a second finger. 1.5 means 50% faster.")]
    public float twoFingerBonus = 1.5f;
    [Tooltip("An extra multiplier applied only when playing in the Unity Editor to make the mouse feel better.")]
    public float editorMouseSensitivityBonus = 6.0f;

    [Header("Glide/Damping Settings")]
    [Tooltip("How quickly the glide effect fades after letting go. Higher is faster.")]
    public float glideDamping = 5f;

    [Header("Power-up Settings")]
    [Tooltip("The multiplier to apply to the speed (e.g., 2.0 for 2x speed).")]
    public float boostMultiplier = 2.0f;
    [Tooltip("How long the speed boost lasts in seconds.")]
    public float boostDuration = 5.0f;
    private float speedMultiplier = 1.0f;

    [Header("Power-up Feedback (Drag from Scene)")]
    public Image boostTimerBar;
    public TextMeshProUGUI boostTimerText;
    public ParticleSystem boostParticles;
    public AudioClip boostStartSound;
    public AudioClip boostEndSound;
    [Tooltip("(Optional) The screen-filling image of anime speed lines.")]
    public Image speedLinesVFX;
    [Tooltip("How much the speed lines image shakes during the boost.")]
    public float speedLinesShakeAmount = 15f;

    private AudioSource audioSource;
    private float lastPullAmount = 0;
    private bool isBoostActive = false;
    private Vector3 initialSpeedLinesPosition;

    void Start()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); }

        startYPosition = transform.position.y;
        WorldSpaceDistancePulled = 0f;

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

    public void ResetPosition()
    {
        transform.position = new Vector3(transform.position.x, startYPosition, transform.position.z);
        lastPullAmount = 0;
        WorldSpaceDistancePulled = 0f;
    }
}