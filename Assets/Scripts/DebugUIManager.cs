using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Required for reloading the scene

public class DebugUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button debugToggleButton;

    [Header("Script References")]
    [SerializeField] private SwipeController swipeController;
    [SerializeField] private PaperRoller paperRoller;
    [SerializeField] private SkinManager catSkinManager;
    [SerializeField] private PaperSkinManager paperSkinManager;
    // --- NEW REFERENCES ---
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private GameDataManager gameDataManager;

    // --- All slider and original button references ---
    #region UI References
    [Header("SwipeController Settings")]
    [SerializeField] private Slider smoothingSlider;
    [SerializeField] private TextMeshProUGUI smoothingValueText;
    [Header("PaperRoller Settings")]
    [SerializeField] private Slider pullSensitivitySlider;
    [SerializeField] private TextMeshProUGUI pullSensitivityValueText;
    [SerializeField] private Slider glideDampingSlider;
    [SerializeField] private TextMeshProUGUI glideDampingValueText;
    [SerializeField] private Slider twoFingerBonusSlider;
    [SerializeField] private TextMeshProUGUI twoFingerBonusValueText;
    [SerializeField] private Button speedBoostButton;
    [Header("Cat Skin Settings")]
    [SerializeField] private Button cycleCatSkinButton;
    [SerializeField] private TextMeshProUGUI currentCatSkinText;
    [Header("Paper Skin Settings")]
    [SerializeField] private Button cyclePaperSkinButton;
    [SerializeField] private TextMeshProUGUI currentPaperSkinText;

    // --- NEW BUTTON REFERENCES ---
    [Header("Cheat Buttons")]
    [SerializeField] private Button addCoinsButton;
    [SerializeField] private Button addDistanceButton;
    [SerializeField] private Button resetProgressButton;
    #endregion


    void Start()
    {
        // Find all references
        swipeController = FindFirstObjectByType<SwipeController>();
        paperRoller = FindFirstObjectByType<PaperRoller>();
        catSkinManager = FindFirstObjectByType<SkinManager>();
        paperSkinManager = FindFirstObjectByType<PaperSkinManager>();
        // --- Find new references ---
        currencyManager = FindFirstObjectByType<CurrencyManager>();
        gameDataManager = FindFirstObjectByType<GameDataManager>();

        // Check for missing critical references
        if (swipeController == null || paperRoller == null || catSkinManager == null || paperSkinManager == null || debugPanel == null || currencyManager == null || gameDataManager == null)
        {
            Debug.LogError("Debug UI Manager is missing critical references! Disabling.", this.gameObject);
            gameObject.SetActive(false);
            return;
        }

        debugPanel.SetActive(false);
        debugToggleButton.onClick.AddListener(ToggleDebugPanel);

        // Initialize UI elements
        InitializeAllSliders();
        InitializeAllButtons();
        InitializeCheatButtons(); // New method for new buttons

        UpdateCatSkinText();
        UpdatePaperSkinText();
    }

    // --- NEW METHOD for initializing cheat buttons ---
    private void InitializeCheatButtons()
    {
        if (addCoinsButton != null)
        {
            addCoinsButton.onClick.AddListener(() => {
                currencyManager?.AddCoins(999999);
            });
        }
        if (addDistanceButton != null)
        {
            addDistanceButton.onClick.AddListener(() => {
                paperRoller?.AddDebugDistance(100000f);
            });
        }
        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.AddListener(() => {
                // First, delete all the PlayerPrefs data
                gameDataManager?.DeleteAllSaveData();
                // Then, reload the current scene to start fresh
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }
    }

    // Unchanged methods
    #region Unchanged Methods
    private void InitializeAllButtons() { if (speedBoostButton != null) { speedBoostButton.onClick.AddListener(() => { paperRoller.ActivateSpeedBoost(); }); } if (cycleCatSkinButton != null) { cycleCatSkinButton.onClick.AddListener(() => { catSkinManager.CycleToNextSkin(); UpdateCatSkinText(); }); } if (cyclePaperSkinButton != null) { cyclePaperSkinButton.onClick.AddListener(() => { paperSkinManager.CycleToNextSkin(); UpdatePaperSkinText(); }); } }
    private void UpdatePaperSkinText() { if (paperSkinManager != null && paperSkinManager.CurrentSkin != null && currentPaperSkinText != null) { currentPaperSkinText.text = $"Current: {paperSkinManager.CurrentSkin.skinName}"; } }
    private void UpdateCatSkinText() { if (catSkinManager != null && catSkinManager.CurrentSkin != null && currentCatSkinText != null) { currentCatSkinText.text = $"Current: {catSkinManager.CurrentSkin.skinName}"; } }
    private void InitializeAllSliders() { smoothingSlider.minValue = 1f; smoothingSlider.maxValue = 50f; smoothingSlider.value = swipeController.swipeSmoothingFactor; smoothingValueText.text = swipeController.swipeSmoothingFactor.ToString("F1"); smoothingSlider.onValueChanged.AddListener(OnSmoothingSliderChanged); pullSensitivitySlider.minValue = 10f; pullSensitivitySlider.maxValue = 200f; pullSensitivitySlider.value = paperRoller.pullSensitivity; pullSensitivityValueText.text = paperRoller.pullSensitivity.ToString("F1"); pullSensitivitySlider.onValueChanged.AddListener(OnPullSensitivitySliderChanged); glideDampingSlider.minValue = 1f; glideDampingSlider.maxValue = 20f; glideDampingSlider.value = paperRoller.glideDamping; glideDampingValueText.text = paperRoller.glideDamping.ToString("F1"); glideDampingSlider.onValueChanged.AddListener(OnGlideDampingSliderChanged); twoFingerBonusSlider.minValue = 1.0f; twoFingerBonusSlider.maxValue = 3.0f; twoFingerBonusSlider.value = paperRoller.twoFingerBonus; twoFingerBonusValueText.text = paperRoller.twoFingerBonus.ToString("F1"); twoFingerBonusSlider.onValueChanged.AddListener(OnTwoFingerBonusSliderChanged); }
    public void ToggleDebugPanel() { debugPanel.SetActive(!debugPanel.activeSelf); }
    public void OnSmoothingSliderChanged(float value) { if (swipeController != null) swipeController.swipeSmoothingFactor = value; if (smoothingValueText != null) smoothingValueText.text = value.ToString("F1"); }
    public void OnPullSensitivitySliderChanged(float value) { if (paperRoller != null) paperRoller.pullSensitivity = value; if (pullSensitivityValueText != null) pullSensitivityValueText.text = value.ToString("F1"); }
    public void OnGlideDampingSliderChanged(float value) { if (paperRoller != null) paperRoller.glideDamping = value; if (glideDampingValueText != null) glideDampingValueText.text = value.ToString("F1"); }
    public void OnTwoFingerBonusSliderChanged(float value) { if (paperRoller != null) paperRoller.twoFingerBonus = value; if (twoFingerBonusValueText != null) twoFingerBonusValueText.text = value.ToString("F1"); }
    #endregion

    private void OnDestroy()
    {
        // Remove listeners from original buttons
        if (debugToggleButton != null) debugToggleButton.onClick.RemoveAllListeners();
        if (smoothingSlider != null) smoothingSlider.onValueChanged.RemoveAllListeners();
        if (pullSensitivitySlider != null) pullSensitivitySlider.onValueChanged.RemoveAllListeners();
        if (glideDampingSlider != null) glideDampingSlider.onValueChanged.RemoveAllListeners();
        if (twoFingerBonusSlider != null) twoFingerBonusSlider.onValueChanged.RemoveAllListeners();
        if (speedBoostButton != null) speedBoostButton.onClick.RemoveAllListeners();
        if (cycleCatSkinButton != null) cycleCatSkinButton.onClick.RemoveAllListeners();
        if (cyclePaperSkinButton != null) cyclePaperSkinButton.onClick.RemoveAllListeners();

        // --- NEW: Remove listeners from cheat buttons ---
        if (addCoinsButton != null) addCoinsButton.onClick.RemoveAllListeners();
        if (addDistanceButton != null) addDistanceButton.onClick.RemoveAllListeners();
        if (resetProgressButton != null) resetProgressButton.onClick.RemoveAllListeners();
    }
}