using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

[System.Serializable]
public class RewardTier
{
    [Tooltip("This reward applies UP TO this distance in meters.")]
    public float distanceThreshold;
    [Tooltip("The number of coins to award per 'metersPerCoin' within this tier.")]
    public int coinAmount;
}

public class ContinuousPaperManager : MonoBehaviour
{
    // All your variables are correct and unchanged
    #region Unchanged Variables
    [Header("UI")]
    public TextMeshProUGUI paperLengthText;

    private PaperRoller paperRoller;

    [Header("Scoring")]
    public float realWorldMetersPerTile = 0.4f;

    [Header("Paper Settings")]
    public GameObject longPaperPrefab;
    public Transform paperSpawnPoint;
    public float paperTileLength = 1f;

    [Header("Visibility Control")]
    public Transform toiletPaperRoll; // This MUST be assigned in the Inspector
    public LayerMask paperLayer = 1;

    [Header("Culling Settings")]
    public float cullDistance = 5f;
    public Camera playerCamera;
    public int maxActiveTiles = 15;

    [Header("Tiered Coin Generation")]
    public float metersPerCoinInterval = 10f;
    public List<RewardTier> rewardTiers;

    private float lastMeterCheck = 0f;
    private List<GameObject> activePaperTiles = new List<GameObject>();
    #endregion

    // --- THE KEY FIX: Use Awake() for finding references ---
    // Awake() is guaranteed to run before any Start() methods in the scene.
    void Awake()
    {
        // Find the PaperRoller reference here so it's ready for any script that needs it.
        paperRoller = FindFirstObjectByType<PaperRoller>();

        // We can also safely set up the camera and reward tiers here.
        playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        if (rewardTiers != null)
        {
            rewardTiers = rewardTiers.OrderBy(tier => tier.distanceThreshold).ToList();
        }
    }

    // The Start() method is no longer needed and can be deleted.

    public void InitializePaperAtRollerPosition()
    {
        // Add safety checks here. Because Awake() has run, paperRoller should not be null.
        if (toiletPaperRoll == null || paperSpawnPoint == null || paperRoller == null)
        {
            Debug.LogError("ContinuousPaperManager: A critical reference is not set! Cannot spawn paper.", this.gameObject);
            return;
        }
        SpawnInitialPaperTiles();
    }

    void SpawnInitialPaperTiles()
    {
        ClearAllPaper();

        Vector3 spawnPosition;
        if (paperRoller.WorldSpaceDistancePulled < 0.01f)
        {
            spawnPosition = paperSpawnPoint.position;
        }
        else
        {
            spawnPosition = toiletPaperRoll.position;
        }

        GameObject firstTile = Instantiate(longPaperPrefab, spawnPosition, Quaternion.identity);
        firstTile.transform.SetParent(this.transform, true);
        activePaperTiles.Add(firstTile);

        PaperTile firstTileComponent = firstTile.GetComponent<PaperTile>();
        if (firstTileComponent != null && PaperSkinManager.Instance != null && PaperSkinManager.Instance.CurrentSkin != null)
        {
            firstTileComponent.SetSkin(PaperSkinManager.Instance.CurrentSkin.tileMaterial);
        }
        SetLayerRecursively(firstTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));
        for (int i = 0; i < 3; i++)
        {
            if (activePaperTiles.Count >= maxActiveTiles) break;
            SpawnOneTileAtTop();
        }
    }

    // All other methods are completely unchanged and correct
    #region Unchanged Methods
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
            if (totalLengthMeters - lastMeterCheck >= metersPerCoinInterval)
            {
                int coinReward = 0;
                foreach (RewardTier tier in rewardTiers)
                {
                    if (lastMeterCheck < tier.distanceThreshold)
                    {
                        coinReward = tier.coinAmount;
                        break;
                    }
                }
                if (coinReward == 0 && rewardTiers.Count > 0)
                {
                    coinReward = rewardTiers.Last().coinAmount;
                }

                if (coinReward > 0)
                {
                    CurrencyManager.Instance.AddCoins(coinReward);
                }
                lastMeterCheck += metersPerCoinInterval;
            }
        }
    }
    void UpdatePaperSpawning()
    {
        if (activePaperTiles.Count == 0 || paperSpawnPoint == null) return;
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
        PaperTile tileComponent = newTile.GetComponent<PaperTile>();
        if (tileComponent != null && PaperSkinManager.Instance != null && PaperSkinManager.Instance.CurrentSkin != null)
        {
            tileComponent.SetSkin(PaperSkinManager.Instance.CurrentSkin.tileMaterial);
        }
        SetLayerRecursively(newTile, Mathf.RoundToInt(Mathf.Log(paperLayer.value, 2)));
        activePaperTiles.Insert(0, newTile);
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
    public void SaveProgress()
    {
        PlayerPrefs.SetFloat(PlayerPrefsKeys.LastCoinRewardCheck, lastMeterCheck);
    }

    public void LoadProgress()
    {
        lastMeterCheck = PlayerPrefs.GetFloat(PlayerPrefsKeys.LastCoinRewardCheck, 0f);
    }
    #endregion
}