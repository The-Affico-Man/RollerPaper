using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PaperSkinManager : MonoBehaviour
{
    [Header("Skin Data")]
    public List<PaperSkin> availableSkins;
    public PaperSkin defaultSkin;

    [Header("Scene References")]
    public MeshRenderer paperRollMeshRenderer;
    // --- THIS IS THE FIX (Part 1): Add a reference to the paper manager ---
    private ContinuousPaperManager continuousPaperManager;

    public PaperSkin CurrentSkin { get; private set; }
    public static PaperSkinManager Instance { get; private set; }

    private HashSet<PaperSkin> unlockedSkins;

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
        unlockedSkins = new HashSet<PaperSkin>();

        // --- THIS IS THE FIX (Part 2): Find the reference at startup ---
        continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();
    }

    private void Start()
    {
        if (paperRollMeshRenderer == null)
        {
            Debug.LogError("PaperSkinManager: The 'Paper Roll Mesh Renderer' has not been assigned!");
        }
    }

    public void SetCurrentSkin(PaperSkin newSkin)
    {
        if (newSkin == null || !availableSkins.Contains(newSkin) || !IsSkinUnlocked(newSkin))
        {
            Debug.LogWarning($"Attempted to equip a locked or invalid paper skin: {newSkin?.name}. Equipping default instead.");
            CurrentSkin = defaultSkin;
        }
        else
        {
            CurrentSkin = newSkin;
        }

        Debug.Log($"Paper skin changed to: {CurrentSkin.skinName}");
        if (paperRollMeshRenderer != null && CurrentSkin.rollMaterial != null)
        {
            paperRollMeshRenderer.material = CurrentSkin.rollMaterial;
        }

        // --- THIS IS THE FIX (Part 3): Call the new method to update existing tiles ---
        continuousPaperManager?.UpdateAllActiveTilesSkin();
    }

    // The rest of your script is completely unchanged.
    #region Unchanged Methods
    public bool IsSkinUnlocked(PaperSkin skin) { return unlockedSkins != null && unlockedSkins.Contains(skin); }
    public void UnlockSkin(PaperSkin skin) { if (skin != null && unlockedSkins != null && !unlockedSkins.Contains(skin)) { unlockedSkins.Add(skin); Debug.Log($"Paper skin unlocked: {skin.skinName}"); } }
    public void SaveProgress() { if (CurrentSkin != null) { PlayerPrefs.SetString(PlayerPrefsKeys.EquippedPaperSkin, CurrentSkin.skinName); } string unlockedSkinsString = string.Join("|", unlockedSkins.Select(s => s.skinName)); PlayerPrefs.SetString(PlayerPrefsKeys.UnlockedPaperSkins, unlockedSkinsString); }
    public void LoadProgress() { string unlockedSkinsString = PlayerPrefs.GetString(PlayerPrefsKeys.UnlockedPaperSkins); List<string> unlockedNames = new List<string>(unlockedSkinsString.Split('|')); unlockedSkins.Clear(); if (defaultSkin != null) { unlockedSkins.Add(defaultSkin); } foreach (PaperSkin skin in availableSkins) { if (unlockedNames.Contains(skin.skinName)) { unlockedSkins.Add(skin); } } string equippedSkinName = PlayerPrefs.GetString(PlayerPrefsKeys.EquippedPaperSkin, defaultSkin?.skinName); PaperSkin equippedSkin = availableSkins.Find(s => s.skinName == equippedSkinName); SetCurrentSkin(equippedSkin); }
    public void CycleToNextSkin() { if (availableSkins == null || availableSkins.Count == 0) return; int currentSkinIndex = availableSkins.IndexOf(CurrentSkin); currentSkinIndex++; if (currentSkinIndex >= availableSkins.Count) { currentSkinIndex = 0; } SetCurrentSkin(availableSkins[currentSkinIndex]); }
    #endregion
}