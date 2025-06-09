using System.Collections;
using UnityEngine;
using TMPro;

public class ResponsiveTextManager : MonoBehaviour
{
    [Header("Core Components")]
    public TextMeshProUGUI paperLengthText;

    [Header("Script References")]
    public SwipeController swipeController;
    public PaperRoller paperRoller;
    public ContinuousPaperManager continuousPaperManager;

    [Header("Responsive Sizing")]
    public float normalFontSize = 36f;
    public float maxFontSize = 48f;
    public float speedForMaxSize = 50f;
    public float resizeSmoothness = 10f;

    [Header("Milestone Pop Effect")]
    public float milestonePopSize = 60f;
    public float milestonePopDuration = 0.3f;
    public AudioClip milestoneSound;

    private AudioSource audioSource;
    private float currentLength = 0f;
    private float nextMilestone = 10f;
    private bool isPopping = false;

    void Start()
    {
        if (swipeController == null) swipeController = FindFirstObjectByType<SwipeController>();
        if (paperRoller == null) paperRoller = FindFirstObjectByType<PaperRoller>();
        if (continuousPaperManager == null) continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); }

        if (paperLengthText != null) { paperLengthText.fontSize = normalFontSize; }
    }

    void Update()
    {
        if (swipeController == null || paperLengthText == null || paperRoller == null || continuousPaperManager == null) return;
        HandleResponsiveSizing();
        CheckForMilestones();
    }

    private void HandleResponsiveSizing()
    {
        if (isPopping) return;
        float pullSpeed = (swipeController.SwipeDelta.y < 0) ? Mathf.Abs(swipeController.SwipeDelta.y) : 0f;
        float sizeFactor = Mathf.Clamp01(pullSpeed / speedForMaxSize);
        float targetSize = Mathf.Lerp(normalFontSize, maxFontSize, sizeFactor);
        paperLengthText.fontSize = Mathf.Lerp(paperLengthText.fontSize, targetSize, Time.deltaTime * resizeSmoothness);
    }

    private void CheckForMilestones()
    {
        float worldDistance = paperRoller.WorldSpaceDistancePulled;
        float conversionFactor = continuousPaperManager.realWorldMetersPerTile / continuousPaperManager.paperTileLength;
        currentLength = worldDistance * conversionFactor;

        if (currentLength >= nextMilestone)
        {
            StartCoroutine(PopTextCoroutine());
            nextMilestone *= 10;
        }
    }

    private IEnumerator PopTextCoroutine()
    {
        isPopping = true;
        float halfDuration = milestonePopDuration / 2f;
        float elapsedTime = 0f;
        if (milestoneSound != null && audioSource != null) { audioSource.PlayOneShot(milestoneSound); }
        float startingSize = paperLengthText.fontSize;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            paperLengthText.fontSize = Mathf.Lerp(startingSize, milestonePopSize, elapsedTime / halfDuration);
            yield return null;
        }
        elapsedTime = 0f;
        startingSize = paperLengthText.fontSize;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            paperLengthText.fontSize = Mathf.Lerp(startingSize, normalFontSize, elapsedTime / halfDuration);
            yield return null;
        }
        paperLengthText.fontSize = normalFontSize;
        isPopping = false;
    }
}