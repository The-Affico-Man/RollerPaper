using System.Collections.Generic;
using UnityEngine;

public class ContinuousPaperManager : MonoBehaviour
{
    [Header("Paper Settings")]
    public GameObject longPaperPrefab; // A long paper tile model (e.g., 10 segments long)
    public Transform paperSpawnPoint; // Where new paper tiles spawn (at/above the toilet roll)
    public float paperTileLength = 1f; // Length of each long paper tile

    [Header("Visibility Control")]
    public Transform toiletPaperRoll; // The toilet paper roll position
    public float hideDistance = 0.5f; // How far above the roll to hide paper
    public LayerMask paperLayer = 1; // Layer for paper objects

    [Header("Culling Settings")]
    public float cullDistance = 5f; // Remove tiles this far below camera
    public Camera playerCamera;
    public int maxActiveTiles = 10; // Maximum number of active paper tiles

    [Header("Spawning Control")]
    public float spawnThreshold = 0.5f; // Spawn new tile when front tile moves this far down

    private List<GameObject> activePaperTiles = new List<GameObject>();
    private PaperRoller paperRoller;
    private GameObject clippingPlane; // Invisible object to hide paper above roll

    void Start()
    {
        // Find components
        paperRoller = FindFirstObjectByType<PaperRoller>();
        //if (paperCamera == null)
            playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        // Create clipping system
        SetupPaperClipping();

        // Spawn initial paper tiles
        SpawnInitialPaperTiles();
    }

    void Update()
    {
        UpdatePaperSpawning();
        CullOffScreenTiles();
    }

    void SetupPaperClipping()
    {
        // Method 1: Create a clipping plane using a shader/material
        // We'll create an invisible box above the toilet roll that masks the paper

        GameObject clipVolume = new GameObject("PaperClipVolume");
        clipVolume.transform.position = toiletPaperRoll.position + Vector3.up * hideDistance;
        clipVolume.transform.localScale = new Vector3(2f, hideDistance * 2f, 2f);

        // Add a box collider for reference (disable if not needed)
        BoxCollider clipBox = clipVolume.AddComponent<BoxCollider>();
        clipBox.isTrigger = true;

        // Method 2: We'll handle visibility programmatically by adjusting paper materials
        clippingPlane = clipVolume;

        Debug.Log($"Clipping volume created at: {clipVolume.transform.position}");
    }

    void SpawnInitialPaperTiles()
    {
        // Spawn 2-3 tiles to start with
        for (int i = 0; i < 3; i++)
        {
            SpawnPaperTile();
        }
    }

    void SpawnPaperTile()
    {
        if (activePaperTiles.Count >= maxActiveTiles)
            return;

        Vector3 spawnPos;

        if (activePaperTiles.Count == 0)
        {
            // First tile spawns at the spawn point
            spawnPos = paperSpawnPoint.position;
        }
        else
        {
            // New tiles spawn above the first tile
            GameObject firstTile = activePaperTiles[0];
            spawnPos = firstTile.transform.position + Vector3.up * paperTileLength;
        }

        GameObject newTile = Instantiate(longPaperPrefab, spawnPos, Quaternion.identity);

        // Set layer for clipping
        SetLayerRecursively(newTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));

        // Add to front of list (newest tiles at front)
        activePaperTiles.Insert(0, newTile);

        // Apply clipping effect
        ApplyClippingToTile(newTile);

        Debug.Log($"Spawned paper tile at: {spawnPos}, Total active: {activePaperTiles.Count}");
    }

    void UpdatePaperSpawning()
    {
        if (activePaperTiles.Count == 0 || paperRoller == null)
            return;

        // Check if we need to spawn a new tile
        // When the first (top) tile moves down enough, spawn a new one above it
        GameObject topTile = activePaperTiles[0];
        if (topTile != null)
        {
            float distanceFromSpawn = paperSpawnPoint.position.y - topTile.transform.position.y;

            if (distanceFromSpawn >= spawnThreshold)
            {
                SpawnPaperTile();
            }
        }
    }

    void CullOffScreenTiles()
    {
        if (playerCamera == null || activePaperTiles.Count == 0)
            return;

        float cullY = playerCamera.transform.position.y - cullDistance;

        // Remove tiles that are too far below the camera (from the end of the list)
        for (int i = activePaperTiles.Count - 1; i >= 0; i--)
        {
            GameObject tile = activePaperTiles[i];
            if (tile == null)
            {
                activePaperTiles.RemoveAt(i);
                continue;
            }

            if (tile.transform.position.y < cullY)
            {
                activePaperTiles.RemoveAt(i);
                Destroy(tile);
                Debug.Log($"Culled paper tile, remaining: {activePaperTiles.Count}");
            }
        }
    }

    void ApplyClippingToTile(GameObject tile)
    {
        // Method 1: Shader-based clipping (requires custom shader)
        // Method 2: Programmatic visibility control

        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // We'll handle clipping in the shader or by adjusting the mesh
            // For now, we'll use a simple approach with materials

            // Store original material for restoration if needed
            Material originalMat = renderer.material;

            // You can replace this with a custom shader that clips based on world position
            // For now, we'll use a simple fade approach
        }
    }

    // Alternative approach: Use a camera mask or stencil buffer
    void SetupCameraMasking()
    {
        // Create a secondary camera that renders only the visible part of paper
        GameObject maskCameraGO = new GameObject("PaperMaskCamera");
        Camera maskCamera = maskCameraGO.AddComponent<Camera>();

        maskCamera.transform.position = playerCamera.transform.position;
        maskCamera.transform.rotation = playerCamera.transform.rotation;

        maskCamera.cullingMask = paperLayer;
        maskCamera.clearFlags = CameraClearFlags.Nothing;
        maskCamera.depth = playerCamera.depth + 1;

        // Set up clipping plane
        Vector4 clipPlane = new Vector4(0, 1, 0, -toiletPaperRoll.position.y);
        maskCamera.worldToCameraMatrix = maskCamera.worldToCameraMatrix;

        // This camera will only render paper below the toilet roll
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Public methods for external access
    public int GetActiveTileCount() => activePaperTiles.Count;

    public float GetTotalPaperLength()
    {
        // Calculate based on how far the paper has moved
        if (paperRoller != null && paperSpawnPoint != null)
        {
            return Mathf.Abs(paperSpawnPoint.position.y - paperRoller.transform.position.y);
        }
        return 0f;
    }

    [ContextMenu("Clear All Paper")]
    public void ClearAllPaper()
    {
        foreach (GameObject tile in activePaperTiles)
        {
            if (tile != null)
                Destroy(tile);
        }
        activePaperTiles.Clear();
        Debug.Log("Cleared all paper tiles");
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        // Show spawn point
        if (paperSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(paperSpawnPoint.position, Vector3.one * 0.2f);
        }

        // Show clipping area
        if (toiletPaperRoll != null)
        {
            Gizmos.color = Color.red;
            Vector3 clipCenter = toiletPaperRoll.position + Vector3.up * hideDistance;
            Gizmos.DrawWireCube(clipCenter, new Vector3(2f, hideDistance * 2f, 2f));
        }

        // Show cull line
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            float cullY = playerCamera.transform.position.y - cullDistance;
            Gizmos.DrawLine(new Vector3(-3, cullY, 0), new Vector3(3, cullY, 0));
        }

        // Show active tiles
        Gizmos.color = Color.blue;
        foreach (GameObject tile in activePaperTiles)
        {
            if (tile != null)
            {
                Gizmos.DrawWireCube(tile.transform.position, Vector3.one * 0.1f);
            }
        }
    }
}