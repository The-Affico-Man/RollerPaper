using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    [Header("Core Components")]
    public GameObject shopPanel;
    public Button pawsTabButton;
    public Button paperTabButton;

    [Header("Content Panels")]
    public GameObject pawsTabScrollView;
    public GameObject paperTabScrollView;
    public Transform pawsContentParent;
    public Transform paperContentParent;

    [Header("Prefabs")]
    public GameObject shopItemPrefab;
    public GameObject warningTextPrefab;
    public Canvas mainCanvas;

    [Header("Other UI Manager References")]
    [Tooltip("Drag the GameObject that has the MilestoneUIManager script on it here.")]
    public MilestoneUIManager milestoneUIManager;
    public Image pawsTabImage;
    public Image paperTabImage;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    // --- FIX #4: Coin Panel Positioning ---
    public RectTransform coinUIPanel;
    public Transform coinUINormalPosition;
    public Transform coinUIShopPosition;
    // --- FIX #2: Notification System ---
    

    #region Manager References
    private SkinManager catSkinManager;
    private PaperSkinManager paperSkinManager;
    private CurrencyManager currencyManager;
    private MilestoneManager milestoneManager;
    private PaperRoller paperRoller;
    // --- THIS IS THE FIX: Add a reference for the paper manager ---
    private ContinuousPaperManager continuousPaperManager;
    #endregion

    void Start()
    {
        catSkinManager = FindFirstObjectByType<SkinManager>();
        paperSkinManager = FindFirstObjectByType<PaperSkinManager>();
        currencyManager = FindFirstObjectByType<CurrencyManager>();
        milestoneManager = FindFirstObjectByType<MilestoneManager>();
        paperRoller = FindFirstObjectByType<PaperRoller>();
        // --- THIS IS THE FIX: Find the reference at startup ---
        continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();

        if (pawsTabButton != null) pawsTabButton.onClick.AddListener(() => OpenTab(pawsTabScrollView));
        if (paperTabButton != null) paperTabButton.onClick.AddListener(() => OpenTab(paperTabScrollView));

        OpenTab(pawsTabScrollView);
        shopPanel.SetActive(false);
    }

    // --- NEW METHOD ---
    private void CheckForAutoUnlocks()
    {
        // Add safety checks for all managers
        if (catSkinManager == null || paperSkinManager == null || milestoneManager == null || paperRoller == null || continuousPaperManager == null)
        {
            Debug.LogWarning("ShopManager is missing a manager reference. Cannot check for auto-unlocks.");
            return;
        }

        float currentMeters = 0;
        // Calculate current distance once to be efficient
        if (paperRoller != null && continuousPaperManager.paperTileLength > 0)
        {
            float worldDistance = paperRoller.WorldSpaceDistancePulled;
            float conversionFactor = continuousPaperManager.realWorldMetersPerTile / continuousPaperManager.paperTileLength;
            currentMeters = worldDistance * conversionFactor;
        }

        // Check Cat Paws
        foreach (CatSkin skin in catSkinManager.availableSkins)
        {
            if (catSkinManager.IsSkinUnlocked(skin)) continue;

            bool conditionMet = false;
            switch (skin.unlockType)
            {
                case UnlockType.ByMilestone:
                    if (skin.requiredMilestone != null && milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) conditionMet = true;
                    break;
                case UnlockType.ByTotalDistance:
                    if (currentMeters >= skin.requiredTotalDistance) conditionMet = true;
                    break;
            }
            if (conditionMet)
            {
                catSkinManager.UnlockSkin(skin);
                Debug.Log($"Auto-unlocked Cat Skin: {skin.skinName}");
                //milestoneUIManager?.ShowUnlockNotification($"New Paw Skin Unlocked:\n<b>{skin.skinName}</b>");
            }
        }

        // Check Paper Skins
        foreach (PaperSkin skin in paperSkinManager.availableSkins)
        {
            if (paperSkinManager.IsSkinUnlocked(skin)) continue;

            bool conditionMet = false;
            switch (skin.unlockType)
            {
                case UnlockType.ByMilestone:
                    if (skin.requiredMilestone != null && milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) conditionMet = true;
                    break;
                case UnlockType.ByTotalDistance:
                    if (currentMeters >= skin.requiredTotalDistance) conditionMet = true;
                    break;
            }
            if (conditionMet)
            {
                paperSkinManager.UnlockSkin(skin);
                Debug.Log($"Auto-unlocked Paper Skin: {skin.skinName}");
                //milestoneUIManager?.ShowUnlockNotification($"New Paper Skin Unlocked:\n<b>{skin.skinName}</b>");
            }
        }
    }

    private void RefreshShop()
    {
        // --- THIS IS THE FIX: Call the new check before populating the UI ---
        CheckForAutoUnlocks();

        PopulatePawsShop();
        PopulatePaperShop();
    }
    private int GetSkinSortScore(object skin)
    {
        if (skin is CatSkin catSkin)
        {
            if (catSkinManager.IsSkinUnlocked(catSkin)) return 0; // Unlocked skins are top priority
            if (catSkin.unlockType == UnlockType.ByCoins) return 1; // Then coin-unlocked
            return 2; // Then everything else (milestone, distance, etc.)
        }
        if (skin is PaperSkin paperSkin)
        {
            if (paperSkinManager.IsSkinUnlocked(paperSkin)) return 0;
            if (paperSkin.unlockType == UnlockType.ByCoins) return 1;
            return 2;
        }
        return 99; // Default fallback
    }
    // All other methods from your original script are correct and unchanged.
    #region Unchanged Methods
    private void PopulatePawsShop() { foreach (Transform child in pawsContentParent) { Destroy(child.gameObject); } if (catSkinManager == null || catSkinManager.availableSkins == null) return;
        var sortedSkins = catSkinManager.availableSkins
      .OrderBy(s => catSkinManager.IsSkinUnlocked(s) ? 0 : 1)    // 1. Unlocked items (0) come before Locked items (1).
      .ThenBy(s => s.unlockType == UnlockType.ByCoins ? 0 : 1) // 2. Within locked items, Coin items (0) come before others (1).
      .ThenBy(s => s.priceInCoins);
        foreach (CatSkin skin in sortedSkins) { GameObject itemGO = Instantiate(shopItemPrefab, pawsContentParent); itemGO.GetComponent<ShopItemButton>().Setup(skin, this); Image itemIcon = itemGO.transform.Find("ItemIcon").GetComponent<Image>(); TextMeshProUGUI itemNameText = itemGO.transform.Find("ItemName").GetComponent<TextMeshProUGUI>(); TextMeshProUGUI unlockConditionText = itemGO.transform.Find("UnlockCondition_Text").GetComponent<TextMeshProUGUI>(); Button itemButton = itemGO.GetComponent<Button>(); TextMeshProUGUI buttonStatusText = itemGO.transform.Find("ButtonStatus_Text").GetComponent<TextMeshProUGUI>(); GameObject lockedOverlay = itemGO.transform.Find("Locked_Overlay").gameObject; GameObject equippedCheckmark = itemGO.transform.Find("Equipped_Checkmark").gameObject; itemNameText.text = skin.skinName; itemIcon.sprite = skin.pawSprite; bool isUnlocked = catSkinManager.IsSkinUnlocked(skin); bool isEquipped = catSkinManager.CurrentSkin == skin; lockedOverlay.SetActive(!isUnlocked); equippedCheckmark.SetActive(isEquipped); itemButton.interactable = !isEquipped; if (isUnlocked) { unlockConditionText.text = "Owned"; buttonStatusText.text = isEquipped ? "Equipped" : "Equip"; } else { unlockConditionText.text = GetUnlockConditionText(skin); buttonStatusText.text = "Locked"; } } }
    private void PopulatePaperShop() { foreach (Transform child in paperContentParent) { Destroy(child.gameObject); } if (paperSkinManager == null || paperSkinManager.availableSkins == null) return;
        var sortedSkins = paperSkinManager.availableSkins
         .OrderBy(s => paperSkinManager.IsSkinUnlocked(s) ? 0 : 1)
         .ThenBy(s => s.unlockType == UnlockType.ByCoins ? 0 : 1)
         .ThenBy(s => s.priceInCoins);
        foreach (PaperSkin skin in sortedSkins) { GameObject itemGO = Instantiate(shopItemPrefab, paperContentParent); itemGO.GetComponent<ShopItemButton>().Setup(skin, this); Image itemIcon = itemGO.transform.Find("ItemIcon").GetComponent<Image>(); TextMeshProUGUI itemNameText = itemGO.transform.Find("ItemName").GetComponent<TextMeshProUGUI>(); TextMeshProUGUI unlockConditionText = itemGO.transform.Find("UnlockCondition_Text").GetComponent<TextMeshProUGUI>(); Button itemButton = itemGO.GetComponent<Button>(); TextMeshProUGUI buttonStatusText = itemGO.transform.Find("ButtonStatus_Text").GetComponent<TextMeshProUGUI>(); GameObject lockedOverlay = itemGO.transform.Find("Locked_Overlay").gameObject; GameObject equippedCheckmark = itemGO.transform.Find("Equipped_Checkmark").gameObject; itemNameText.text = skin.skinName; itemIcon.sprite = skin.thumbnail; itemIcon.enabled = (skin.thumbnail != null); bool isUnlocked = paperSkinManager.IsSkinUnlocked(skin); bool isEquipped = paperSkinManager.CurrentSkin == skin; lockedOverlay.SetActive(!isUnlocked); equippedCheckmark.SetActive(isEquipped); itemButton.interactable = !isEquipped; if (isUnlocked) { unlockConditionText.text = "Owned"; buttonStatusText.text = isEquipped ? "Equipped" : "Equip"; } else { unlockConditionText.text = GetUnlockConditionText(skin); buttonStatusText.text = "Locked"; } } }
    public void HandleUnlockAttempt(object skinObject, Vector2 clickPosition)
    {
        if (skinObject is CatSkin catSkin)
        {
            if (catSkinManager.IsSkinUnlocked(catSkin))
            {
                // It's already unlocked, so this is an "Equip" action.
                SoundManager.Instance?.PlayEquipSound();
                catSkinManager.SetCurrentSkin(catSkin);
                RefreshShop();
            }
            else
            {
                // It's locked, so this is a "Purchase/Unlock" attempt.
                TryUnlock(catSkin, clickPosition);
            }
        }
        else if (skinObject is PaperSkin paperSkin)
        {
            if (paperSkinManager.IsSkinUnlocked(paperSkin))
            {
                // Equip action
                SoundManager.Instance?.PlayEquipSound();
                paperSkinManager.SetCurrentSkin(paperSkin);
                RefreshShop();
            }
            else
            {
                // Purchase/Unlock attempt
                TryUnlock(paperSkin, clickPosition);
            }
        }
    }
    private void TryUnlock(CatSkin skin, Vector2 clickPosition) { string failureReason = ""; switch (skin.unlockType) { case UnlockType.ByCoins: if (!currencyManager.TrySpendCoins(skin.priceInCoins)) failureReason = "Not enough coins!"; break; case UnlockType.ByMilestone: if (skin.requiredMilestone != null && !milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) failureReason = "Milestone not reached!"; break; case UnlockType.ByTotalDistance: if (continuousPaperManager != null) { float currentMeters = (paperRoller.WorldSpaceDistancePulled * continuousPaperManager.realWorldMetersPerTile) / continuousPaperManager.paperTileLength; if (currentMeters < skin.requiredTotalDistance) failureReason = "Not enough distance pulled!"; } break; } if (string.IsNullOrEmpty(failureReason)) { SoundManager.Instance?.PlayPurchaseSuccess(); catSkinManager.UnlockSkin(skin); RefreshShop(); } else { SoundManager.Instance?.PlayPurchaseFailed(); SpawnWarningText(failureReason, clickPosition); } }
    private void TryUnlock(PaperSkin skin, Vector2 clickPosition) { string failureReason = ""; switch (skin.unlockType) { case UnlockType.ByCoins: if (!currencyManager.TrySpendCoins(skin.priceInCoins)) failureReason = "Not enough coins!"; break; case UnlockType.ByMilestone: if (skin.requiredMilestone != null && !milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) failureReason = "Milestone not reached!"; break; case UnlockType.ByTotalDistance: if (continuousPaperManager != null) { float currentMeters = (paperRoller.WorldSpaceDistancePulled * continuousPaperManager.realWorldMetersPerTile) / continuousPaperManager.paperTileLength; if (currentMeters < skin.requiredTotalDistance) failureReason = "Not enough distance pulled!"; } break; } if (string.IsNullOrEmpty(failureReason)) { SoundManager.Instance?.PlayPurchaseSuccess(); paperSkinManager.UnlockSkin(skin); RefreshShop(); } else { SoundManager.Instance?.PlayPurchaseFailed(); SpawnWarningText(failureReason, clickPosition); } }
    private void SpawnWarningText(string message, Vector2 screenPosition) { if (warningTextPrefab == null || mainCanvas == null) { return; } GameObject textGO = Instantiate(warningTextPrefab, mainCanvas.transform); textGO.transform.SetAsLastSibling(); RectTransform textRect = textGO.GetComponent<RectTransform>(); RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>(); RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, mainCanvas.worldCamera, out Vector2 localPoint); textRect.anchoredPosition = localPoint; textGO.GetComponent<TextMeshProUGUI>().text = message; }
    private void OpenTab(GameObject activeScrollView) { pawsTabScrollView.SetActive(activeScrollView == pawsTabScrollView); paperTabScrollView.SetActive(activeScrollView == paperTabScrollView); if (pawsTabImage != null)
        {
            pawsTabImage.color = (activeScrollView == pawsTabScrollView) ? activeTabColor : inactiveTabColor;
        }
        if (paperTabImage != null)
        {
            paperTabImage.color = (activeScrollView == paperTabScrollView) ? activeTabColor : inactiveTabColor;
        }
    }
    public void ToggleShopPanel()
    {
        bool isOpening = !shopPanel.activeSelf;

        if (isOpening)
        {
            // If we are opening the shop, first make sure the milestone screen is closed.
            milestoneUIManager?.CloseMilestoneScreen(); // We will add this method next
            ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.VisitShop);
            RefreshShop();
        }

        shopPanel.SetActive(isOpening);

        // Always update the central UI state
        UIStateManager.Instance?.SetUIBlockingState(isOpening);
        if (coinUIPanel != null)
        {
            coinUIPanel.position = isOpening ? coinUIShopPosition.position : coinUINormalPosition.position;
        }
    }

    public void CloseShopPanel()
    {
        if (shopPanel.activeSelf)
        {
            shopPanel.SetActive(false);
            // Note: We don't change the UIStateManager here, because the
            // other manager that is opening will be responsible for setting it to true.
        }
    }
    private string GetUnlockConditionText(CatSkin skin) { switch (skin.unlockType) { case UnlockType.ByCoins: return $"{skin.priceInCoins} Coins"; case UnlockType.ByMilestone: return skin.requiredMilestone != null ? $"Reach {skin.requiredMilestone.milestoneName}" : "Locked"; case UnlockType.ByTotalDistance: return $"Pull {skin.requiredTotalDistance}m Total"; case UnlockType.Premium: return "Premium"; default: return "Locked"; } }
    private string GetUnlockConditionText(PaperSkin skin) { switch (skin.unlockType) { case UnlockType.ByCoins: return $"{skin.priceInCoins} Coins"; case UnlockType.ByMilestone: return skin.requiredMilestone != null ? $"Reach {skin.requiredMilestone.milestoneName}" : "Locked"; case UnlockType.ByTotalDistance: return $"Pull {skin.requiredTotalDistance}m Total"; case UnlockType.Premium: return "Premium"; default: return "Locked"; } }
    #endregion
}