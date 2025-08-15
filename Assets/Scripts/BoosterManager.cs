// BoosterManager.cs (FINAL, COMPLETE SCRIPT)
using System;
using System.Collections;
using UnityEngine;

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    // This event will be fired whenever booster data changes. The UI will listen to this.
    public static event Action OnBoosterDataChanged;

    [Header("Developer Settings")]
    [Tooltip("Sets the number of Lucky Cats the player starts with ON FIRST LAUNCH ONLY.")]
    public int initialLuckyCatAmount = 0;

    [Header("Lucky Cat Booster")]
    public float luckyCatDuration = 300f;
    public int luckyCatCost = 500;
    public float luckyCatCoinMultiplier = 2f;
    public Sprite luckyCatIcon;
    public int LuckyCatInventory { get; private set; }
    public bool IsLuckyCatActive { get; private set; } = false;

    [Header("Turbo Paws Booster")]
    public int turboPawsFreeDailyUses = 3;
    public Sprite turboPawsIcon;
    public int TurboPawsUsedToday { get; private set; } = 0;

    public float CoinMultiplier { get; private set; } = 1f;
    private Coroutine luckyCatEffectCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    private void Start()
    {
        LoadProgress();
    }

    /// <summary>
    /// A public method that other scripts can call to safely trigger the OnBoosterDataChanged event.
    /// This is the FIX for the compiler error.
    /// </summary>
    public void NotifyDataChanged()
    {
        OnBoosterDataChanged?.Invoke();
    }

    public void AddLuckyCat(int amount)
    {
        if (amount <= 0) return;
        LuckyCatInventory += amount;
        SaveProgress();
        OnBoosterDataChanged?.Invoke();
    }

    public void UseLuckyCat()
    {
        if (LuckyCatInventory > 0 && !IsLuckyCatActive)
        {
            LuckyCatInventory--;
            SaveProgress();
            if (luckyCatEffectCoroutine != null) StopCoroutine(luckyCatEffectCoroutine);
            luckyCatEffectCoroutine = StartCoroutine(LuckyCatEffectCoroutine());
            OnBoosterDataChanged?.Invoke();
        }
    }

    public void UseTurboPaws()
    {
        if (CanUseTurboPaws())
        {
            TurboPawsUsedToday++;
            SaveProgress();
            FindFirstObjectByType<PaperRoller>()?.ActivateSpeedBoost();
            OnBoosterDataChanged?.Invoke();
        }
    }

    public bool CanUseTurboPaws()
    {
        CheckDailyReset();
        return TurboPawsUsedToday < turboPawsFreeDailyUses;
    }

    private IEnumerator LuckyCatEffectCoroutine()
    {
        IsLuckyCatActive = true;
        CoinMultiplier = luckyCatCoinMultiplier;
        OnBoosterDataChanged?.Invoke();

        yield return new WaitForSeconds(luckyCatDuration);

        CoinMultiplier = 1f;
        IsLuckyCatActive = false;
        luckyCatEffectCoroutine = null;
        OnBoosterDataChanged?.Invoke();
    }

    private void CheckDailyReset()
    {
        string lastResetDate = PlayerPrefs.GetString("TurboPaws_LastResetDate", "");
        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (lastResetDate != todayDate)
        {
            TurboPawsUsedToday = 0;
            PlayerPrefs.SetString("TurboPaws_LastResetDate", todayDate);
            SaveProgress();
            OnBoosterDataChanged?.Invoke();
        }
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("Inventory_LuckyCat", LuckyCatInventory);
        PlayerPrefs.SetInt("TurboPaws_UsedToday", TurboPawsUsedToday);
    }

    private void LoadProgress()
    {
        LuckyCatInventory = PlayerPrefs.GetInt("Inventory_LuckyCat", -1);
        if (LuckyCatInventory == -1)
        {
            LuckyCatInventory = initialLuckyCatAmount;
        }

        TurboPawsUsedToday = PlayerPrefs.GetInt("TurboPaws_UsedToday", 0);
        CheckDailyReset();
        OnBoosterDataChanged?.Invoke();
    }
}