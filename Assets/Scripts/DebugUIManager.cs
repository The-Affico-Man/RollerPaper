using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button debugToggleButton;

    [Header("Script References")]
    [SerializeField] private SwipeController swipeController;
    [SerializeField] private PaperRoller paperRoller;

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

    void Start()
    {
        if (swipeController == null) swipeController = FindFirstObjectByType<SwipeController>();
        if (paperRoller == null) paperRoller = FindFirstObjectByType<PaperRoller>();
        if (swipeController == null || paperRoller == null || debugPanel == null)
        {
            Debug.LogError("Debug UI Manager is missing critical references! Disabling.");
            if (gameObject != null) gameObject.SetActive(false);
            return;
        }

        debugPanel.SetActive(false);
        debugToggleButton.onClick.AddListener(ToggleDebugPanel);

        InitializeSmoothingSlider();
        InitializePullSensitivitySlider(); // This method is now corrected
        InitializeGlideDampingSlider();
        InitializeTwoFingerBonusSlider();

        if (speedBoostButton != null)
        {
            speedBoostButton.onClick.AddListener(() => { paperRoller.ActivateSpeedBoost(); });
        }
    }

    private void InitializeSmoothingSlider()
    {
        smoothingSlider.minValue = 1f;
        smoothingSlider.maxValue = 50f;
        smoothingSlider.value = swipeController.swipeSmoothingFactor;
        smoothingValueText.text = swipeController.swipeSmoothingFactor.ToString("F1");
        smoothingSlider.onValueChanged.AddListener(OnSmoothingSliderChanged);
    }

    // --- THIS IS THE CORRECTED METHOD ---
    private void InitializePullSensitivitySlider()
    {
        // Use the new, larger range for the normalized input system
        pullSensitivitySlider.minValue = 10f;
        pullSensitivitySlider.maxValue = 200f;
        pullSensitivitySlider.value = paperRoller.pullSensitivity;
        pullSensitivityValueText.text = paperRoller.pullSensitivity.ToString("F1");
        pullSensitivitySlider.onValueChanged.AddListener(OnPullSensitivitySliderChanged);
    }
    // ------------------------------------

    private void InitializeGlideDampingSlider()
    {
        glideDampingSlider.minValue = 1f;
        glideDampingSlider.maxValue = 20f;
        glideDampingSlider.value = paperRoller.glideDamping;
        glideDampingValueText.text = paperRoller.glideDamping.ToString("F1");
        glideDampingSlider.onValueChanged.AddListener(OnGlideDampingSliderChanged);
    }

    private void InitializeTwoFingerBonusSlider()
    {
        twoFingerBonusSlider.minValue = 1.0f;
        twoFingerBonusSlider.maxValue = 3.0f;
        twoFingerBonusSlider.value = paperRoller.twoFingerBonus;
        twoFingerBonusValueText.text = paperRoller.twoFingerBonus.ToString("F1");
        twoFingerBonusSlider.onValueChanged.AddListener(OnTwoFingerBonusSliderChanged);
    }

    public void ToggleDebugPanel() { debugPanel.SetActive(!debugPanel.activeSelf); }
    public void OnSmoothingSliderChanged(float value) { if (swipeController != null) swipeController.swipeSmoothingFactor = value; if (smoothingValueText != null) smoothingValueText.text = value.ToString("F1"); }
    public void OnPullSensitivitySliderChanged(float value) { if (paperRoller != null) paperRoller.pullSensitivity = value; if (pullSensitivityValueText != null) pullSensitivityValueText.text = value.ToString("F1"); }
    public void OnGlideDampingSliderChanged(float value) { if (paperRoller != null) paperRoller.glideDamping = value; if (glideDampingValueText != null) glideDampingValueText.text = value.ToString("F1"); }
    public void OnTwoFingerBonusSliderChanged(float value) { if (paperRoller != null) paperRoller.twoFingerBonus = value; if (twoFingerBonusValueText != null) twoFingerBonusValueText.text = value.ToString("F1"); }

    private void OnDestroy()
    {
        if (debugToggleButton != null) debugToggleButton.onClick.RemoveAllListeners();
        if (smoothingSlider != null) smoothingSlider.onValueChanged.RemoveAllListeners();
        if (pullSensitivitySlider != null) pullSensitivitySlider.onValueChanged.RemoveAllListeners();
        if (glideDampingSlider != null) glideDampingSlider.onValueChanged.RemoveAllListeners();
        if (twoFingerBonusSlider != null) twoFingerBonusSlider.onValueChanged.RemoveAllListeners();
        if (speedBoostButton != null) speedBoostButton.onClick.RemoveAllListeners();
    }
}