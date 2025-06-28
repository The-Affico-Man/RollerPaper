using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
        }
    }

    // This method is now correct because it receives the correct content parent.
    private void PopulateChallengeList(Transform contentParent, List<ChallengeState> challengeStates)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        if (challengeStates == null) return;

        var sortedStates = challengeStates
            .OrderBy(s => ChallengeManager.Instance.IsRewardClaimed(s.challenge))
            .ThenByDescending(s => s.IsComplete());

        foreach (ChallengeState state in sortedStates)
        {
            GameObject itemGO = Instantiate(challengeItemPrefab, contentParent);
            ChallengeItemUI uiItem = itemGO.GetComponent<ChallengeItemUI>();

            if (uiItem == null)
            {
                Debug.LogError("ChallengeItem_Prefab is missing the ChallengeItemUI script!", itemGO);
                continue;
            }

            uiItem.descriptionText.text = state.challenge.description.Replace("{goal}", $"{state.challenge.goal:N0}");
            float displayProgress = Mathf.Min(state.progress, state.challenge.goal);
            uiItem.progressText.text = $"{displayProgress:N0} / {state.challenge.goal:N0}";
            uiItem.progressBar.maxValue = state.challenge.goal;
            uiItem.progressBar.value = displayProgress;

            bool isComplete = state.IsComplete();
            bool isClaimed = ChallengeManager.Instance.IsRewardClaimed(state.challenge);

            if (isClaimed)
            {
                uiItem.progressBar.gameObject.SetActive(false);
                uiItem.progressText.gameObject.SetActive(false);
                uiItem.claimButtonObject.SetActive(true);
                uiItem.claimButton.interactable = false;
                uiItem.claimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Claimed";
                if (uiItem.completedOverlay != null) uiItem.completedOverlay.SetActive(true);
            }
            else if (isComplete)
            {
                uiItem.progressBar.gameObject.SetActive(false);
                uiItem.progressText.gameObject.SetActive(false);
                uiItem.claimButtonObject.SetActive(true);
                uiItem.claimButton.interactable = true;
                uiItem.claimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Claim";
                if (uiItem.completedOverlay != null) uiItem.completedOverlay.SetActive(false);

                uiItem.claimButton.onClick.RemoveAllListeners();
                uiItem.claimButton.onClick.AddListener(() => {
                    ChallengeManager.Instance.ClaimReward(state.challenge);
                    RefreshUI();
                });
            }
            else
            {
                uiItem.progressBar.gameObject.SetActive(true);
                uiItem.progressText.gameObject.SetActive(true);
                uiItem.claimButtonObject.SetActive(false);
                if (uiItem.completedOverlay != null) uiItem.completedOverlay.SetActive(false);
            }
        }
    }
}