using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections;

public class MilestoneUIManager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("UI Components")]
    public GameObject milestoneScreenPanel;
    public GameObject milestoneItemPrefab;
    public ScrollRect scrollRect;
    private RectTransform contentPanel;
    public TextMeshProUGUI notificationText;

    [Header("Manual Layout & Snapping")]
    public float firstItemCenterPosX;
    public float snapJumpValue;
    public float snapSpeed = 10f;
    public float snapVelocityThreshold = 200f;
    public float itemSpacing = 50f;
    public float leftPadding = 20f;
    public float rightPadding = 20f;

    #region Private Variables
    [Header("Script References")]
    public PaperRoller paperRoller;
    public ContinuousPaperManager paperManager;
    private List<GameObject> spawnedMilestoneItems = new List<GameObject>();
    private List<float> snapPositionsX = new List<float>();
    private Milestone nextMilestoneToUnlock;
    private bool notificationIsShowing = false;
    private bool isDragging = false;
    private int currentSnapTargetIndex = 0;
    #endregion

    private void Start()
    {
        contentPanel = scrollRect.content;
        milestoneScreenPanel.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        if (paperRoller == null) paperRoller = FindFirstObjectByType<PaperRoller>();
        if (paperManager == null) paperManager = FindFirstObjectByType<ContinuousPaperManager>();

        // --- THIS IS THE CORRECTED RESET LOGIC ---
        // At the start of the game, this UI Manager takes responsibility for the reset.
        if (MilestoneManager.Instance != null)
        {
            MilestoneManager.Instance.ResetProgress();
        }
        if (paperRoller != null)
        {
            // Call the existing ResetPosition() method. No change needed in PaperRoller.cs.
            paperRoller.ResetPosition();
        }
        // --------------------------------------------------------------------------

        // These run after the reset, ensuring they have fresh data.
        BuildMilestoneList();
        FindNextMilestone();
    }

    // --- All other methods are correct and unchanged from the last working version ---
    #region Unchanged Methods
    private void Update()
    {
        if (milestoneScreenPanel.activeSelf && !isDragging && scrollRect.velocity.magnitude < snapVelocityThreshold)
        {
            FindClosestSnapPointAndSetTarget();
            if (snapPositionsX.Count > 0 && currentSnapTargetIndex < snapPositionsX.Count)
            {
                Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y);
                contentPanel.anchoredPosition = Vector2.Lerp(
                    contentPanel.anchoredPosition,
                    targetPosition,
                    Time.deltaTime * snapSpeed
                );
            }
        }
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
            newItem.name = $"Milestone_{milestone.milestoneName}";
            RectTransform newItemRect = newItem.GetComponent<RectTransform>();
            newItemRect.anchorMin = new Vector2(0, 0.5f);
            newItemRect.anchorMax = new Vector2(0, 0.5f);
            newItemRect.pivot = new Vector2(0, 0.5f);
            newItemRect.anchoredPosition = new Vector2(currentXPosition, 0);
            float snapPosX = firstItemCenterPosX - (i * snapJumpValue);
            snapPositionsX.Add(snapPosX);
            currentXPosition += itemWidth + itemSpacing;
            newItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = milestone.milestoneName;
            newItem.transform.Find("DistanceText").GetComponent<TextMeshProUGUI>().text = $"{milestone.distanceInMeters} m";
            Image icon = newItem.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = milestone.milestoneIcon;
            if (!MilestoneManager.Instance.IsMilestoneUnlocked(milestone)) { icon.color = Color.black; }
            spawnedMilestoneItems.Add(newItem);
        }
        float totalContentWidth = currentXPosition - itemSpacing + rightPadding;
        contentPanel.sizeDelta = new Vector2(totalContentWidth, contentPanel.sizeDelta.y);
    }
    private void SetSnapPosition(int index, bool immediate = false)
    {
        if (snapPositionsX.Count == 0 || index < 0 || index >= snapPositionsX.Count) return;
        currentSnapTargetIndex = index;
        if (immediate)
        {
            Vector2 targetPosition = new Vector2(snapPositionsX[currentSnapTargetIndex], contentPanel.anchoredPosition.y);
            contentPanel.anchoredPosition = targetPosition;
        }
    }
    private void SnapToLatestUnlockedMilestone()
    {
        int targetIndex = 0;
        int lastUnlockedIndex = -1;
        for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++)
        {
            if (MilestoneManager.Instance.IsMilestoneUnlocked(MilestoneManager.Instance.SortedMilestones[i])) { lastUnlockedIndex = i; }
        }
        if (lastUnlockedIndex != -1) { targetIndex = lastUnlockedIndex; }
        SetSnapPosition(targetIndex, true);
    }
    public void ToggleMilestoneScreen() { milestoneScreenPanel.SetActive(!milestoneScreenPanel.activeSelf); if (milestoneScreenPanel.activeSelf) { BuildMilestoneList(); SnapToLatestUnlockedMilestone(); } }
    private void FindClosestSnapPointAndSetTarget() { if (snapPositionsX.Count == 0) return; float currentX = contentPanel.anchoredPosition.x; float minDistance = float.MaxValue; int closestIndex = 0; for (int i = 0; i < snapPositionsX.Count; i++) { float distance = Mathf.Abs(currentX - snapPositionsX[i]); if (distance < minDistance) { minDistance = distance; closestIndex = i; } } currentSnapTargetIndex = closestIndex; }
    public void OnBeginDrag(PointerEventData eventData) { isDragging = true; }
    public void OnEndDrag(PointerEventData eventData) { isDragging = false; }
    private void LateUpdate() { if (!milestoneScreenPanel.activeSelf) { float currentDistance = GetCurrentMeters(); CheckForMilestoneUnlock(currentDistance); CheckForNearMilestone(currentDistance); } }
    private void FindNextMilestone() { nextMilestoneToUnlock = null; if (MilestoneManager.Instance == null) return; foreach (var milestone in MilestoneManager.Instance.SortedMilestones) { if (!MilestoneManager.Instance.IsMilestoneUnlocked(milestone)) { nextMilestoneToUnlock = milestone; return; } } }
    private void CheckForMilestoneUnlock(float currentDistance) { if (nextMilestoneToUnlock != null && currentDistance >= nextMilestoneToUnlock.distanceInMeters) { MilestoneManager.Instance.UnlockMilestone(nextMilestoneToUnlock); ShowNotification($"Milestone Reached!\n{nextMilestoneToUnlock.milestoneName}"); FindNextMilestone(); if (milestoneScreenPanel.activeSelf) BuildMilestoneList(); } }
    private void CheckForNearMilestone(float currentDistance) { if (nextMilestoneToUnlock == null || notificationIsShowing) return; float distanceToNext = nextMilestoneToUnlock.distanceInMeters - currentDistance; float notificationThreshold = nextMilestoneToUnlock.distanceInMeters * 0.1f; if (distanceToNext > 0 && distanceToNext <= notificationThreshold) { ShowNotification($"Almost there! Only {distanceToNext:F0}m to {nextMilestoneToUnlock.milestoneName}!"); } }
    private void ShowNotification(string message) { if (notificationIsShowing) return; StartCoroutine(NotificationCoroutine(message)); }
    private IEnumerator NotificationCoroutine(string message) { notificationIsShowing = true; notificationText.text = message; notificationText.gameObject.SetActive(true); yield return new WaitForSeconds(3f); notificationText.gameObject.SetActive(false); notificationIsShowing = false; }
    private float GetCurrentMeters() { if (paperRoller == null || paperManager == null || paperManager.paperTileLength <= 0) return 0f; float worldDistance = paperRoller.WorldSpaceDistancePulled; float conversionFactor = paperManager.realWorldMetersPerTile / paperManager.paperTileLength; return worldDistance * conversionFactor; }
    #endregion
}