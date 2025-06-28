using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class PaperSkinManager : MonoBehaviour
{
    [Header("Skin Data")]
    public List<PaperSkin> availableSkins;
    public PaperSkin defaultSkin;
    [Header("Scene References")]
    public MeshRenderer paperRollMeshRenderer;
    private ContinuousPaperManager continuousPaperManager;

    public PaperSkin CurrentSkin { get; private set; }
    public static PaperSkinManager Instance { get; private set; }
    private HashSet<PaperSkin> unlockedSkins;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
        unlockedSkins = new HashSet<PaperSkin>();
        continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();
    }

    private void Start()
    {
        if (paperRollMeshRenderer == null) { Debug.LogError("PaperSkinManager: 'Paper Roll Mesh Renderer' has not been assigned!"); }
    }

    // --- NEW "QUIET" METHOD FOR LOADING ---
    private void ApplySkinOnLoad(PaperSkin skinToApply)
    {
        if (skinToApply == null || !unlockedSkins.Contains(skinToApply))
        {
            CurrentSkin = defaultSkin;
        }
        else
        {
            CurrentSkin = skinToApply;
        }

        if (paperRollMeshRenderer != null && CurrentSkin != null && CurrentSkin.rollMaterial != null)
        {
            paperRollMeshRenderer.material = CurrentSkin.rollMaterial;
        }
    }

    // --- PUBLIC METHOD FOR PLAYER ACTIONS ---
    public void SetCurrentSkin(PaperSkin newSkin)
    {
        if (newSkin == null || !unlockedSkins.Contains(newSkin) || CurrentSkin == newSkin)
        {
            return;
        }

        CurrentSkin = newSkin;
        Debug.Log($"Player equipped Paper Skin: {CurrentSkin.skinName}");

        if (paperRollMeshRenderer != null && CurrentSkin.rollMaterial != null)
        {
            paperRollMeshRenderer.material = CurrentSkin.rollMaterial;
        }

        // Update visuals and report to challenges
        continuousPaperManager?.UpdateAllActiveTilesSkin();
        ReportSkinChangeToChallengeManager();
        ChallengeManager.Instance?.UpdateChallengeProgress(ChallengeType.ChangePaperSkin);
    }

    public void LoadProgress()
    {
        // ... (loading unlockedSkins is unchanged) ...
        #region Unchanged Load Unlocks
        string unlockedSkinsString = PlayerPrefs.GetString(PlayerPrefsKeys.UnlockedPaperSkins);
        List<string> unlockedNames = new List<string>(unlockedSkinsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        unlockedSkins.Clear();
        if (defaultSkin != null) { unlockedSkins.Add(defaultSkin); }
        foreach (PaperSkin skin in availableSkins) { if (unlockedNames.Contains(skin.skinName)) { unlockedSkins.Add(skin); } }
        #endregion

        string equippedSkinName = PlayerPrefs.GetString(PlayerPrefsKeys.EquippedPaperSkin, defaultSkin?.skinName);
        PaperSkin equippedSkin = availableSkins.Find(s => s.skinName == equippedSkinName);

        // --- THE FIX: Call the "quiet" method ---
        ApplySkinOnLoad(equippedSkin);
    }

    // All other methods are unchanged and correct.
    #region Unchanged Methods
    public void ReportSkinChangeToChallengeManager() { if (CurrentSkin != null) { ChallengeManager.Instance?.OnPaperSkinChanged(CurrentSkin.skinName); } }
    public bool IsSkinUnlocked(PaperSkin skin) { return unlockedSkins != null && unlockedSkins.Contains(skin); }
    public void UnlockSkin(PaperSkin skin) { if (skin != null && unlockedSkins != null && !unlockedSkins.Contains(skin)) { unlockedSkins.Add(skin); } }
    public void SaveProgress() { if (CurrentSkin != null) { PlayerPrefs.SetString(PlayerPrefsKeys.EquippedPaperSkin, CurrentSkin.skinName); } string unlockedSkinsString = string.Join(",", unlockedSkins.Select(s => s.skinName)); PlayerPrefs.SetString(PlayerPrefsKeys.UnlockedPaperSkins, unlockedSkinsString); }
    public void CycleToNextSkin() { if (availableSkins == null || availableSkins.Count == 0) return; int currentSkinIndex = availableSkins.IndexOf(CurrentSkin); currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Count; SetCurrentSkin(availableSkins[currentSkinIndex]); }
    #endregion
}