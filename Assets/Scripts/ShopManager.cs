using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    #region Manager References
    private SkinManager catSkinManager;
    private PaperSkinManager paperSkinManager;
    private CurrencyManager currencyManager;
    private MilestoneManager milestoneManager;
    private PaperRoller paperRoller;
    #endregion

    void Start()
    {
        catSkinManager = FindFirstObjectByType<SkinManager>();
        paperSkinManager = FindFirstObjectByType<PaperSkinManager>();
        currencyManager = FindFirstObjectByType<CurrencyManager>();
        milestoneManager = FindFirstObjectByType<MilestoneManager>();
        paperRoller = FindFirstObjectByType<PaperRoller>();

        if (pawsTabButton != null) pawsTabButton.onClick.AddListener(() => OpenTab(pawsTabScrollView));
        if (paperTabButton != null) paperTabButton.onClick.AddListener(() => OpenTab(paperTabScrollView));

        OpenTab(pawsTabScrollView);
        shopPanel.SetActive(false);
    }

    private void PopulatePawsShop()
    {
        foreach (Transform child in pawsContentParent) { Destroy(child.gameObject); }
        if (catSkinManager == null || catSkinManager.availableSkins == null) return;

        foreach (CatSkin skin in catSkinManager.availableSkins)
        {
            GameObject itemGO = Instantiate(shopItemPrefab, pawsContentParent);

            // --- THIS IS THE NEW, SIMPLIFIED SETUP ---
            // Tell the button on the new item everything it needs to know about itself.
            itemGO.GetComponent<ShopItemButton>().Setup(skin, this);
            // ----------------------------------------

            #region UI State Setup
            Image itemIcon = itemGO.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI itemName = itemGO.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI itemPriceText = itemGO.transform.Find("ItemPrice").GetComponent<TextMeshProUGUI>();
            Button itemButton = itemGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = itemButton.GetComponentInChildren<TextMeshProUGUI>();
            GameObject lockedOverlay = itemGO.transform.Find("Locked_Overlay").gameObject;
            GameObject equippedCheckmark = itemGO.transform.Find("Equipped_Checkmark").gameObject;

            itemName.text = skin.skinName;
            itemIcon.sprite = skin.pawSprite;
            bool isUnlocked = catSkinManager.IsSkinUnlocked(skin);
            bool isEquipped = catSkinManager.CurrentSkin == skin;
            lockedOverlay.SetActive(!isUnlocked);
            equippedCheckmark.SetActive(isEquipped);
            itemButton.interactable = !isEquipped;

            if (isUnlocked)
            {
                itemPriceText.text = "Owned";
                buttonText.text = isEquipped ? "Equipped" : "Equip";
            }
            else
            {
                itemPriceText.text = GetUnlockConditionText(skin);
                buttonText.text = "Unlock";
            }
            #endregion
        }
    }

    private void PopulatePaperShop()
    {
        foreach (Transform child in paperContentParent) { Destroy(child.gameObject); }
        if (paperSkinManager == null || paperSkinManager.availableSkins == null) return;

        foreach (PaperSkin skin in paperSkinManager.availableSkins)
        {
            GameObject itemGO = Instantiate(shopItemPrefab, paperContentParent);

            // --- THIS IS THE NEW, SIMPLIFIED SETUP ---
            itemGO.GetComponent<ShopItemButton>().Setup(skin, this);
            // ----------------------------------------

            #region UI State Setup
            Image itemIcon = itemGO.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI itemName = itemGO.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI itemPriceText = itemGO.transform.Find("ItemPrice").GetComponent<TextMeshProUGUI>();
            Button itemButton = itemGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = itemButton.GetComponentInChildren<TextMeshProUGUI>();
            GameObject lockedOverlay = itemGO.transform.Find("Locked_Overlay").gameObject;
            GameObject equippedCheckmark = itemGO.transform.Find("Equipped_Checkmark").gameObject;

            itemName.text = skin.skinName;
            itemIcon.sprite = skin.thumbnail;
            itemIcon.enabled = (skin.thumbnail != null);
            bool isUnlocked = paperSkinManager.IsSkinUnlocked(skin);
            bool isEquipped = paperSkinManager.CurrentSkin == skin;
            lockedOverlay.SetActive(!isUnlocked);
            equippedCheckmark.SetActive(isEquipped);
            itemButton.interactable = !isEquipped;

            if (isUnlocked)
            {
                itemPriceText.text = "Owned";
                buttonText.text = isEquipped ? "Equipped" : "Equip";
            }
            else
            {
                itemPriceText.text = GetUnlockConditionText(skin);
                buttonText.text = "Unlock";
            }
            #endregion
        }
    }

    // --- NEW UNIFIED UNLOCK METHOD ---
    public void HandleUnlockAttempt(object skinObject, Vector2 clickPosition)
    {
        if (skinObject is CatSkin catSkin)
        {
            if (catSkinManager.IsSkinUnlocked(catSkin))
            {
                catSkinManager.SetCurrentSkin(catSkin);
                RefreshShop();
            }
            else
            {
                TryUnlock(catSkin, clickPosition);
            }
        }
        else if (skinObject is PaperSkin paperSkin)
        {
            if (paperSkinManager.IsSkinUnlocked(paperSkin))
            {
                paperSkinManager.SetCurrentSkin(paperSkin);
                RefreshShop();
            }
            else
            {
                TryUnlock(paperSkin, clickPosition);
            }
        }
    }

    private void TryUnlock(CatSkin skin, Vector2 clickPosition)
    {
        string failureReason = "";
        switch (skin.unlockType)
        {
            case UnlockType.ByCoins:
                if (!currencyManager.TrySpendCoins(skin.priceInCoins)) failureReason = "Not enough coins!";
                break;
            case UnlockType.ByMilestone:
                if (!milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) failureReason = "Milestone not reached!";
                break;
            case UnlockType.ByTotalDistance:
                ContinuousPaperManager cpm = FindFirstObjectByType<ContinuousPaperManager>();
                float currentMeters = (paperRoller.WorldSpaceDistancePulled * cpm.realWorldMetersPerTile) / cpm.paperTileLength;
                if (currentMeters < skin.requiredTotalDistance) failureReason = "Not enough distance pulled!";
                break;
        }

        if (string.IsNullOrEmpty(failureReason))
        {
            catSkinManager.UnlockSkin(skin);
            RefreshShop();
        }
        else { SpawnWarningText(failureReason, clickPosition); }
    }

    private void TryUnlock(PaperSkin skin, Vector2 clickPosition)
    {
        string failureReason = "";
        switch (skin.unlockType)
        {
            case UnlockType.ByCoins:
                if (!currencyManager.TrySpendCoins(skin.priceInCoins)) failureReason = "Not enough coins!";
                break;
            case UnlockType.ByMilestone:
                if (!milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) failureReason = "Milestone not reached!";
                break;
            case UnlockType.ByTotalDistance:
                ContinuousPaperManager cpm = FindFirstObjectByType<ContinuousPaperManager>();
                float currentMeters = (paperRoller.WorldSpaceDistancePulled * cpm.realWorldMetersPerTile) / cpm.paperTileLength;
                if (currentMeters < skin.requiredTotalDistance) failureReason = "Not enough distance pulled!";
                break;
        }

        if (string.IsNullOrEmpty(failureReason))
        {
            paperSkinManager.UnlockSkin(skin);
            RefreshShop();
        }
        else { SpawnWarningText(failureReason, clickPosition); }
    }
    // -------------------------------

    private void SpawnWarningText(string message, Vector2 screenPosition)
    {
        if (warningTextPrefab == null || mainCanvas == null)
        {
            Debug.LogError("WarningText Prefab or Main Canvas is not assigned in the ShopManager Inspector!");
            return;
        }

        GameObject textGO = Instantiate(warningTextPrefab, mainCanvas.transform);
        textGO.transform.SetAsLastSibling();

        RectTransform textRect = textGO.GetComponent<RectTransform>();

        // --- THIS IS THE DEFINITIVE FIX ---

        // To correctly position a UI element based on a screen point (like a mouse click),
        // we need to convert the screen point to the local space of the canvas's RectTransform.

        // Get the RectTransform of the main canvas.
        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();

        // Use this special Unity function to do the conversion.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            mainCanvas.worldCamera, // Assumes you have a camera assigned to your canvas
            out Vector2 localPoint
        );

        // Now, set the anchoredPosition to the converted local point.
        textRect.anchoredPosition = localPoint;

        // --- END OF FIX ---

        textGO.GetComponent<TextMeshProUGUI>().text = message;
    }

    // Unchanged Methods
    #region Unchanged Code
    private void RefreshShop() { PopulatePawsShop(); PopulatePaperShop(); }
    private void OpenTab(GameObject activeScrollView) { pawsTabScrollView.SetActive(activeScrollView == pawsTabScrollView); paperTabScrollView.SetActive(activeScrollView == paperTabScrollView); }
    public void ToggleShopPanel() { bool isNowActive = !shopPanel.activeSelf; shopPanel.SetActive(isNowActive); if (isNowActive) { RefreshShop(); } if (UIStateManager.Instance != null) { UIStateManager.Instance.SetUIBlockingState(isNowActive); } }
    private string GetUnlockConditionText(CatSkin skin) { switch (skin.unlockType) { case UnlockType.ByCoins: return $"{skin.priceInCoins} Coins"; case UnlockType.ByMilestone: return $"Reach {skin.requiredMilestone.milestoneName}"; case UnlockType.ByTotalDistance: return $"Pull {skin.requiredTotalDistance}m Total"; case UnlockType.Premium: return "Premium"; default: return "Locked"; } }
    private string GetUnlockConditionText(PaperSkin skin) { switch (skin.unlockType) { case UnlockType.ByCoins: return $"{skin.priceInCoins} Coins"; case UnlockType.ByMilestone: return $"Reach {skin.requiredMilestone.milestoneName}"; case UnlockType.ByTotalDistance: return $"Pull {skin.requiredTotalDistance}m Total"; case UnlockType.Premium: return "Premium"; default: return "Locked"; } }
    #endregion
}