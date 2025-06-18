using System.Collections.Generic;
using UnityEngine;

public class PaperSkinManager : MonoBehaviour
{
    [Header("Skin Data")]
    public List<PaperSkin> availableSkins;
    public PaperSkin defaultSkin;

    [Header("Scene References")]
    public MeshRenderer paperRollMeshRenderer;

    public PaperSkin CurrentSkin { get; private set; }
    public static PaperSkinManager Instance { get; private set; }

    private int currentSkinIndex = -1;

    // --- THIS IS THE NEW LOGIC TO TRACK UNLOCKS ---
    private HashSet<PaperSkin> unlockedSkins;
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
        unlockedSkins = new HashSet<PaperSkin>();
    }

    private void Start()
    {
        if (paperRollMeshRenderer == null)
        {
            Debug.LogError("PaperSkinManager: The 'Paper Roll Mesh Renderer' has not been assigned!");
        }

        // --- MODIFIED: Automatically unlock the default skin ---
        if (defaultSkin != null)
        {
            UnlockSkin(defaultSkin); // Ensure the default is always unlocked
            SetCurrentSkin(defaultSkin);
        }
        else if (availableSkins.Count > 0)
        {
            UnlockSkin(availableSkins[0]); // Fallback to the first skin
            SetCurrentSkin(availableSkins[0]);
        }
        // ----------------------------------------------------
    }

    public void SetCurrentSkin(PaperSkin newSkin)
    {
        // --- MODIFIED: Check if the skin is unlocked before equipping ---
        if (newSkin == null || !availableSkins.Contains(newSkin) || !IsSkinUnlocked(newSkin))
        {
            Debug.LogWarning($"Attempted to equip a locked or invalid paper skin: {newSkin?.name}");
            return;
        }
        // ----------------------------------------------------------------

        CurrentSkin = newSkin;
        currentSkinIndex = availableSkins.IndexOf(newSkin);
        Debug.Log($"Paper skin changed to: {CurrentSkin.skinName}");

        if (paperRollMeshRenderer != null && CurrentSkin.rollMaterial != null)
        {
            paperRollMeshRenderer.material = CurrentSkin.rollMaterial;
        }

        UpdateExistingPaperTiles();
    }

    // --- THIS IS THE NEW METHOD THE SHOP NEEDS ---
    /// <summary>
    /// Checks if a specific skin has been added to the unlocked set.
    /// </summary>
    public bool IsSkinUnlocked(PaperSkin skin)
    {
        return unlockedSkins != null && unlockedSkins.Contains(skin);
    }
    // ---------------------------------------------

    // --- THIS IS THE NEW METHOD FOR THE SHOP TO CALL ---
    /// <summary>
    /// Adds a skin to the unlocked set. This would be called after a successful purchase.
    /// </summary>
    public void UnlockSkin(PaperSkin skin)
    {
        if (skin != null && unlockedSkins != null && !unlockedSkins.Contains(skin))
        {
            unlockedSkins.Add(skin);
            Debug.Log($"Paper skin unlocked: {skin.skinName}");
        }
    }
    // --------------------------------------------------

    // Your existing CycleToNextSkin and UpdateExistingPaperTiles methods are unchanged.
    #region Unchanged Working Code
    public void CycleToNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;
        currentSkinIndex++;
        if (currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }
        // NOTE: This might need a change later to cycle only through UNLOCKED skins.
        // For now, for the debug menu, it's fine.
        SetCurrentSkin(availableSkins[currentSkinIndex]);
    }

    private void UpdateExistingPaperTiles()
    {
        if (CurrentSkin == null) return;
        PaperTile[] activeTiles = FindObjectsByType<PaperTile>(FindObjectsSortMode.None);
        foreach (PaperTile tile in activeTiles)
        {
            tile.SetSkin(CurrentSkin.tileMaterial);
        }
    }
    #endregion
}