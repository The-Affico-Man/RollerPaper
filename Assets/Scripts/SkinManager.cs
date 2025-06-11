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
        if (defaultSkin != null && availableSkins.Contains(defaultSkin))
        {
            SetCurrentSkin(defaultSkin);
        }
        else if (availableSkins.Count > 0)
        {
            SetCurrentSkin(availableSkins[0]);
        }
        else
        {
            Debug.LogError("SkinManager has no available skins!");
        }
    }

    public void SetCurrentSkin(CatSkin newSkin)
    {
        int skinIndex = availableSkins.IndexOf(newSkin);
        if (skinIndex != -1)
        {
            CurrentSkin = newSkin;
            currentSkinIndex = skinIndex;
            Debug.Log($"Current cat skin set to: {CurrentSkin.skinName}");
        }
        else
        {
            Debug.LogError($"Attempted to set a skin '{newSkin.name}' that is not in the availableSkins list!");
        }
    }

    // --- THIS IS THE NEW METHOD ---
    /// <summary>
    /// Cycles to the next skin in the 'availableSkins' list.
    /// If it reaches the end, it loops back to the beginning.
    /// </summary>
    public void CycleToNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0)
        {
            Debug.LogWarning("No skins available to cycle through.");
            return;
        }

        // Increment the index, and if it goes past the end of the list, loop back to 0.
        currentSkinIndex++;
        if (currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }

        // Set the new current skin based on the updated index.
        SetCurrentSkin(availableSkins[currentSkinIndex]);
    }
    // ----------------------------
}