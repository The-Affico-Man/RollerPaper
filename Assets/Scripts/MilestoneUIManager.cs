using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections;

public class MilestoneUIManager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    // ... all variables are correct ...
    #region Unchanged Variables
    [Header("UI Components")]
    public GameObject milestoneScreenPanel;
    public GameObject milestoneItemPrefab;
    public ScrollRect scrollRect;
    private RectTransform contentPanel;
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI celebrationText;
    public GameObject redDotNotification;
    [Header("Manual Layout & Snapping")]
    public float firstItemCenterPosX;
    public float snapJumpValue;
    public float snapSpeed = 10f;
    public float snapVelocityThreshold = 200f;
    public float itemSpacing = 50f;
    public float leftPadding = 20f;
    public float rightPadding = 20f;
    [Header("Notification Settings")]
    public float celebrationDuration = 4f;
    [Header("Script References")]
    public PaperRoller paperRoller;
    public ContinuousPaperManager paperManager;
    private List<GameObject> spawnedMilestoneItems = new List<GameObject>();
    private List<float> snapPositionsX = new List<float>();
    private Milestone nextMilestoneToUnlock;
    private bool notificationIsShowing = false;
    private bool celebrationIsShowing = false;
    private bool isDragging = false;
    private int currentSnapTargetIndex = 0;
    private Milestone currentlyNotifiedMilestone = null;
    #endregion

    private void Start()
    {
        contentPanel = scrollRect.content;
        milestoneScreenPanel.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        if (celebrationText != null) celebrationText.gameObject.SetActive(false);
        if (redDotNotification != null) redDotNotification.SetActive(false);
        if (paperRoller == null) paperRoller = FindFirstObjectByType<PaperRoller>();
        if (paperManager == null) paperManager = FindFirstObjectByType<ContinuousPaperManager>();
        if (MilestoneManager.Instance != null) { MilestoneManager.Instance.ResetProgress(); }

        // --- THIS IS THE FIX ---
        if (paperRoller != null)
        {
            paperRoller.ResetPosition(); // Changed from ResetState()
        }
        // --- END OF FIX ---

        FindNextMilestone();
    }

    private void BuildMilestoneList()
    {
        foreach (var item in spawnedMilestoneItems) { Destroy(item); }
        spawnedMilestoneItems.Clear();
        snapPositionsX.Clear();
        if (MilestoneManager.Instance == null || milestoneItemPrefab == null) return;
        float itemWidth = milestoneItemPrefab.GetComponent<RectTransform>().rect.width;
        float currentXPosition = leftPadding;

        for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++)
        {
            Milestone milestone = MilestoneManager.Instance.SortedMilestones[i];
            GameObject newItem = Instantiate(milestoneItemPrefab, contentPanel);

            MilestoneItemController itemController = newItem.GetComponent<MilestoneItemController>();
            itemController.Setup(milestone, this);

            // --- THIS IS THE FIX ---
            // This ensures consistent positioning.
            RectTransform newItemRect = newItem.GetComponent<RectTransform>();
            newItemRect.anchorMin = new Vector2(0, 0.5f);
            newItemRect.anchorMax = new Vector2(0, 0.5f);
            newItemRect.pivot = new Vector2(0, 0.5f);
            newItemRect.anchoredPosition = new Vector2(currentXPosition, 0);
            // --- END OF FIX ---

            #region UI State Setup
            Button collectButton = newItem.transform.Find("Collect_Button").GetComponent<Button>();
            TextMeshProUGUI nameText = newItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            Image icon = newItem.transform.Find("Icon").GetComponent<Image>();
            bool isUnlocked = MilestoneManager.Instance.IsMilestoneUnlocked(milestone);
            bool rewardCollected = MilestoneManager.Instance.HasRewardBeenCollected(milestone);
            if (isUnlocked)
            {
                icon.color = Color.white;
                string measurementWord = (milestone.measurementType == MilestoneType.Height) ? "Height" : "Length";
                nameText.text = $"You reached the {measurementWord} of\n<b>{milestone.milestoneName}</b>";
                collectButton.gameObject.SetActive(!rewardCollected);
            }
            else
            {
                icon.color = Color.black;
                nameText.text = milestone.milestoneName;
                collectButton.gameObject.SetActive(false);
            }
            #endregion

            #region Unchanged Positioning & Data
            newItem.name = $"Milestone_{milestone.milestoneName}";
            float snapPosX = firstItemCenterPosX - (i * snapJumpValue);
            snapPositionsX.Add(snapPosX);
            currentXPosition += itemWidth + itemSpacing;
            newItem.transform.Find("DistanceText").GetComponent<TextMeshProUGUI>().text = $"{milestone.distanceInMeters} m";
            icon.sprite = milestone.milestoneIcon;
            spawnedMilestoneItems.Add(newItem);
            #endregion
        }
        float totalContentWidth = currentXPosition - itemSpacing + rightPadding;
        contentPanel.sizeDelta = new Vector2(totalContentWidth, contentPanel.sizeDelta.y);
    }

    public void ToggleMilestoneScreen()
    {
        bool isNowActive = !milestoneScreenPanel.activeSelf;
        milestoneScreenPanel.SetActive(isNowActive);

        // --- THIS IS THE FIX ---
        if (isNowActive)
        {
            RefreshMilestoneList(); // Calls the correct method
            SnapToLatestUnlockedMilestone();
        }
        // --- END OF FIX ---

        if (UIStateManager.Instance != null) { UIStateManager.Instance.SetUIBlockingState(isNowActive); }
    }

    // --- ADD THIS METHOD BACK ---
    public void RefreshMilestoneList()
    {
        BuildMilestoneList();
    }
    // -------------------------

    // The rest of the script is correct.
    #region Unchanged Code
    private void Update() { if (milestoneScreenPanel.activeSelf && !isDragging && scrollRect.velocity.magnitude < snapVelocityThreshold) { FindClosestSnapPointAndSetTarget(); if (snapPositionsX.Count > 0 && currentSnapTargetIndex < snapPositionsX.Count) { Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y); contentPanel.anchoredPosition = Vector2.Lerp(contentPanel.anchoredPosition, targetPosition, Time.deltaTime * snapSpeed); } } }
    private void LateUpdate() { float currentDistance = GetCurrentMeters(); CheckForMilestoneUnlock(currentDistance); CheckForNearMilestone(currentDistance); CheckForUncollectedRewards(); }
    private void CheckForUncollectedRewards() { if (redDotNotification != null) { redDotNotification.SetActive(MilestoneManager.Instance.AreThereUncollectedRewards()); } }
    private void SetSnapPosition(int index, bool immediate = false) { if (snapPositionsX.Count == 0 || index < 0 || index >= snapPositionsX.Count) return; currentSnapTargetIndex = index; if (immediate) { Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y); contentPanel.anchoredPosition = targetPosition; } }
    private void SnapToLatestUnlockedMilestone() { int targetIndex = 0; int lastUnlockedIndex = -1; if (MilestoneManager.Instance != null) { for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++) { if (MilestoneManager.Instance.IsMilestoneUnlocked(MilestoneManager.Instance.SortedMilestones[i])) { lastUnlockedIndex = i; } } } if (lastUnlockedIndex != -1) { targetIndex = lastUnlockedIndex; } SetSnapPosition(targetIndex, true); }
    private void FindClosestSnapPointAndSetTarget() { if (snapPositionsX.Count == 0) return; float currentX = contentPanel.anchoredPosition.x; float minDistance = float.MaxValue; int closestIndex = 0; for (int i = 0; i < snapPositionsX.Count; i++) { float distance = Mathf.Abs(currentX - snapPositionsX[i]); if (distance < minDistance) { minDistance = distance; closestIndex = i; } } currentSnapTargetIndex = closestIndex; }
    public void OnBeginDrag(PointerEventData eventData) { isDragging = true; }
    public void OnEndDrag(PointerEventData eventData) { isDragging = false; }
    private void FindNextMilestone() { Milestone previousNext = nextMilestoneToUnlock; nextMilestoneToUnlock = null; if (MilestoneManager.Instance == null) return; foreach (var milestone in MilestoneManager.Instance.SortedMilestones) { if (!MilestoneManager.Instance.IsMilestoneUnlocked(milestone)) { nextMilestoneToUnlock = milestone; if (previousNext != nextMilestoneToUnlock) { currentlyNotifiedMilestone = null; } return; } } }
    private void CheckForMilestoneUnlock(float currentDistance) { if (nextMilestoneToUnlock != null && currentDistance >= nextMilestoneToUnlock.distanceInMeters) { Milestone unlockedMilestone = nextMilestoneToUnlock; MilestoneManager.Instance.UnlockMilestone(unlockedMilestone); ShowCelebration(unlockedMilestone); FindNextMilestone(); } }
    private void CheckForNearMilestone(float currentDistance) { if (nextMilestoneToUnlock == null || notificationIsShowing || celebrationIsShowing || currentlyNotifiedMilestone == nextMilestoneToUnlock) return; float distanceToNext = nextMilestoneToUnlock.distanceInMeters - currentDistance; float tenPercentOfGoal = nextMilestoneToUnlock.distanceInMeters * 0.1f; if (distanceToNext > 0 && distanceToNext <= tenPercentOfGoal) { float distanceToShow = (distanceToNext < 0.1f) ? 0.1f : distanceToNext; ShowNotification($"Almost there! Just {distanceToShow:F1}m to the {nextMilestoneToUnlock.milestoneName}!"); currentlyNotifiedMilestone = nextMilestoneToUnlock; } }
    private void ShowCelebration(Milestone milestone) { if (celebrationText == null) return; StartCoroutine(CelebrationCoroutine(milestone)); }
    private IEnumerator CelebrationCoroutine(Milestone milestone) { celebrationIsShowing = true; if (notificationText != null) { notificationText.gameObject.SetActive(false); } celebrationText.text = $"MILESTONE REACHED!\n<size=80%>{milestone.milestoneName}"; celebrationText.gameObject.SetActive(true); yield return new WaitForSeconds(celebrationDuration); celebrationText.gameObject.SetActive(false); celebrationIsShowing = false; }
    private void ShowNotification(string message, float duration = 3f) { if (notificationIsShowing || notificationText == null || celebrationIsShowing) return; StartCoroutine(NotificationCoroutine(message, duration)); }
    private IEnumerator NotificationCoroutine(string message, float duration) { notificationIsShowing = true; notificationText.text = message; notificationText.gameObject.SetActive(true); yield return new WaitForSeconds(duration); notificationText.gameObject.SetActive(false); notificationIsShowing = false; }
    private float GetCurrentMeters() { if (paperRoller == null || paperManager == null || paperManager.paperTileLength <= 0) return 0f; float worldDistance = paperRoller.WorldSpaceDistancePulled; float conversionFactor = paperManager.realWorldMetersPerTile / paperManager.paperTileLength; return worldDistance * conversionFactor; }
    #endregion
}