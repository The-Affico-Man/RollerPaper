using System.Collections.Generic;
using UnityEngine;
using TMPro; // Make sure you have this


public class ContinuousPaperManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI paperLengthText;
    private SwipeController swipeController; // Reference to the SwipeController to get total distance

    [Header("Paper Settings")]
    public GameObject longPaperPrefab; // A long paper tile model (e.g., 10 segments long)
    public Transform paperSpawnPoint; // Where new paper tiles spawn (at/above the toilet roll)
    public float paperTileLength = 1f; // Length of each long paper tile (MUST match prefab's actual length)

    [Header("Visibility Control")]
    public Transform toiletPaperRoll; // The toilet paper roll position
    public float hideDistance = 0.5f; // How far above the roll to hide paper
    public LayerMask paperLayer = 1; // Layer for paper objects

    [Header("Culling Settings")]
    public float cullDistance = 5f; // Remove tiles this far below camera
    public Camera playerCamera; // Reference to the main camera
    public int maxActiveTiles = 10; // Maximum number of active paper tiles allowed in the scene

    [Header("Spawning Control")]
    // Defines how far the top of the paper chain can drop below the spawn point before a new tile is spawned.
    // A small positive value (e.g., 0.05f) helps prevent visible gaps.
    public float spawnThreshold = 0.05f;

    private List<GameObject> activePaperTiles = new List<GameObject>(); // List to keep track of active paper tiles
    private PaperRoller paperRoller; // Reference to the PaperRoller script (moves the paper)
    private GameObject clippingPlane; // A conceptual object for hiding paper above the roll (requires custom shader for true clipping)

    void Start()
    {
        // Ensure required components are found and log errors if not.
        swipeController = FindFirstObjectByType<SwipeController>();
        if (swipeController == null)
        {
            Debug.LogError("SwipeController not found! Paper length UI will not update correctly.", this);
        }

        paperRoller = FindFirstObjectByType<PaperRoller>();
        if (paperRoller == null)
        {
            Debug.LogError("PaperRoller not found! Paper will not move.", this);
        }

        playerCamera = Camera.main ?? FindFirstObjectByType<Camera>(); // Try to get main camera, fall back to any camera
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera not found! Culling may not work correctly.", this);
        }

        // Setup the clipping system to hide paper above the roll.
        SetupPaperClipping();

        // Spawn initial paper tiles to ensure there's always paper visible when the game starts.
        SpawnInitialPaperTiles();
    }

    void Update()
    {
        // Continuously check for new paper tile spawning.
        UpdatePaperSpawning();
        // Continuously check and remove off-screen paper tiles.
        CullOffScreenTiles();
        // Update the UI text displaying the paper length.
        UpdatePaperLengthUI();
    }

    /// <summary>
    /// Updates the UI text to display the total length of paper pulled.
    /// The conversion factor (0.0002f) is based on your original code's scaling.
    /// </summary>
    void UpdatePaperLengthUI()
    {
        if (paperLengthText != null && swipeController != null)
        {
            float totalLengthMeters = swipeController.TotalSwipeDistance * 0.0002f;
            paperLengthText.text = $"Paper Pulled: {totalLengthMeters:F2} m";
        }
    }

    /// <summary>
    /// Sets up a conceptual clipping volume above the toilet paper roll.
    /// For true visual clipping of meshes, a custom shader or stencil buffer setup is typically required.
    /// This currently creates an invisible GameObject to mark the area.
    /// </summary>
    void SetupPaperClipping()
    {
        GameObject clipVolume = new GameObject("PaperClipVolume");
        // Position the center of the clipping volume above the toilet paper roll.
        clipVolume.transform.position = toiletPaperRoll.position + Vector3.up * hideDistance;
        // Scale the volume to encompass the area where paper should be hidden.
        clipVolume.transform.localScale = new Vector3(2f, hideDistance * 2f, 2f);

        // Add a box collider as a visual reference in the editor (can be disabled or removed at runtime).
        BoxCollider clipBox = clipVolume.AddComponent<BoxCollider>();
        clipBox.isTrigger = true; // Set as trigger to not interfere with physics interactions.

        clippingPlane = clipVolume; // Store reference for potential future use (e.g., passing to a shader).

        Debug.Log($"Clipping volume created at: {clipVolume.transform.position}", this);
    }

    /// <summary>
    /// Spawns an initial set of paper tiles when the game starts.
    /// Ensures there's always paper visible from the start.
    /// </summary>
    void SpawnInitialPaperTiles()
    {
        // Spawn at least one tile to begin with if the list is empty.
        // The UpdatePaperSpawning will handle spawning subsequent tiles as needed.
        if (activePaperTiles.Count == 0)
        {
            SpawnPaperTile();
        }
    }

    /// <summary>
    /// Instantiates a new paper tile prefab and adds it to the list of active tiles.
    /// Positions the new tile directly above the current topmost tile, or at the spawn point if it's the first tile.
    /// </summary>
    void SpawnPaperTile()
    {
        // Prevent spawning if the maximum number of active tiles has been reached to optimize performance.
        if (activePaperTiles.Count >= maxActiveTiles)
        {
            Debug.LogWarning("Max active paper tiles reached. Not spawning new tile.", this);
            return;
        }

        Vector3 spawnPos;

        // If no tiles exist yet, spawn the very first one at the defined paperSpawnPoint.
        if (activePaperTiles.Count == 0)
        {
            spawnPos = paperSpawnPoint.position;
        }
        else
        {
            // If tiles already exist, get the current topmost tile (at index 0).
            GameObject firstTile = activePaperTiles[0];
            // Safety check: ensure the reference is not null.
            if (firstTile == null)
            {
                Debug.LogError("First tile in activePaperTiles list is null. Cannot spawn new tile.", this);
                return;
            }
            // Position the new tile directly above the existing topmost tile to maintain continuity.
            spawnPos = firstTile.transform.position + Vector3.up * paperTileLength;
        }

        // Instantiate the new paper tile prefab at the calculated position.
        GameObject newTile = Instantiate(longPaperPrefab, spawnPos, Quaternion.identity);

        // Set the layer of the new tile and its children recursively. This is useful
        // for camera culling, custom rendering, or collision filtering.
        SetLayerRecursively(newTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));

        // Add the new tile to the beginning of the list, making it the new topmost tile.
        activePaperTiles.Insert(0, newTile);

        Debug.Log($"Spawned paper tile at: {spawnPos}, Total active: {activePaperTiles.Count}", this);
    }

    /// <summary>
    /// Checks if a new paper tile needs to be spawned. This is crucial for continuous paper flow.
    /// A new tile is spawned when the visual top of the current paper chain drops
    /// below the 'paperSpawnPoint' by a specified 'spawnThreshold'.
    /// </summary>
    void UpdatePaperSpawning()
    {
        // Only proceed if there are active tiles and the PaperRoller is available.
        if (activePaperTiles.Count == 0 || paperRoller == null)
            return;

        GameObject topTile = activePaperTiles[0]; // Get the current topmost paper tile.

        // Calculate the effective 'top edge' of the current paper chain.
        // This is the center of the topmost tile plus half of its length upwards.
        float topEdgeOfCurrentPaperChain = topTile.transform.position.y + (paperTileLength / 2f);

        // Compare the 'top edge' with the 'paperSpawnPoint' (where new paper should emerge).
        // If the 'top edge' has fallen below the spawn point by more than 'spawnThreshold',
        // it means there's a visible gap forming, and a new tile needs to be spawned.
        if (topEdgeOfCurrentPaperChain < (paperSpawnPoint.position.y - spawnThreshold))
        {
            SpawnPaperTile(); // Call the method to instantiate a new tile.
        }
    }

    /// <summary>
    /// Removes paper tiles that have moved too far below the player's camera.
    /// This optimizes performance and memory usage by destroying off-screen objects.
    /// </summary>
    void CullOffScreenTiles()
    {
        // Only proceed if a camera is available and there are active paper tiles.
        if (playerCamera == null || activePaperTiles.Count == 0)
            return;

        // Calculate the Y-position below which tiles should be considered off-screen and culled.
        float cullY = playerCamera.transform.position.y - cullDistance;

        // Iterate backwards through the list to safely remove elements while iterating.
        for (int i = activePaperTiles.Count - 1; i >= 0; i--)
        {
            GameObject tile = activePaperTiles[i];
            // If a tile reference is null (e.g., object was already destroyed by other means),
            // simply remove its entry from the list.
            if (tile == null)
            {
                activePaperTiles.RemoveAt(i);
                continue;
            }

            // If the tile's position is below the cull line, remove it from the list and destroy its GameObject.
            if (tile.transform.position.y < cullY)
            {
                activePaperTiles.RemoveAt(i);
                Destroy(tile);
                Debug.Log($"Culled paper tile, remaining: {activePaperTiles.Count}", this);
            }
        }
    }

    /// <summary>
    /// This method is a placeholder for actual clipping logic.
    /// For true visual clipping (hiding parts of a mesh), you would typically use a custom shader
    /// that checks world position against a clipping plane, or manipulate the mesh vertices directly.
    /// </summary>
    void ApplyClippingToTile(GameObject tile)
    {
        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Example of how you might interact with a custom shader's clipping plane parameter:
            // renderer.material.SetVector("_ClippingPlane", new Vector4(0, 1, 0, -clippingPlane.transform.position.y));
        }
    }

    /// <summary>
    /// An alternative, more complex method for visual clipping using multiple cameras (not fully implemented).
    /// This typically involves setting up a secondary camera to render specific layers with its own clipping planes
    /// or using render textures/stencil buffers.
    /// </summary>
    void SetupCameraMasking()
    {
        // This method is left as a conceptual outline as it's not the primary clipping approach implemented here.
        GameObject maskCameraGO = new GameObject("PaperMaskCamera");
        Camera maskCamera = maskCameraGO.AddComponent<Camera>();

        maskCamera.transform.position = playerCamera.transform.position;
        maskCamera.transform.rotation = playerCamera.transform.rotation;

        maskCamera.cullingMask = paperLayer; // This camera would only render objects on the paper layer.
        maskCamera.clearFlags = CameraClearFlags.Nothing; // It wouldn't clear the buffer, just draw on top.
        maskCamera.depth = playerCamera.depth + 1; // Render after the main camera.

        // Further implementation would be needed to set up custom clipping planes for this camera.
    }

    /// <summary>
    /// Recursively sets the layer for a GameObject and all its children.
    /// This is a utility function often used for organizing objects for rendering pipelines,
    /// physics interactions, or custom culling.
    /// </summary>
    /// <param name="obj">The GameObject to set the layer for.</param>
    /// <param name="layer">The integer layer index (e.g., from `LayerMask.NameToLayer("YourLayer")`).</param>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Public methods for external access to manager data.
    public int GetActiveTileCount() => activePaperTiles.Count;

    /// <summary>
    /// Clears all active paper tiles from the scene immediately.
    /// This method can be invoked from the Unity Editor by right-clicking on the component.
    /// </summary>
    [ContextMenu("Clear All Paper")]
    public void ClearAllPaper()
    {
        // Iterate through the list of active tiles and destroy each GameObject.
        foreach (GameObject tile in activePaperTiles)
        {
            if (tile != null)
                Destroy(tile); // Destroy the actual Unity GameObject instance.
        }
        activePaperTiles.Clear(); // Clear all references from the list.
        Debug.Log("Cleared all paper tiles", this);
    }

    /// <summary>
    /// Visualizes key points and areas in the Unity Editor using Gizmos.
    /// This helps in debugging and understanding the logic in the scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        // Show spawn point as a green wire cube.
        if (paperSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(paperSpawnPoint.position, Vector3.one * 0.2f);
        }

        // Show clipping area as a red wire cube.
        if (toiletPaperRoll != null)
        {
            Gizmos.color = Color.red;
            Vector3 clipCenter = toiletPaperRoll.position + Vector3.up * hideDistance;
            Gizmos.DrawWireCube(clipCenter, new Vector3(2f, hideDistance * 2f, 2f));
        }

        // Show cull line as a yellow line across the X-axis.
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            float cullY = playerCamera.transform.position.y - cullDistance;
            Gizmos.DrawLine(new Vector3(-3, cullY, 0), new Vector3(3, cullY, 0));
        }

        // Show active tiles as small blue wire cubes at their center positions.
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
