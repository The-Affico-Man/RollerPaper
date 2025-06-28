using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class SkinManager : MonoBehaviour
{
    public List<CatSkin> availableSkins;
    public CatSkin defaultSkin;
    public CatSkin CurrentSkin { get; private set; }
    public static SkinManager Instance { get; private set; }

    private HashSet<CatSkin> unlockedSkins;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
        unlockedSkins = new HashSet<CatSkin>();
    }

    // --- THIS IS THE NEW "QUIET" METHOD FOR LOADING ---
    /// <summary>
    /// Applies a skin without triggering any game events. Used only for loading saved state.
    /// </summary>
    private void ApplySkinOnLoad(CatSkin skinToApply)
    {
        if (skinToApply == null || !unlockedSkins.Contains(skinToApply))
        {
            CurrentSkin = defaultSkin;
        }
        else
        {
            CurrentSkin = skinToApply;
        }
    }

    // --- THIS IS THE PUBLIC METHOD FOR PLAYER ACTIONS ---
    /// <summary>
    /// Called when the player manually clicks to equip a skin. This triggers challenges.
    /// </summary>
    public void SetCurrentSkin(CatSkin newSkin)
    {
        // First, check if the skin is valid and not already equipped.
        if (newSkin == null || !unlockedSkins.Contains(newSkin) || CurrentSkin == newSkin)
        {
            return;
        }

        CurrentSkin = newSkin;
        Debug.Log($"Player equipped Cat Skin: {CurrentSkin.skinName}");

        // This is a player action, so we update the challenges.
        ReportSkinChangeToChallengeManager();
        ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.ChangePawSkin);
    }

    public void LoadProgress()
    {
        // ... (loading the unlockedSkins hash set is unchanged) ...
        #region Unchanged Load Unlocks
        string unlockedSkinsString = PlayerPrefs.GetString(PlayerPrefsKeys.UnlockedPawSkins);
        List<string> unlockedNames = new List<string>(unlockedSkinsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        unlockedSkins.Clear();
        if (defaultSkin != null) { unlockedSkins.Add(defaultSkin); }
        foreach (CatSkin skin in availableSkins) { if (unlockedNames.Contains(skin.skinName)) { unlockedSkins.Add(skin); } }
        #endregion

        // Load the name of the last equipped skin
        string equippedSkinName = PlayerPrefs.GetString(PlayerPrefsKeys.EquippedPawSkin, defaultSkin?.skinName);
        CatSkin equippedSkin = availableSkins.Find(s => s.skinName == equippedSkinName);

        // --- THE FIX: Call the "quiet" method that doesn't trigger challenges ---
        ApplySkinOnLoad(equippedSkin);
    }

    // All other methods are unchanged and correct.
    #region Unchanged Methods
    public bool IsSkinUnlocked(CatSkin skin) { return unlockedSkins != null && unlockedSkins.Contains(skin); }
    public void ReportSkinChangeToChallengeManager() { if (CurrentSkin != null) { ChallengeManager.Instance?.OnPawSkinChanged(CurrentSkin.skinName); } }
    public void UnlockSkin(CatSkin skin) { if (skin != null && unlockedSkins != null && !unlockedSkins.Contains(skin)) { unlockedSkins.Add(skin); } }
    public void SaveProgress() { if (CurrentSkin != null) { PlayerPrefs.SetString(PlayerPrefsKeys.EquippedPawSkin, CurrentSkin.skinName); } string unlockedSkinsString = string.Join(",", unlockedSkins.Select(s => s.skinName)); PlayerPrefs.SetString(PlayerPrefsKeys.UnlockedPawSkins, unlockedSkinsString); }
    public void CycleToNextSkin() { if (availableSkins == null || availableSkins.Count == 0) return; int currentSkinIndex = availableSkins.IndexOf(CurrentSkin); currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Count; SetCurrentSkin(availableSkins[currentSkinIndex]); }
    #endregion
}