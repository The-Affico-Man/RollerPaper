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

    private int currentSkinIndex = -1; // Keep track of our position in the list

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
    }

    private void Start()
    {
        if (paperRollMeshRenderer == null)
        {
            Debug.LogError("PaperSkinManager: The 'Paper Roll Mesh Renderer' has not been assigned!");
        }

        if (defaultSkin != null)
        {
            SetCurrentSkin(defaultSkin);
        }
        else if (availableSkins.Count > 0)
        {
            SetCurrentSkin(availableSkins[0]);
        }
    }

    public void SetCurrentSkin(PaperSkin newSkin)
    {
        if (newSkin == null || !availableSkins.Contains(newSkin))
        {
            Debug.LogError("Attempted to set an invalid or unavailable paper skin.");
            return;
        }

        CurrentSkin = newSkin;
        currentSkinIndex = availableSkins.IndexOf(newSkin); // Update the index
        Debug.Log($"Paper skin changed to: {CurrentSkin.skinName}");

        if (paperRollMeshRenderer != null && CurrentSkin.rollMaterial != null)
        {
            paperRollMeshRenderer.material = CurrentSkin.rollMaterial;
        }

        // When the skin changes, we need to update any existing paper tiles.
        UpdateExistingPaperTiles();
    }

    // --- THIS IS THE NEW METHOD ---
    /// <summary>
    /// Cycles to the next skin in the 'availableSkins' list.
    /// </summary>
    public void CycleToNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;

        currentSkinIndex++;
        if (currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }

        SetCurrentSkin(availableSkins[currentSkinIndex]);
    }
    // ----------------------------

    /// <summary>
    /// Finds all active paper tiles and tells them to update their skin.
    /// This is important for when the player changes skins mid-game.
    /// </summary>
    private void UpdateExistingPaperTiles()
    {
        PaperTile[] activeTiles = FindObjectsByType<PaperTile>(FindObjectsSortMode.None);
        foreach (PaperTile tile in activeTiles)
        {
            tile.SetSkin(CurrentSkin.tileMaterial);
        }
    }
}