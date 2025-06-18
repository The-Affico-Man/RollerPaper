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

    // --- THIS SECTION IS NOW CORRECTED ---
    [Header("Tab Panels")]
    [Tooltip("The parent GameObject for the entire Paws scroll view.")]
    public GameObject pawsTabScrollView; // Was pawsContentPanel
    [Tooltip("The parent GameObject for the entire Paper scroll view.")]
    public GameObject paperTabScrollView; // Was paperContentPanel

    [Header("Content Parents (for spawning items into)")]
    [Tooltip("The Transform of the Content object inside the Paws scroll view.")]
    public Transform pawsContentParent;
    [Tooltip("The Transform of the Content object inside the Paper scroll view.")]
    public Transform paperContentParent;
    // ------------------------------------

    [Header("Prefabs")]
    public GameObject shopItemPrefab;

    // References to other managers
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

        // This logic now correctly targets the parent ScrollView objects
        if (pawsTabButton != null) pawsTabButton.onClick.AddListener(() => OpenTab(pawsTabScrollView));
        if (paperTabButton != null) paperTabButton.onClick.AddListener(() => OpenTab(paperTabScrollView));

        // Start with the paws tab open
        OpenTab(pawsTabScrollView);
        shopPanel.SetActive(false);
    }

    private void RefreshShop()
    {
        PopulatePawsShop();
        PopulatePaperShop();
    }

    // --- THIS METHOD IS NOW CORRECTED ---
    private void OpenTab(GameObject activeScrollView)
    {
        // This will now correctly enable/disable the entire ScrollView object
        pawsTabScrollView.SetActive(activeScrollView == pawsTabScrollView);
        paperTabScrollView.SetActive(activeScrollView == paperTabScrollView);
    }
    // ------------------------------------

    private void PopulatePawsShop()
    {
        // This now correctly targets the content parent transform
        foreach (Transform child in pawsContentParent) { Destroy(child.gameObject); }
        if (catSkinManager == null || catSkinManager.availableSkins == null) return;

        foreach (CatSkin skin in catSkinManager.availableSkins)
        {
            // This now correctly instantiates items into the content parent
            GameObject itemGO = Instantiate(shopItemPrefab, pawsContentParent);

            #region Paw Item Setup
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
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(() => EquipPawSkin(skin));
            }
            else
            {
                itemPriceText.text = GetUnlockConditionText(skin);
                buttonText.text = "Unlock";
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(() => TryUnlockPawSkin(skin));
            }
            #endregion
        }
    }

    private void PopulatePaperShop()
    {
        // This now correctly targets the content parent transform
        foreach (Transform child in paperContentParent) { Destroy(child.gameObject); }
        if (paperSkinManager == null || paperSkinManager.availableSkins == null) return;

        foreach (PaperSkin skin in paperSkinManager.availableSkins)
        {
            // This now correctly instantiates items into the content parent
            GameObject itemGO = Instantiate(shopItemPrefab, paperContentParent);

            #region Paper Item Setup
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
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(() => EquipPaperSkin(skin));
            }
            else
            {
                itemPriceText.text = GetUnlockConditionText(skin);
                buttonText.text = "Unlock";
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(() => TryUnlockPaperSkin(skin));
            }
            #endregion
        }
    }

    // --- All other methods are correct and unchanged ---
    #region Unchanged Methods
    void TryUnlockPawSkin(CatSkin skin) { bool success = false; switch (skin.unlockType) { case UnlockType.ByCoins: if (currencyManager.TrySpendCoins(skin.priceInCoins)) { success = true; } break; case UnlockType.ByMilestone: if (milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) { success = true; } break; case UnlockType.ByTotalDistance: ContinuousPaperManager cpm = FindFirstObjectByType<ContinuousPaperManager>(); float currentMeters = (paperRoller.WorldSpaceDistancePulled * cpm.realWorldMetersPerTile) / cpm.paperTileLength; if (currentMeters >= skin.requiredTotalDistance) { success = true; } break; } if (success) { catSkinManager.UnlockSkin(skin); RefreshShop(); } else { Debug.Log("Unlock conditions not met for " + skin.skinName); } }
    void TryUnlockPaperSkin(PaperSkin skin) { bool success = false; switch (skin.unlockType) { case UnlockType.ByCoins: if (currencyManager.TrySpendCoins(skin.priceInCoins)) { success = true; } break; case UnlockType.ByMilestone: if (milestoneManager.IsMilestoneUnlocked(skin.requiredMilestone)) { success = true; } break; case UnlockType.ByTotalDistance: ContinuousPaperManager cpm = FindFirstObjectByType<ContinuousPaperManager>(); float currentMeters = (paperRoller.WorldSpaceDistancePulled * cpm.realWorldMetersPerTile) / cpm.paperTileLength; if (currentMeters >= skin.requiredTotalDistance) { success = true; } break; } if (success) { paperSkinManager.UnlockSkin(skin); RefreshShop(); } else { Debug.Log("Unlock conditions not met for " + skin.skinName); } }
    void EquipPawSkin(CatSkin skin) { catSkinManager.SetCurrentSkin(skin); RefreshShop(); }
    void EquipPaperSkin(PaperSkin skin) { paperSkinManager.SetCurrentSkin(skin); RefreshShop(); }
    public void ToggleShopPanel() { bool isNowActive = !shopPanel.activeSelf; shopPanel.SetActive(isNowActive); if (isNowActive) { RefreshShop(); } if (UIStateManager.Instance != null) { UIStateManager.Instance.SetUIBlockingState(isNowActive); } }
    private string GetUnlockConditionText(CatSkin skin) { switch (skin.unlockType) { case UnlockType.ByCoins: return $"{skin.priceInCoins} Coins"; case UnlockType.ByMilestone: return $"Reach {skin.requiredMilestone.milestoneName}"; case UnlockType.ByTotalDistance: return $"Pull {skin.requiredTotalDistance}m Total"; case UnlockType.Premium: return "Premium"; default: return "Locked"; } }
    private string GetUnlockConditionText(PaperSkin skin) { switch (skin.unlockType) { case UnlockType.ByCoins: return $"{skin.priceInCoins} Coins"; case UnlockType.ByMilestone: return $"Reach {skin.requiredMilestone.milestoneName}"; case UnlockType.ByTotalDistance: return $"Pull {skin.requiredTotalDistance}m Total"; case UnlockType.Premium: return "Premium"; default: return "Locked"; } }
    #endregion
}