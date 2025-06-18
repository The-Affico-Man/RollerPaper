using System.Collections.Generic;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    [Tooltip("The list of all available cat skins you have created.")]
    public List<CatSkin> availableSkins;

    [Tooltip("The skin that will be used by default when the game starts.")]
    public CatSkin defaultSkin;

    public CatSkin CurrentSkin { get; private set; }
    public static SkinManager Instance { get; private set; }

    private int currentSkinIndex = -1;

    // --- THIS IS THE NEW LOGIC TO TRACK UNLOCKS ---
    private HashSet<CatSkin> unlockedSkins;
    // ---------------------------------------------

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

        // Initialize the set when the game starts.
        unlockedSkins = new HashSet<CatSkin>();
    }

    private void Start()
    {
        // --- MODIFIED: Automatically unlock the default skin ---
        if (defaultSkin != null && availableSkins.Contains(defaultSkin))
        {
            UnlockSkin(defaultSkin); // Ensure the default is always unlocked
            SetCurrentSkin(defaultSkin);
        }
        else if (availableSkins.Count > 0)
        {
            UnlockSkin(availableSkins[0]); // Fallback to the first skin
            SetCurrentSkin(availableSkins[0]);
        }
        else
        {
            Debug.LogError("SkinManager has no available skins!");
        }
        // ----------------------------------------------------
    }

    public void SetCurrentSkin(CatSkin newSkin)
    {
        // --- MODIFIED: Check if the skin is unlocked before equipping ---
        if (newSkin == null || !availableSkins.Contains(newSkin) || !IsSkinUnlocked(newSkin))
        {
            Debug.LogWarning($"Attempted to equip a locked or invalid cat skin: {newSkin?.name}");
            return;
        }
        // ----------------------------------------------------------------

        int skinIndex = availableSkins.IndexOf(newSkin);
        if (skinIndex != -1)
        {
            CurrentSkin = newSkin;
            currentSkinIndex = skinIndex;
            Debug.Log($"Current cat skin set to: {CurrentSkin.skinName}");
        }
    }

    // --- THIS IS THE NEW METHOD THE SHOP NEEDS ---
    /// <summary>
    /// Checks if a specific skin has been added to the unlocked set.
    /// </summary>
    public bool IsSkinUnlocked(CatSkin skin)
    {
        return unlockedSkins != null && unlockedSkins.Contains(skin);
    }
    // ---------------------------------------------

    // --- THIS IS THE NEW METHOD FOR THE SHOP TO CALL ---
    /// <summary>
    /// Adds a skin to the unlocked set. This would be called after a successful purchase.
    /// </summary>
    public void UnlockSkin(CatSkin skin)
    {
        if (skin != null && unlockedSkins != null && !unlockedSkins.Contains(skin))
        {
            unlockedSkins.Add(skin);
            Debug.Log($"Cat skin unlocked: {skin.skinName}");
        }
    }
    // --------------------------------------------------


    // Your existing CycleToNextSkin method is unchanged and correct.
    #region Unchanged Working Code
    public void CycleToNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0)
        {
            Debug.LogWarning("No skins available to cycle through.");
            return;
        }

        currentSkinIndex++;
        if (currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }

        // NOTE: This will still cycle through ALL skins, not just unlocked ones.
        // This is fine for a debug menu, but for a player-facing feature,
        // you would want to modify this to only cycle through the 'unlockedSkins' list.
        SetCurrentSkin(availableSkins[currentSkinIndex]);
    }
    #endregion
}