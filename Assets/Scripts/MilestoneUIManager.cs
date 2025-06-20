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

    [Header("Notification System")]
    [Tooltip("Drag your new NotificationPanel prefab here.")]
    public GameObject notificationPanelPrefab;
    // --- THIS IS THE FIX (Part 1): Add a reference to the main canvas ---
    [Tooltip("Drag your main UI Canvas object from the scene hierarchy here.")]
    public Canvas mainCanvas;
    [Tooltip("How long the notification stays on screen before sliding out.")]
    public float notificationDuration = 3f;
    [Tooltip("How long the celebration stays on screen before sliding out.")]
    public float celebrationDuration = 4f;

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
            StartCoroutine(ShowAnimatedPanel(message, celebrationDuration));
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
            StartCoroutine(ShowAnimatedPanel(message, notificationDuration));
            currentlyNotifiedMilestone = nextMilestoneToUnlock;
        }
    }

    private IEnumerator ShowAnimatedPanel(string message, float duration)
    {
        // --- THIS IS THE FIX (Part 2): Add safety checks and use the canvas ---
        if (notificationPanelPrefab == null)
        {
            Debug.LogError("Notification Panel Prefab is not assigned!", this.gameObject);
            yield break;
        }
        if (mainCanvas == null)
        {
            Debug.LogError("Main Canvas is not assigned!", this.gameObject);
            yield break;
        }

        isNotificationShowing = true;

        // 1. Create an instance of the panel prefab AS A CHILD OF THE CANVAS
        GameObject panelInstance = Instantiate(notificationPanelPrefab, mainCanvas.transform);

        // 2. Set it to be the first child, so it renders BEHIND other UI elements.
        panelInstance.transform.SetAsFirstSibling();

        // 3. Get its components
        Animator animator = panelInstance.GetComponent<Animator>();
        TextMeshProUGUI textComponent = panelInstance.GetComponentInChildren<TextMeshProUGUI>();

        // 4. Set the message
        if (textComponent != null)
        {
            textComponent.text = message;
        }

        // 5. Wait for the specified duration while it's on-screen
        yield return new WaitForSeconds(duration);

        // 6. Trigger the slide-out animation
        if (animator != null)
        {
            animator.SetTrigger("Hide");
        }

        // 7. Wait for the slide-out animation to finish before destroying
        yield return new WaitForSeconds(0.5f);

        // 8. Clean up the instance
        Destroy(panelInstance);

        isNotificationShowing = false;
    }

    // All other methods are unchanged and correct
    #region Unchanged Methods
    private void Update() { if (milestoneScreenPanel.activeSelf && !isDragging && scrollRect.velocity.magnitude < snapVelocityThreshold) { FindClosestSnapPointAndSetTarget(); if (snapPositionsX.Count > 0 && currentSnapTargetIndex < snapPositionsX.Count) { Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y); contentPanel.anchoredPosition = Vector2.Lerp(contentPanel.anchoredPosition, targetPosition, Time.deltaTime * snapSpeed); } } }
    private void BuildMilestoneList() { foreach (var item in spawnedMilestoneItems) { Destroy(item); } spawnedMilestoneItems.Clear(); snapPositionsX.Clear(); if (MilestoneManager.Instance == null || milestoneItemPrefab == null) return; float itemWidth = milestoneItemPrefab.GetComponent<RectTransform>().rect.width; float currentXPosition = leftPadding; for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++) { Milestone milestone = MilestoneManager.Instance.SortedMilestones[i]; GameObject newItem = Instantiate(milestoneItemPrefab, contentPanel); MilestoneItemController itemController = newItem.GetComponent<MilestoneItemController>(); itemController.Setup(milestone, this); RectTransform newItemRect = newItem.GetComponent<RectTransform>(); newItemRect.anchorMin = new Vector2(0, 0.5f); newItemRect.anchorMax = new Vector2(0, 0.5f); newItemRect.pivot = new Vector2(0, 0.5f); newItemRect.anchoredPosition = new Vector2(currentXPosition, 0); Button collectButton = newItem.transform.Find("Collect_Button").GetComponent<Button>(); TextMeshProUGUI nameText = newItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>(); Image icon = newItem.transform.Find("Icon").GetComponent<Image>(); bool isUnlocked = MilestoneManager.Instance.IsMilestoneUnlocked(milestone); bool rewardCollected = MilestoneManager.Instance.HasRewardBeenCollected(milestone); if (isUnlocked) { icon.color = Color.white; string measurementWord = (milestone.measurementType == MilestoneType.Height) ? "Height" : "Length"; nameText.text = $"You reached the {measurementWord} of\n<b>{milestone.milestoneName}</b>"; collectButton.gameObject.SetActive(!rewardCollected); } else { icon.color = Color.black; nameText.text = milestone.milestoneName; collectButton.gameObject.SetActive(false); } newItem.name = $"Milestone_{milestone.milestoneName}"; float snapPosX = firstItemCenterPosX - (i * snapJumpValue); snapPositionsX.Add(snapPosX); currentXPosition += itemWidth + itemSpacing; newItem.transform.Find("DistanceText").GetComponent<TextMeshProUGUI>().text = $"{milestone.distanceInMeters} m"; icon.sprite = milestone.milestoneIcon; spawnedMilestoneItems.Add(newItem); } float totalContentWidth = currentXPosition - itemSpacing + rightPadding; contentPanel.sizeDelta = new Vector2(totalContentWidth, contentPanel.sizeDelta.y); }
    public void ToggleMilestoneScreen() { bool isNowActive = !milestoneScreenPanel.activeSelf; milestoneScreenPanel.SetActive(isNowActive); if (isNowActive) { RefreshMilestoneList(); SnapToLatestUnlockedMilestone(); } if (UIStateManager.Instance != null) { UIStateManager.Instance.SetUIBlockingState(isNowActive); } }
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