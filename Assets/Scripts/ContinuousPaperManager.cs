using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// This is the definitive, corrected version.
/// It fixes the spawning gaps by moving the spawning logic to LateUpdate(),
/// which guarantees it runs AFTER all paper movement has been calculated for the frame.
/// This works reliably on both PC and mobile, regardless of framerate.
/// </summary>
public class ContinuousPaperManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI paperLengthText;
    private SwipeController swipeController;

    [Header("Paper Settings")]
    public GameObject longPaperPrefab;
    public Transform paperSpawnPoint;
    public float paperTileLength = 1f;

    // --- THIS SECTION WAS ACCIDENTALLY REMOVED AND IS NOW RESTORED ---
    [Header("Visibility Control")]
    public Transform toiletPaperRoll; // The toilet paper roll position
    public float hideDistance = 0.5f; // How far above the roll to hide paper
    public LayerMask paperLayer = 1;  // Layer for paper objects (THIS WAS THE MISSING LINE)
    // --------------------------------------------------------------------

    [Header("Culling Settings")]
    public float cullDistance = 5f;
    public Camera playerCamera;
    public int maxActiveTiles = 15; // Increased slightly for a bigger buffer on mobile

    // Unused, but kept for script compatibility
    [Header("Spawning Control")]
    public float spawnThreshold = 0.5f;

    // References to other components, unchanged
    private List<GameObject> activePaperTiles = new List<GameObject>();
    private PaperRoller paperRoller;

    void Start()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        paperRoller = FindFirstObjectByType<PaperRoller>();
        playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        SpawnInitialPaperTiles();
    }

    void Update()
    {
        // UI and culling can safely happen in Update().
        CullOffScreenTiles();
        UpdatePaperLengthUI();
    }

    /// <summary>
    /// LateUpdate runs AFTER all Update() calls. This is the key to the fix.
    /// We check for and fill gaps here, after the PaperRoller has already moved the paper.
    /// </summary>
    void LateUpdate()
    {
        UpdatePaperSpawning();
    }

    // THIS METHOD IS UNCHANGED, as requested.
    void UpdatePaperLengthUI()
    {
        if (paperLengthText != null && swipeController != null)
        {
            float totalLengthMeters = swipeController.TotalSwipeDistance * 0.0002f;
            paperLengthText.text = $"Paper Pulled: {totalLengthMeters:F2} m";
        }
    }

    // This is the robust spawning logic. It now runs at the correct time (LateUpdate).
    void UpdatePaperSpawning()
    {
        if (activePaperTiles.Count == 0) return;

        // This 'while' loop is the bulletproof part.
        // It checks if the current top tile is below the spawn point.
        // If so, it spawns a new tile and IMMEDIATELY re-checks, filling any size gap in a single frame.
        while (activePaperTiles[0].transform.position.y < paperSpawnPoint.position.y)
        {
            if (activePaperTiles.Count >= maxActiveTiles) break; // Stop if we hit the limit

            SpawnOneTileAtTop();
        }
    }

    void SpawnOneTileAtTop()
    {
        if (activePaperTiles.Count == 0) return;

        // Calculate spawn position based on the current top tile.
        GameObject topTile = activePaperTiles[0];
        Vector3 spawnPos = topTile.transform.position + Vector3.up * paperTileLength;

        GameObject newTile = Instantiate(longPaperPrefab, spawnPos, Quaternion.identity);

        // This line now works because 'paperLayer' is declared again.
        SetLayerRecursively(newTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));

        // Insert at the front of the list, making it the new top tile.
        activePaperTiles.Insert(0, newTile);
    }

    void SpawnInitialPaperTiles()
    {
        ClearAllPaper();

        // Spawn the first tile at the spawn point.
        GameObject firstTile = Instantiate(longPaperPrefab, paperSpawnPoint.position, Quaternion.identity);
        activePaperTiles.Add(firstTile);
        SetLayerRecursively(firstTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));

        // Spawn a few more tiles upwards to create a small starting buffer.
        for (int i = 0; i < 3; i++)
        {
            if (activePaperTiles.Count >= maxActiveTiles) break;
            SpawnOneTileAtTop();
        }
    }

    void CullOffScreenTiles()
    {
        if (playerCamera == null || activePaperTiles.Count == 0) return;

        float cullY = playerCamera.transform.position.y - cullDistance;

        // More efficient to use a while loop from the bottom
        while (activePaperTiles.Count > 0)
        {
            int lastIndex = activePaperTiles.Count - 1;
            GameObject tile = activePaperTiles[lastIndex];

            if (tile == null || tile.transform.position.y < cullY)
            {
                if (tile != null) Destroy(tile);
                activePaperTiles.RemoveAt(lastIndex);
            }
            else
            {
                // If the bottom-most tile is safe, all others are too.
                break;
            }
        }
    }

    #region Unchanged Helper Methods
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    [ContextMenu("Clear All Paper")]
    public void ClearAllPaper()
    {
        // Make a copy to iterate over, as destroying modifies the original list.
        var tilesToDestroy = new List<GameObject>(activePaperTiles);
        foreach (var tile in tilesToDestroy)
        {
            if (tile != null)
            {
                if (Application.isPlaying) Destroy(tile);
                else DestroyImmediate(tile);
            }
        }
        activePaperTiles.Clear();
    }
    #endregion
}