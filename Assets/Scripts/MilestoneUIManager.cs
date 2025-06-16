using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class MilestoneUIManager : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject milestoneScreenPanel;
    public GameObject milestoneItemPrefab;
    public ScrollRect scrollRect;
    private RectTransform contentPanel;
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI celebrationText;
    [Header("Notification Settings")]
    public float celebrationDuration = 4f;
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
    private bool notificationIsShowing = false;
    private bool celebrationIsShowing = false;
    private bool isDragging = false;
    private int currentSnapTargetIndex = 0;
    private Milestone currentlyNotifiedMilestone = null;
    [Header("Script References")]
    // ... your other variables
    //private int currentSnapTargetIndex = 0;

    // --- ADD THIS NEW VARIABLE ---
    private bool isFirstOpen = true;

    private void Start()
    {
        contentPanel = scrollRect.content;
        milestoneScreenPanel.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        if (celebrationText != null) celebrationText.gameObject.SetActive(false);

        if (paperRoller == null) paperRoller = FindFirstObjectByType<PaperRoller>();
        if (paperManager == null) paperManager = FindFirstObjectByType<ContinuousPaperManager>();

        if (MilestoneManager.Instance != null) { MilestoneManager.Instance.ResetProgress(); }
        if (paperRoller != null) { paperRoller.ResetPosition(); }

        FindNextMilestone();
    }
    // THE NEW, CORRECT Update METHOD
    private void Update()
    {
        // --- THIS IS THE FIX ---
       
    }
    // --- END OF FIX ---
    private void LateUpdate()
    {
        // Only check for progress when the main game is active (panel is closed).
        if (!milestoneScreenPanel.activeSelf)
        {
            float currentDistance = GetCurrentMeters();
            CheckForMilestoneUnlock(currentDistance);
            CheckForNearMilestone(currentDistance);
        }
    }

    private void BuildMilestoneList()
    {
        // 1. Clear everything to start fresh.
        foreach (Transform child in contentPanel) { Destroy(child.gameObject); }
        spawnedMilestoneItems.Clear();
        snapPositionsX.Clear();

        if (MilestoneManager.Instance == null || milestoneItemPrefab == null) return;

        // 2. Get the full, sorted list of milestones.
        List<Milestone> allSortedMilestones = MilestoneManager.Instance.SortedMilestones;
        if (allSortedMilestones.Count == 0) return;

        // 3. Get the width of a single item prefab.
        float itemWidth = milestoneItemPrefab.GetComponent<RectTransform>().rect.width;
        float currentXPosition = leftPadding;

        // 4. Loop through EVERY milestone and place it, calculating its snap position.
        for (int i = 0; i < allSortedMilestones.Count; i++)
        {
            Milestone milestone = allSortedMilestones[i];

            GameObject newItem = Instantiate(milestoneItemPrefab, contentPanel);
            newItem.name = $"Milestone_{milestone.milestoneName}";
            RectTransform newItemRect = newItem.GetComponent<RectTransform>();

            // Set anchor and pivot for predictable positioning.
            newItemRect.anchorMin = new Vector2(0, 0.5f);
            newItemRect.anchorMax = new Vector2(0, 0.5f);
            newItemRect.pivot = new Vector2(0, 0.5f);

            // Place the item at the current position.
            newItemRect.anchoredPosition = new Vector2(currentXPosition, 0);

            // Calculate and store the snap position for this item.
            float snapPosX = firstItemCenterPosX - (i * snapJumpValue);
            snapPositionsX.Add(snapPosX);

            // Set up the next item's position.
            currentXPosition += itemWidth + itemSpacing;

            // Set the text and icon visuals.
            #region Text and Icon Setup
            TextMeshProUGUI nameText = newItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            Image icon = newItem.transform.Find("Icon").GetComponent<Image>();
            if (MilestoneManager.Instance.IsMilestoneUnlocked(milestone))
            {
                string measurementWord = (milestone.measurementType == MilestoneType.Height) ? "Height" : "Length";
                nameText.text = $"You reached the {measurementWord} of\n<b>{milestone.milestoneName}</b>";
                icon.color = Color.white;
            }
            else
            {
                nameText.text = milestone.milestoneName;
                icon.color = Color.black;
            }
            newItem.transform.Find("DistanceText").GetComponent<TextMeshProUGUI>().text = $"{milestone.distanceInMeters} m";
            icon.sprite = milestone.milestoneIcon;
            #endregion

            spawnedMilestoneItems.Add(newItem);
        }

        // --- 5. THIS IS THE NEW, FOOLPROOF WIDTH CALCULATION ---
        // The total width is simply the position of the last item's left edge,
        // plus its own width, plus the final padding on the right.
        float totalContentWidth = spawnedMilestoneItems.Last().GetComponent<RectTransform>().anchoredPosition.x
                                + itemWidth
                                + rightPadding;

        contentPanel.sizeDelta = new Vector2(totalContentWidth, contentPanel.sizeDelta.y);
        // --------------------------------------------------------
    }

    // THE NEW, CORRECT ToggleMilestoneScreen() METHOD
    public void ToggleMilestoneScreen()
    {
        bool isNowActive = !milestoneScreenPanel.activeSelf;
        milestoneScreenPanel.SetActive(isNowActive);

        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.SetUIBlockingState(isNowActive);
        }

        // This logic only runs when the panel is opened.
        if (isNowActive)
        {
            // First, rebuild the list to show the most up-to-date unlock statuses.
            BuildMilestoneList();

            // --- THIS IS THE KEY ---
            // Second, find the correct starting milestone and instantly snap to it.
            SnapToLatestUnlockedMilestone();
            // -----------------------
        }
    }

    #region Notification Logic
    private void FindNextMilestone()
    {
        Milestone previousNext = nextMilestoneToUnlock;
        nextMilestoneToUnlock = null;
        if (MilestoneManager.Instance == null) return;
        foreach (var milestone in MilestoneManager.Instance.SortedMilestones)
        {
            if (!MilestoneManager.Instance.IsMilestoneUnlocked(milestone))
            {
                nextMilestoneToUnlock = milestone;
                // If the goal has changed, allow a new "almost there" notification.
                if (previousNext != nextMilestoneToUnlock)
                {
                    currentlyNotifiedMilestone = null;
                }
                return;
            }
        }
    }

    private void CheckForMilestoneUnlock(float currentDistance)
    {
        if (nextMilestoneToUnlock != null && currentDistance >= nextMilestoneToUnlock.distanceInMeters)
        {
            Milestone unlockedMilestone = nextMilestoneToUnlock;
            MilestoneManager.Instance.UnlockMilestone(unlockedMilestone);
            ShowCelebration(unlockedMilestone);
            FindNextMilestone();
        }
    }

    private void CheckForNearMilestone(float currentDistance)
    {
        if (nextMilestoneToUnlock == null || notificationIsShowing || celebrationIsShowing || currentlyNotifiedMilestone == nextMilestoneToUnlock) return;

        float distanceToNext = nextMilestoneToUnlock.distanceInMeters - currentDistance;
        float notificationThreshold = nextMilestoneToUnlock.distanceInMeters * 0.1f;

        if (distanceToNext > 0 && distanceToNext <= notificationThreshold)
        {
            float distanceToShow = (distanceToNext < 0.1f) ? 0.1f : distanceToNext;
            ShowNotification($"Almost there! Just {distanceToShow:F1}m to the {nextMilestoneToUnlock.milestoneName}!");
            currentlyNotifiedMilestone = nextMilestoneToUnlock;
        }
    }

    private void ShowCelebration(Milestone milestone)
    {
        if (celebrationText == null) return;
        StartCoroutine(CelebrationCoroutine(milestone));
    }

    private IEnumerator CelebrationCoroutine(Milestone milestone)
    {
        celebrationIsShowing = true;
        if (notificationText != null) { notificationText.gameObject.SetActive(false); }
        celebrationText.text = $"MILESTONE REACHED!\n<size=80%>{milestone.milestoneName}";
        celebrationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(celebrationDuration);
        celebrationText.gameObject.SetActive(false);
        celebrationIsShowing = false;
    }

    private void ShowNotification(string message, float duration = 3f)
    {
        if (notificationIsShowing || notificationText == null || celebrationIsShowing) return;
        StartCoroutine(NotificationCoroutine(message, duration));
    }

    private IEnumerator NotificationCoroutine(string message, float duration)
    {
        notificationIsShowing = true;
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        notificationText.gameObject.SetActive(false);
        notificationIsShowing = false;
    }

    private float GetCurrentMeters()
    {
        if (paperRoller == null || paperManager == null || paperManager.paperTileLength <= 0) return 0f;
        float worldDistance = paperRoller.WorldSpaceDistancePulled;
        float conversionFactor = paperManager.realWorldMetersPerTile / paperManager.paperTileLength;
        return worldDistance * conversionFactor;
    }
    private void SnapToLatestUnlockedMilestone()
    {
        int targetIndex = 0; // Default to the first milestone (index 0)
        int lastUnlockedIndex = -1;

        // Find the index of the last unlocked milestone in the list
        if (MilestoneManager.Instance != null)
        {
            for (int i = 0; i < MilestoneManager.Instance.SortedMilestones.Count; i++)
            {
                if (MilestoneManager.Instance.IsMilestoneUnlocked(MilestoneManager.Instance.SortedMilestones[i]))
                {
                    lastUnlockedIndex = i;
                }
            }
        }

        // If we found any unlocked milestone, that's our target.
        if (lastUnlockedIndex != -1)
        {
            targetIndex = lastUnlockedIndex;
        }

        // Use your existing method to instantly set the position.
        SetSnapPosition(targetIndex, true);
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

    #endregion
}