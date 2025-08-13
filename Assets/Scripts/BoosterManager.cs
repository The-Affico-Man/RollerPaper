// BoosterManager.cs (REVISED)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    // --- Lucky Cat (Consumable) ---
    [Header("Lucky Cat Booster")]
    public float luckyCatDuration = 60f;
    public int luckyCatCost = 500;
    public float luckyCatCoinMultiplier = 2f;
    public int LuckyCatInventory { get; private set; }
    public bool IsLuckyCatActive { get; private set; } = false;

    // --- Turbo Paws (Daily Limit) ---
    [Header("Turbo Paws Booster")]
    public int turboPawsFreeDailyUses = 3;
    private int turboPawsUsedToday = 0;

    // --- UI References ---
    [Header("Active Booster UI (Top of Screen)")]
    public GameObject activeBoosterUIPanel;
    public Image activeBoosterIcon;
    public Image activeBoosterTimerFill;
    public TextMeshProUGUI activeBoosterTimerText;

    [Header("Inventory Button UI (Main Screen)")]
    public Button useLuckyCatButton;
    public TextMeshProUGUI luckyCatInventoryText;

    // --- Public Properties ---
    public float CoinMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    private void Start()
    {
        // --- Load Progress ---
        LoadProgress();

        // --- Setup UI ---
        if (activeBoosterUIPanel != null) activeBoosterUIPanel.SetActive(false);
        if (useLuckyCatButton != null) useLuckyCatButton.onClick.AddListener(TryUseLuckyCat);
        UpdateLuckyCatInventoryUI();
    }

    // --- Lucky Cat Methods ---

    public void AddLuckyCat(int amount)
    {
        LuckyCatInventory += amount;
        UpdateLuckyCatInventoryUI();
        SaveProgress();
    }

    public void TryUseLuckyCat()
    {
        if (LuckyCatInventory > 0 && !IsLuckyCatActive)
        {
            LuckyCatInventory--;
            UpdateLuckyCatInventoryUI();
            SaveProgress();
            StartCoroutine(LuckyCatCoroutine());
        }
        else
        {
            // Optional: Play a "cannot use" sound or show a message
            Debug.Log("Cannot use Lucky Cat. Inventory empty or already active.");
        }
    }

    private void UpdateLuckyCatInventoryUI()
    {
        if (useLuckyCatButton != null)
        {
            useLuckyCatButton.interactable = (LuckyCatInventory > 0 && !IsLuckyCatActive);
        }
        if (luckyCatInventoryText != null)
        {
            luckyCatInventoryText.text = LuckyCatInventory.ToString();
            luckyCatInventoryText.transform.parent.gameObject.SetActive(LuckyCatInventory > 0);
        }
    }

    private IEnumerator LuckyCatCoroutine()
    {
        IsLuckyCatActive = true;
        CoinMultiplier = luckyCatCoinMultiplier;
        UpdateLuckyCatInventoryUI(); // Disable button while active

        if (activeBoosterUIPanel != null) activeBoosterUIPanel.SetActive(true);

        float timeLeft = luckyCatDuration;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (activeBoosterTimerFill != null) activeBoosterTimerFill.fillAmount = timeLeft / luckyCatDuration;
            if (activeBoosterTimerText != null) activeBoosterTimerText.text = $"{Mathf.CeilToInt(timeLeft)}s";
            yield return null;
        }

        CoinMultiplier = 1f;
        IsLuckyCatActive = false;
        if (activeBoosterUIPanel != null) activeBoosterUIPanel.SetActive(false);
        UpdateLuckyCatInventoryUI(); // Re-enable button if more are in inventory
    }


    // --- Turbo Paws Methods ---

    public bool CanUseTurboPaws()
    {
        CheckDailyReset();
        return turboPawsUsedToday < turboPawsFreeDailyUses;
    }

    public void UseTurboPaws()
    {
        if (CanUseTurboPaws())
        {
            turboPawsUsedToday++;
            SaveProgress();
            // The PaperRoller will be responsible for the actual boost effect.
            // This method just handles the logic.
            Debug.Log($"Used Turbo Paws. Uses left today: {turboPawsFreeDailyUses - turboPawsUsedToday}");
        }
    }

    private void CheckDailyReset()
    {
        string lastResetDate = PlayerPrefs.GetString("TurboPaws_LastResetDate", "");
        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (lastResetDate != todayDate)
        {
            turboPawsUsedToday = 0;
            PlayerPrefs.SetString("TurboPaws_LastResetDate", todayDate);
            Debug.Log("Turbo Paws daily uses have been reset.");
            SaveProgress();
        }
    }

    // --- Save/Load ---
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("Inventory_LuckyCat", LuckyCatInventory);
        PlayerPrefs.SetInt("TurboPaws_UsedToday", turboPawsUsedToday);
    }

    public void LoadProgress()
    {
        LuckyCatInventory = PlayerPrefs.GetInt("Inventory_LuckyCat", 0);
        turboPawsUsedToday = PlayerPrefs.GetInt("TurboPaws_UsedToday", 0);
        CheckDailyReset(); // Ensure we check the date on load
    }
}