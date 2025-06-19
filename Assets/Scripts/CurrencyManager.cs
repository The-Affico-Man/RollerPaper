using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("UI Reference")]
    public TextMeshProUGUI coinUIText;

    public int CurrentCoins { get; private set; }
    public static event System.Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        // NOTE: We no longer load here. The GameDataManager controls the load order.
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Tried to add a non-positive amount of coins.");
            return;
        }
        CurrentCoins += amount;
        UpdateUI();
        Debug.Log($"Added {amount} coins. New balance: {CurrentCoins}");
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Tried to spend a non-positive amount of coins.");
            return false;
        }
        if (CurrentCoins >= amount)
        {
            CurrentCoins -= amount;
            UpdateUI();
            Debug.Log($"Spent {amount} coins. New balance: {CurrentCoins}");
            return true;
        }
        else
        {
            Debug.Log("Not enough coins to spend.");
            return false;
        }
    }

    private void UpdateUI()
    {
        if (coinUIText != null)
        {
            coinUIText.text = CurrentCoins.ToString();
        }
        OnCoinsChanged?.Invoke(CurrentCoins);
    }

    // --- NEW SAVE/LOAD METHODS ---

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(PlayerPrefsKeys.PlayerCoins, CurrentCoins);
    }

    public void LoadProgress()
    {
        // Load the saved coins, defaulting to 0 if no key exists.
        CurrentCoins = PlayerPrefs.GetInt(PlayerPrefsKeys.PlayerCoins, 0);
        UpdateUI();
    }
}