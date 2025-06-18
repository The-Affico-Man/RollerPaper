using UnityEngine;
using TMPro; // We'll need this for the UI text

/// <summary>
/// A Singleton that manages the player's currency (coins).
/// This is the single source of truth for the player's wallet.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Drag the TextMeshProUGUI component that displays the player's coin total here.")]
    public TextMeshProUGUI coinUIText;

    // The current balance. Publicly readable, but can only be changed by this script.
    public int CurrentCoins { get; private set; }

    // This is a C# Action that other scripts can "listen" to.
    // When the coin amount changes, it will notify any listeners.
    public static event System.Action<int> OnCoinsChanged;

    private void Awake()
    {
        // Standard Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // For now, we start with 0 coins.
        // Later, this is where you would load the saved amount from PlayerPrefs/Firebase.
        LoadCoins();
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
        SaveCoins(); // Save after every change
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
            // Player has enough coins
            CurrentCoins -= amount;
            UpdateUI();
            SaveCoins(); // Save after every change
            Debug.Log($"Spent {amount} coins. New balance: {CurrentCoins}");
            return true;
        }
        else
        {
            // Not enough coins
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

        // Notify any other scripts that are listening for a coin change
        OnCoinsChanged?.Invoke(CurrentCoins);
    }

    // These will be used in a later step
    private void SaveCoins()
    {
        // PlayerPrefs.SetInt("PlayerCoins", CurrentCoins);
    }

    private void LoadCoins()
    {
        // CurrentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        CurrentCoins = 0; // For now, always start at 0
        UpdateUI();
    }
}