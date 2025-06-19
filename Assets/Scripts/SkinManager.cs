using System.Collections.Generic;
using UnityEngine;
using System.Linq; // We need this for LINQ queries

public class SkinManager : MonoBehaviour
{
    public List<CatSkin> availableSkins;
    public CatSkin defaultSkin;
    public CatSkin CurrentSkin { get; private set; }
    public static SkinManager Instance { get; private set; }

    private HashSet<CatSkin> unlockedSkins;

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
        // Initialize the set, but don't load here.
        unlockedSkins = new HashSet<CatSkin>();
    }

    public void SetCurrentSkin(CatSkin newSkin)
    {
        if (newSkin == null || !availableSkins.Contains(newSkin) || !IsSkinUnlocked(newSkin))
        {
            Debug.LogWarning($"Attempted to equip a locked or invalid cat skin: {newSkin?.name}. Equipping default instead.");
            // Fallback to default if the requested skin is invalid
            CurrentSkin = defaultSkin;
            return;
        }
        CurrentSkin = newSkin;
        Debug.Log($"Current cat skin set to: {CurrentSkin.skinName}");
    }

    public bool IsSkinUnlocked(CatSkin skin)
    {
        return unlockedSkins != null && unlockedSkins.Contains(skin);
    }

    public void UnlockSkin(CatSkin skin)
    {
        if (skin != null && unlockedSkins != null && !unlockedSkins.Contains(skin))
        {
            unlockedSkins.Add(skin);
            Debug.Log($"Cat skin unlocked: {skin.skinName}");
        }
    }

    // --- NEW SAVE/LOAD METHODS ---

    public void SaveProgress()
    {
        // 1. Save the equipped skin's name
        if (CurrentSkin != null)
        {
            PlayerPrefs.SetString(PlayerPrefsKeys.EquippedPawSkin, CurrentSkin.skinName);
        }

        // 2. Save the list of unlocked skins
        // We convert the list of skin names into a single string like "Default|Calico|Tuxedo"
        string unlockedSkinsString = string.Join("|", unlockedSkins.Select(s => s.skinName));
        PlayerPrefs.SetString(PlayerPrefsKeys.UnlockedPawSkins, unlockedSkinsString);
    }

    public void LoadProgress()
    {
        // 1. Load the unlocked skins string
        string unlockedSkinsString = PlayerPrefs.GetString(PlayerPrefsKeys.UnlockedPawSkins);
        List<string> unlockedNames = new List<string>(unlockedSkinsString.Split('|'));

        // 2. Populate the unlockedSkins HashSet
        unlockedSkins.Clear();
        // Always unlock the default skin as a fallback.
        if (defaultSkin != null)
        {
            unlockedSkins.Add(defaultSkin);
        }
        foreach (CatSkin skin in availableSkins)
        {
            if (unlockedNames.Contains(skin.skinName))
            {
                unlockedSkins.Add(skin);
            }
        }

        // 3. Load and set the equipped skin
        string equippedSkinName = PlayerPrefs.GetString(PlayerPrefsKeys.EquippedPawSkin, defaultSkin?.skinName);
        CatSkin equippedSkin = availableSkins.Find(s => s.skinName == equippedSkinName);

        // Set the skin. The SetCurrentSkin method already handles fallbacks.
        SetCurrentSkin(equippedSkin);
    }

    // Debug cycle method remains unchanged
    #region Unchanged Debug Cycle
    public void CycleToNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0)
        {
            Debug.LogWarning("No skins available to cycle through.");
            return;
        }
        int currentSkinIndex = availableSkins.IndexOf(CurrentSkin);
        currentSkinIndex++;
        if (currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }
        // This debug tool will still equip locked skins, which is fine for testing.
        SetCurrentSkin(availableSkins[currentSkinIndex]);
    }
    #endregion
}