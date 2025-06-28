using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MilestoneUIManager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    // --- UPDATED UI REFERENCES ---
    [Header("UI Components")]
    public GameObject milestoneScreenPanel;
    public GameObject milestoneItemPrefab;
    public ScrollRect scrollRect;
    public GameObject redDotNotification;
    public Transform coinUIScreenPosition;
    public Transform coinUINormalPosition;
    public RectTransform coinUIPanel;
    [Header("Other UI Manager References")]
    [Tooltip("Drag the GameObject that has the ShopManager script on it here.")]
    public ShopManager shopManager;

    [Header("Notification System")]
    [Tooltip("Drag your new NotificationPanel prefab here.")]
    public GameObject notificationPanelPrefab;
    // --- THIS IS THE FIX (Part 1): Add a reference to the main canvas ---
    [Tooltip("Drag your main UI Canvas object from the scene hierarchy here.")]
    public Canvas mainCanvas;
    public Canvas topLayerCanvas;
    [Tooltip("How long the notification stays on screen before sliding out.")]
    public float notificationDuration = 3f;
    [Tooltip("How long the celebration stays on screen before sliding out.")]
    public float celebrationDuration = 4f;
    [Tooltip("Drag your new ConfettiEffect prefab here.")]
    public GameObject confettiEffectPrefab;
    [Tooltip("An optional transform to control where the confetti spawns. If empty, it will spawn at the center of the screen.")]
    public Transform confettiSpawnPoint;
    // Unchanged variables
    #region Unchanged Variables
    private RectTransform contentPanel;
    [Header("Manual Layout & Snapping")]
    public float firstItemCenterPosX;
    public float snapJumpValue;
    public float snapSpeed = 10f;
    public float snapVelocityThreshold = 200f;
    public float itemSpacing = 50f;
    public float leftPadding = 20f;
    public float rightPadding = 20f;
    [Header("Script References")]
    public PaperRoller paperRoller;
    public ContinuousPaperManager paperManager;
    private List<GameObject> spawnedMilestoneItems = new List<GameObject>();
    private List<float> snapPositionsX = new List<float>();
    private Milestone nextMilestoneToUnlock;
    private bool isNotificationShowing = false;
    private bool isDragging = false;
    private int currentSnapTargetIndex = 0;
    private Milestone currentlyNotifiedMilestone = null;
    #endregion

    void Start()
    {
        contentPanel = scrollRect.content;
        milestoneScreenPanel.SetActive(false);
        if (redDotNotification != null) redDotNotification.SetActive(false);
        paperRoller = FindFirstObjectByType<PaperRoller>();
        paperManager = FindFirstObjectByType<ContinuousPaperManager>();
        FindNextMilestone();
    }

    private void LateUpdate()
    {
        if (isNotificationShowing) return;
        float currentDistance = GetCurrentMeters();
        CheckForMilestoneUnlock(currentDistance);
        CheckForNearMilestone(currentDistance);
        CheckForUncollectedRewards();
    }

    private void CheckForMilestoneUnlock(float currentDistance)
    {
        if (nextMilestoneToUnlock != null && currentDistance >= nextMilestoneToUnlock.distanceInMeters)
        {
            Milestone unlockedMilestone = nextMilestoneToUnlock;
            MilestoneManager.Instance.UnlockMilestone(unlockedMilestone);
            string message = $"MILESTONE REACHED!\n<size=80%>{unlockedMilestone.milestoneName}";
            StartCoroutine(ShowAnimatedPanel(message, celebrationDuration, true));
            Vector3 spawnPos = confettiSpawnPoint != null ? confettiSpawnPoint.position : Vector3.zero;
            ParticlePooler.Instance?.SpawnFromPool("Confetti", spawnPos, Quaternion.identity);
            FindNextMilestone();
        }
    }

    private void CheckForNearMilestone(float currentDistance)
    {
        if (nextMilestoneToUnlock == null || currentlyNotifiedMilestone == nextMilestoneToUnlock) return;
        float distanceToNext = nextMilestoneToUnlock.distanceInMeters - currentDistance;
        float tenPercentOfGoal = nextMilestoneToUnlock.distanceInMeters * 0.1f;
        if (distanceToNext > 0 && distanceToNext <= tenPercentOfGoal)
        {
            float distanceToShow = (distanceToNext < 0.1f) ? 0.1f : distanceToNext;
            string message = $"Almost there! Just {distanceToShow:F1}m to the {nextMilestoneToUnlock.milestoneName}!";
            StartCoroutine(ShowAnimatedPanel(message, notificationDuration, false));
            currentlyNotifiedMilestone = nextMilestoneToUnlock;
        }
    }

    private IEnumerator ShowAnimatedPanel(string message, float duration, bool showConfetti)
    {
        if (notificationPanelPrefab == null || mainCanvas == null) yield break;

        isNotificationShowing = true;

        // 1. Create the UI panel instance
        GameObject panelInstance = Instantiate(notificationPanelPrefab, mainCanvas.transform);
        panelInstance.transform.SetAsFirstSibling();
        Animator animator = panelInstance.GetComponent<Animator>();
        TextMeshProUGUI textComponent = panelInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = message;
        }

        // --- NEW LOGIC: Spawn confetti after the panel slides in ---

        // Wait for the slide-in animation to finish (e.g., 0.5 seconds)
        yield return new WaitForSeconds(0.5f);

        if (showConfetti && confettiEffectPrefab != null)
        {
            // 2. Get the RectTransform of the UI panel we just created.
            RectTransform panelRect = panelInstance.GetComponent<RectTransform>();

            // 3. Get the main camera.
            Camera mainCamera = Camera.main;

            // 4. Convert the center of the UI panel's rectangle into a 3D world point.
            //    We tell it to place the point 10 units away from the camera.
            Vector3 spawnPos = mainCamera.ScreenToWorldPoint(new Vector3(panelRect.position.x, panelRect.position.y, 10));

            // 5. Instantiate the confetti at that calculated 3D position.
            Instantiate(confettiEffectPrefab, spawnPos, Quaternion.identity);
        }
        // --- END OF NEW LOGIC ---

        // 6. Wait for the rest of the specified duration.
        //    We subtract the 0.5s we already waited for the slide-in.
        yield return new WaitForSeconds(duration - 0.5f);

        // 7. Trigger the slide-out animation.
        if (animator != null)
        {
            animator.SetTrigger("Hide");
        }

        // 8. Wait for the slide-out animation to finish before destroying.
        yield return new WaitForSeconds(0.5f);

        // 9. Clean up the instance.
        Destroy(panelInstance);

        isNotificationShowing = false;
    }

    // All other methods are unchanged and correct
    #region Unchanged Methods
    private void Update() { if (milestoneScreenPanel.activeSelf && !isDragging && scrollRect.velocity.magnitude < snapVelocityThreshold) { FindClosestSnapPointAndSetTarget(); if (snapPositionsX.Count > 0 && currentSnapTargetIndex < snapPositionsX.Count) { Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y); contentPanel.anchoredPosition = Vector2.Lerp(contentPanel.anchoredPosition, targetPosition, Time.deltaTime * snapSpeed); } } }
    private void BuildMilestoneList() { foreach (var item in spawnedMilestoneItems) { Destroy(item); } spawnedMilestoneItems.Clear(); snapPositionsX.Clear(); if (MilestoneManager.Instance == null || milestoneItemPrefab == null) return; float itemWidth = milestoneItemPrefab.GetComponent<RectTransform>().rect.width; float currentXPosition = leftPadding; for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++) { Milestone milestone = MilestoneManager.Instance.SortedMilestones[i]; GameObject newItem = Instantiate(milestoneItemPrefab, contentPanel); MilestoneItemController itemController = newItem.GetComponent<MilestoneItemController>(); itemController.Setup(milestone, this); RectTransform newItemRect = newItem.GetComponent<RectTransform>(); newItemRect.anchorMin = new Vector2(0, 0.5f); newItemRect.anchorMax = new Vector2(0, 0.5f); newItemRect.pivot = new Vector2(0, 0.5f); newItemRect.anchoredPosition = new Vector2(currentXPosition, 0); Button collectButton = newItem.transform.Find("Collect_Button").GetComponent<Button>(); TextMeshProUGUI nameText = newItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>(); Image icon = newItem.transform.Find("Icon").GetComponent<Image>(); bool isUnlocked = MilestoneManager.Instance.IsMilestoneUnlocked(milestone); bool rewardCollected = MilestoneManager.Instance.HasRewardBeenCollected(milestone); if (isUnlocked) { icon.color = Color.white; string measurementWord = (milestone.measurementType == MilestoneType.Height) ? "Height" : "Length"; nameText.text = $"You reached the {measurementWord} of\n<b>{milestone.milestoneName}</b>"; collectButton.gameObject.SetActive(!rewardCollected); } else { icon.color = Color.black; nameText.text = milestone.milestoneName; collectButton.gameObject.SetActive(false); } newItem.name = $"Milestone_{milestone.milestoneName}"; float snapPosX = firstItemCenterPosX - (i * snapJumpValue); snapPositionsX.Add(snapPosX); currentXPosition += itemWidth + itemSpacing; newItem.transform.Find("DistanceText").GetComponent<TextMeshProUGUI>().text = $"{milestone.distanceInMeters} m"; icon.sprite = milestone.milestoneIcon; spawnedMilestoneItems.Add(newItem); } float totalContentWidth = currentXPosition - itemSpacing + rightPadding; contentPanel.sizeDelta = new Vector2(totalContentWidth, contentPanel.sizeDelta.y); }
    public void ToggleMilestoneScreen()
    {
        bool isOpening = !milestoneScreenPanel.activeSelf;

        if (isOpening)
        {
            // If we are opening the milestone screen, first make sure the shop is closed.
            shopManager?.CloseShopPanel(); // We just added this method to ShopManager
            ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.VisitMilestones);
            RefreshMilestoneList();
            SnapToLatestUnlockedMilestone();
        }

        milestoneScreenPanel.SetActive(isOpening);

        // Always update the central UI state
        UIStateManager.Instance?.SetUIBlockingState(isOpening);
        if (coinUIPanel != null && coinUINormalPosition != null && coinUIScreenPosition != null)
        {
            coinUIPanel.position = isOpening ? coinUIScreenPosition.position : coinUINormalPosition.position;
        }
    }
    public void CloseMilestoneScreen()
    {
        if (milestoneScreenPanel.activeSelf)
        {
            milestoneScreenPanel.SetActive(false);
        }
    }
    public void ShowUnlockNotification(string message)
    {
        // We can reuse the existing panel animation coroutine for this
        StartCoroutine(ShowAnimatedPanel(message, notificationDuration, false)); // false = no confetti
    }
    public void RefreshMilestoneList() { BuildMilestoneList(); }
    private void CheckForUncollectedRewards() { if (redDotNotification != null) { redDotNotification.SetActive(MilestoneManager.Instance.AreThereUncollectedRewards()); } }
    private void SetSnapPosition(int index, bool immediate = false) { if (snapPositionsX.Count == 0 || index < 0 || index >= snapPositionsX.Count) return; currentSnapTargetIndex = index; if (immediate) { Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y); contentPanel.anchoredPosition = targetPosition; } }
    private void SnapToLatestUnlockedMilestone() { int targetIndex = 0; int lastUnlockedIndex = -1; if (MilestoneManager.Instance != null) { for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++) { if (MilestoneManager.Instance.IsMilestoneUnlocked(MilestoneManager.Instance.SortedMilestones[i])) { lastUnlockedIndex = i; } } } if (lastUnlockedIndex != -1) { targetIndex = lastUnlockedIndex; } SetSnapPosition(targetIndex, true); }
    private void FindClosestSnapPointAndSetTarget() { if (snapPositionsX.Count == 0) return; float currentX = contentPanel.anchoredPosition.x; float minDistance = float.MaxValue; int closestIndex = 0; for (int i = 0; i < snapPositionsX.Count; i++) { float distance = Mathf.Abs(currentX - snapPositionsX[i]); if (distance < minDistance) { minDistance = distance; closestIndex = i; } } currentSnapTargetIndex = closestIndex; }
    public void OnBeginDrag(PointerEventData eventData) { isDragging = true; }
    public void OnEndDrag(PointerEventData eventData) { isDragging = false; }
    private void FindNextMilestone() { Milestone previousNext = nextMilestoneToUnlock; nextMilestoneToUnlock = null; if (MilestoneManager.Instance == null) return; foreach (var milestone in MilestoneManager.Instance.SortedMilestones) { if (!MilestoneManager.Instance.IsMilestoneUnlocked(milestone)) { nextMilestoneToUnlock = milestone; if (previousNext != nextMilestoneToUnlock) { currentlyNotifiedMilestone = null; } return; } } }
    private float GetCurrentMeters() { if (paperRoller == null || paperManager == null || paperManager.paperTileLength <= 0) return 0f; float worldDistance = paperRoller.WorldSpaceDistancePulled; float conversionFactor = paperManager.realWorldMetersPerTile / paperManager.paperTileLength; return worldDistance * conversionFactor; }
    #endregion
}