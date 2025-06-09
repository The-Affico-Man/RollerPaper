using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ContinuousPaperManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI paperLengthText;

    private PaperRoller paperRoller;

    [Header("Scoring")]
    [Tooltip("The real-world length of one paper tile prefab in meters. (e.g., 4 squares * 10cm/square = 0.4m)")]
    public float realWorldMetersPerTile = 0.4f;

    [Header("Paper Settings")]
    public GameObject longPaperPrefab;
    public Transform paperSpawnPoint;
    [Tooltip("The length of the paper tile in Unity's world units. This should match the prefab's model size.")]
    public float paperTileLength = 1f;

    [Header("Visibility Control")]
    public Transform toiletPaperRoll;
    public float hideDistance = 0.5f;
    public LayerMask paperLayer = 1;

    [Header("Culling Settings")]
    public float cullDistance = 5f;
    public Camera playerCamera;
    public int maxActiveTiles = 15;

    private List<GameObject> activePaperTiles = new List<GameObject>();

    void Start()
    {
        paperRoller = GetComponent<PaperRoller>();
        if (paperRoller == null) { Debug.LogError("ContinuousPaperManager could not find the PaperRoller script on the same GameObject!"); }

        playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        SpawnInitialPaperTiles();
    }

    void Update()
    {
        CullOffScreenTiles();
        UpdatePaperLengthUI();
    }

    void LateUpdate()
    {
        UpdatePaperSpawning();
    }

    void UpdatePaperLengthUI()
    {
        if (paperLengthText != null && paperRoller != null)
        {
            float worldDistance = paperRoller.WorldSpaceDistancePulled;
            float conversionFactor = realWorldMetersPerTile / paperTileLength;
            float totalLengthMeters = worldDistance * conversionFactor;

            paperLengthText.text = $"{totalLengthMeters:F2}";
        }
    }

    void UpdatePaperSpawning()
    {
        if (activePaperTiles.Count == 0) return;
        while (activePaperTiles.Count > 0 && activePaperTiles[0].transform.position.y < paperSpawnPoint.position.y)
        {
            if (activePaperTiles.Count >= maxActiveTiles) break;
            SpawnOneTileAtTop();
        }
    }

    void SpawnOneTileAtTop()
    {
        if (activePaperTiles.Count == 0) return;
        GameObject topTile = activePaperTiles[0];
        Vector3 spawnPos = topTile.transform.position + Vector3.up * paperTileLength;
        GameObject newTile = Instantiate(longPaperPrefab, spawnPos, Quaternion.identity);
        newTile.transform.SetParent(this.transform, true);
        SetLayerRecursively(newTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));
        activePaperTiles.Insert(0, newTile);
    }

    void SpawnInitialPaperTiles()
    {
        ClearAllPaper();
        GameObject firstTile = Instantiate(longPaperPrefab, paperSpawnPoint.position, Quaternion.identity);
        firstTile.transform.SetParent(this.transform, true);
        activePaperTiles.Add(firstTile);
        SetLayerRecursively(firstTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));
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
        while (activePaperTiles.Count > 0)
        {
            int lastIndex = activePaperTiles.Count - 1;
            GameObject tile = activePaperTiles[lastIndex];
            if (tile == null || tile.transform.position.y < cullY)
            {
                if (tile != null) Destroy(tile);
                activePaperTiles.RemoveAt(lastIndex);
            }
            else { break; }
        }
    }

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
        var tilesToDestroy = new List<GameObject>(activePaperTiles);
        foreach (var tile in tilesToDestroy)
        {
            if (tile != null) { if (Application.isPlaying) Destroy(tile); else DestroyImmediate(tile); }
        }
        activePaperTiles.Clear();
    }
}