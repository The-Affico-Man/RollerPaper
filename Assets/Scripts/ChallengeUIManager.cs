using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class ChallengeUIManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject challengesPanel;
    public Button openButton;
    public Button closeButton;

    [Header("Tabs")]
    public Button dailyTabButton;
    public Button weeklyTabButton;

    // --- THIS IS THE KEY FIX: We reference the parent ScrollView objects ---
    [Tooltip("Drag the 'DailyScrollView' GameObject here.")]
    public GameObject dailyScrollViewObject;
    [Tooltip("Drag the 'WeeklyScrollView' GameObject here.")]
    public GameObject weeklyScrollViewObject;
    // -------------------------------------------------------------------

    public Image dailyTabImage;
    public Image weeklyTabImage;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    [Header("Prefabs")]
    public GameObject challengeItemPrefab;

    [Header("Other UI Manager References")]
    public ShopManager shopManager;
    public MilestoneUIManager milestoneUIManager;

    // We will get the content transforms from the scroll views.
    private Transform dailyChallengesContent;
    private Transform weeklyChallengesContent;

    [Header("UI Interactivity")]
    [Tooltip("Drag the parent object of your coin counter UI here.")]
    public RectTransform coinUIPanel;
    [Tooltip("Drag the empty GameObject for the coin UI's normal position here.")]
    public Transform coinUINormalPosition;
    [Tooltip("Drag the empty GameObject for the coin UI's position when this screen is open.")]
    public Transform coinUIScreenPosition;
    [Tooltip("Drag the red dot notification image/object that is on your OpenChallengesButton here.")]
    public GameObject redDotNotification;

    [Header("Timers")]
    [Tooltip("The text element to display the daily reset countdown.")]
    public TextMeshProUGUI dailyTimerText;
    [Tooltip("The text element to display the weekly reset countdown.")]
    public TextMeshProUGUI weeklyTimerText;
    private void Start()
    {
        // Find the content transforms from our new references. This is more robust.
        if (dailyScrollViewObject != null)
        {
            dailyChallengesContent = dailyScrollViewObject.transform.Find("Viewport/Content");
        }
        if (weeklyScrollViewObject != null)
        {
            weeklyChallengesContent = weeklyScrollViewObject.transform.Find("Viewport/Content");
        }

        challengesPanel.SetActive(false);
        openButton.onClick.AddListener(TogglePanel);
        closeButton.onClick.AddListener(TogglePanel);
        dailyTabButton.onClick.AddListener(() => OpenTab(true));
        weeklyTabButton.onClick.AddListener(() => OpenTab(false));
        UpdateRedDot();
    }


    public void TogglePanel()
    {
        bool isOpening = !challengesPanel.activeSelf;
        if (isOpening)
        {
            shopManager?.CloseShopPanel();
            milestoneUIManager?.CloseMilestoneScreen();
            RefreshUI();
            OpenTab(true); // Default to daily tab
        }
        challengesPanel.SetActive(isOpening);
        UIStateManager.Instance?.SetUIBlockingState(isOpening);
        if (coinUIPanel != null && coinUINormalPosition != null && coinUIScreenPosition != null)
        {
            coinUIPanel.position = isOpening ? coinUIScreenPosition.position : coinUINormalPosition.position;
        }
    }

    // --- THIS METHOD IS NOW FIXED AND ROBUST ---
    private void OpenTab(bool isDaily)
    {
        // Toggle the parent ScrollView GameObjects directly.
        // This guarantees only one is active at a time, fixing all issues.
        if (dailyScrollViewObject != null) dailyScrollViewObject.SetActive(isDaily);
        if (weeklyScrollViewObject != null) weeklyScrollViewObject.SetActive(!isDaily);

        // Update tab visuals
        if (dailyTabImage != null) dailyTabImage.color = isDaily ? activeTabColor : inactiveTabColor;
        if (weeklyTabImage != null) weeklyTabImage.color = !isDaily ? activeTabColor : inactiveTabColor;
        if (dailyTimerText != null) dailyTimerText.gameObject.SetActive(isDaily);
        if (weeklyTimerText != null) weeklyTimerText.gameObject.SetActive(!isDaily);
    }

    public void RefreshUI()
    {
        if (ChallengeManager.Instance == null) return;
        if (dailyChallengesContent != null)
        {
            PopulateChallengeList(dailyChallengesContent, ChallengeManager.Instance.ActiveDailies);
        }
        if (weeklyChallengesContent != null)
        {
            PopulateChallengeList(weeklyChallengesContent, ChallengeManager.Instance.ActiveWeeklies);
        }
    }

    public void CloseChallengePanel()
    {
        if (challengesPanel.activeSelf)
        {
            challengesPanel.SetActive(false);
            if (coinUIPanel != null && coinUINormalPosition != null)
            {
                coinUIPanel.position = coinUINormalPosition.position;
            }
        }
    }
    void Update()
    {
        UpdateRedDot();
        if (challengesPanel.activeSelf)
        {
            UpdateTimers();
        }
    }
    private void UpdateTimers()
    {
        if (TimeManager.Instance == null) return;

        // Update Daily Timer
        if (dailyTimerText != null)
        {
            TimeSpan dailyTimeLeft = TimeManager.Instance.GetTimeUntilNextDailyReset();
            dailyTimerText.text = $"New Dailies in: {dailyTimeLeft.Hours:D2}:{dailyTimeLeft.Minutes:D2}:{dailyTimeLeft.Seconds:D2}";
        }

        // Update Weekly Timer
        if (weeklyTimerText != null)
        {
            TimeSpan weeklyTimeLeft = TimeManager.Instance.GetTimeUntilNextWeeklyReset();
            // Show days only if more than 1 day is left
            if (weeklyTimeLeft.TotalDays >= 1)
            {
                weeklyTimerText.text = $"New Weeklies in: {weeklyTimeLeft.Days}d {weeklyTimeLeft.Hours:D2}h";
            }
            else
            {
                weeklyTimerText.text = $"New Weeklies in: {weeklyTimeLeft.Hours:D2}:{weeklyTimeLeft.Minutes:D2}:{weeklyTimeLeft.Seconds:D2}";
            }
        }
    }
    private void UpdateRedDot()
    {
        if (redDotNotification != null && ChallengeManager.Instance != null)
        {
            // The dot is active if the ChallengeManager reports there are unclaimed rewards.
            redDotNotification.SetActive(ChallengeManager.Instance.AreThereUnclaimedRewards());
        }
    }
    // This method is now correct because it receives the correct content parent.
    private void PopulateChallengeList(Transform contentParent, List<ChallengeState> challengeStates)
    {
        // Clear old items first.
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (challengeStates == null) return;

        // Sort the list to show claimable, then in-progress, then claimed challenges.
        var sortedStates = challengeStates
            .OrderBy(s => ChallengeManager.Instance.IsRewardClaimed(s.challenge)) // Claimed (true=1) are last
            .ThenByDescending(s => s.IsComplete()); // Completed (true=1) are first among unclaimed

        foreach (ChallengeState state in sortedStates)
        {
            GameObject itemGO = Instantiate(challengeItemPrefab, contentParent);

            // Get the helper scripts from the prefab.
            ChallengeItemUI uiItem = itemGO.GetComponent<ChallengeItemUI>();
            ChallengeItemController itemController = itemGO.GetComponent<ChallengeItemController>(); // It should be on the root.

            if (uiItem == null || itemController == null)
            {
                Debug.LogError("ChallengeItem_Prefab is missing the ChallengeItemUI or ChallengeItemController script!", itemGO);
                continue;
            }

            // --- THE FIX: Set up the controller with its data ---
            itemController.Setup(state, this);
            // ---------------------------------------------------

            // Populate the UI using safe references from ChallengeItemUI.
            uiItem.descriptionText.text = state.challenge.description.Replace("{goal}", $"{state.challenge.goal:N0}");
            float displayProgress = Mathf.Min(state.progress, state.challenge.goal);
            uiItem.progressText.text = $"{displayProgress:N0} / {state.challenge.goal:N0}";
            uiItem.progressBar.maxValue = state.challenge.goal;
            uiItem.progressBar.value = displayProgress;

            bool isComplete = state.IsComplete();
            bool isClaimed = ChallengeManager.Instance.IsRewardClaimed(state.challenge);

            if (isClaimed)
            {
                // --- CLAIMED STATE ---
                uiItem.progressBar.gameObject.SetActive(false);
                uiItem.progressText.gameObject.SetActive(false);
                uiItem.claimButtonObject.SetActive(true);
                uiItem.claimButton.interactable = false;
                uiItem.claimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Claimed";
                if (uiItem.completedOverlay != null) uiItem.completedOverlay.SetActive(true);
            }
            else if (isComplete)
            {
                // --- COMPLETED, READY TO CLAIM STATE ---
                uiItem.progressBar.gameObject.SetActive(false);
                uiItem.progressText.gameObject.SetActive(false);
                uiItem.claimButtonObject.SetActive(true);
                uiItem.claimButton.interactable = true;
                uiItem.claimButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Claim\n<size=70%>{state.challenge.coinReward} Coins";
                if (uiItem.completedOverlay != null) uiItem.completedOverlay.SetActive(false);

                // The button now calls its own controller's method.
                uiItem.claimButton.onClick.RemoveAllListeners();
                uiItem.claimButton.onClick.AddListener(itemController.OnClaimButtonPressed);
            }
            else
            {
                // --- IN-PROGRESS STATE ---
                uiItem.progressBar.gameObject.SetActive(true);
                uiItem.progressText.gameObject.SetActive(true);
                uiItem.claimButtonObject.SetActive(false);
                if (uiItem.completedOverlay != null) uiItem.completedOverlay.SetActive(false);
            }
        }
    }
}