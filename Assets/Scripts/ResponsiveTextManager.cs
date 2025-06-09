using System.Collections;
using UnityEngine;
using TMPro;

public class ResponsiveTextManager : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("The TextMeshProUGUI component for the paper length counter.")]
    public TextMeshProUGUI paperLengthText;

    [Header("Responsive Sizing")]
    [Tooltip("The normal font size when the player is not swiping.")]
    public float normalFontSize = 36f;
    [Tooltip("The maximum font size when the player is swiping at full speed.")]
    public float maxFontSize = 48f;
    [Tooltip("The swipe speed (pixels per frame) needed to reach the maximum font size.")]
    public float speedForMaxSize = 50f;
    [Tooltip("How quickly the font size changes. Higher is faster.")]
    public float resizeSmoothness = 10f;

    [Header("Milestone Pop Effect")]
    [Tooltip("The font size the text will 'pop' to when hitting a milestone (10, 100, 1000).")]
    public float milestonePopSize = 60f;
    [Tooltip("How long the pop animation lasts in seconds.")]
    public float milestonePopDuration = 0.3f;
    [Tooltip("The sound to play when a milestone is reached.")]
    public AudioClip milestoneSound;


    // --- Private state variables ---
    private SwipeController swipeController;
    private AudioSource audioSource;
    private float currentLength = 0f;
    private float nextMilestone = 10f;
    private bool isPopping = false;

    void Start()
    {
        // Find the swipe controller to get swipe speed.
        swipeController = FindFirstObjectByType<SwipeController>();

        // Ensure we have an AudioSource for the pop sound.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Set the initial font size.
        if (paperLengthText != null)
        {
            paperLengthText.fontSize = normalFontSize;
        }
    }

    void Update()
    {
        // If we don't have the necessary components, do nothing.
        if (swipeController == null || paperLengthText == null)
        {
            return;
        }

        // Handle the two visual effects.
        HandleResponsiveSizing();
        CheckForMilestones();
    }

    /// <summary>
    /// Continuously adjusts the font size based on the current swipe speed.
    /// </summary>
    private void HandleResponsiveSizing()
    {
        // If a milestone 'pop' animation is playing, let it have control.
        if (isPopping)
        {
            return;
        }

        // Get the downward pull speed.
        float pullSpeed = (swipeController.SwipeDelta.y < 0) ? Mathf.Abs(swipeController.SwipeDelta.y) : 0f;

        // Calculate a "size factor" from 0.0 to 1.0 based on the pull speed.
        float sizeFactor = Mathf.Clamp01(pullSpeed / speedForMaxSize);

        // Determine the target font size based on the factor.
        float targetSize = Mathf.Lerp(normalFontSize, maxFontSize, sizeFactor);

        // Smoothly animate the current font size towards the target size.
        paperLengthText.fontSize = Mathf.Lerp(paperLengthText.fontSize, targetSize, Time.deltaTime * resizeSmoothness);
    }

    /// <summary>
    /// Checks if the total pulled length has crossed a milestone.
    /// </summary>
    private void CheckForMilestones()
    {
        // Get the current length from the swipe controller's raw data.
        // We use the same conversion factor as your ContinuousPaperManager.
        currentLength = swipeController.TotalSwipeDistance * 0.0002f;

        // If the current length has passed our next milestone...
        if (currentLength >= nextMilestone)
        {
            // Trigger the pop effect!
            StartCoroutine(PopTextCoroutine());

            // Set the next milestone (10 -> 100 -> 1000 -> etc.).
            nextMilestone *= 10;
        }
    }

    /// <summary>
    /// A coroutine that handles the "pop" animation over several frames.
    /// </summary>
    private IEnumerator PopTextCoroutine()
    {
        isPopping = true;
        float halfDuration = milestonePopDuration / 2f;
        float elapsedTime = 0f;

        // Play the milestone sound effect if it exists.
        if (milestoneSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(milestoneSound);
        }

        // --- Animate UP to the pop size ---
        float startingSize = paperLengthText.fontSize;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            paperLengthText.fontSize = Mathf.Lerp(startingSize, milestonePopSize, progress);
            yield return null; // Wait for the next frame.
        }

        // --- Animate DOWN to the normal size ---
        elapsedTime = 0f;
        startingSize = paperLengthText.fontSize;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            paperLengthText.fontSize = Mathf.Lerp(startingSize, normalFontSize, progress);
            yield return null; // Wait for the next frame.
        }

        // Ensure the font size is exactly the normal size at the end.
        paperLengthText.fontSize = normalFontSize;
        isPopping = false;
    }
}