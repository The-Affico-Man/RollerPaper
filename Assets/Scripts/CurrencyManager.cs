using System.Collections;
using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    // ... Variables and Awake() are unchanged ...
    #region Unchanged Code
    public static CurrencyManager Instance { get; private set; }
    [Header("UI Reference")]
    public TextMeshProUGUI coinUIText;
    [Header("Coin Animation")]
    public Canvas topLayerCanvas;
    public int maxVisualCoins = 30;
    public float coinMoveDuration = 1.5f;
    public AudioClip coinSound;
    private AudioSource audioSource;
    private Camera mainCamera;
    public int CurrentCoins { get; private set; }
    public static event System.Action<int> OnCoinsChanged;
    private void Awake() { if (Instance != null && Instance != this) { Destroy(this.gameObject); } else { Instance = this; } audioSource = gameObject.AddComponent<AudioSource>(); mainCamera = Camera.main; }
    #endregion

    // --- NEW PUBLIC METHOD #1 (For Buttons) ---
    public void AddCoinsFromWorldPosition(int amount, Vector3 startWorldPosition)
    {
        // This method directly uses the provided world position.
        StartCoroutine(AnimateCoins(amount, startWorldPosition));
    }

    // --- NEW PUBLIC METHOD #2 (For Score Text) ---
    public void AddCoinsFromScreenPosition(int amount, Vector2 startScreenPosition)
    {
        if (mainCamera == null || topLayerCanvas == null) { AddCoins(amount); return; }

        // This method converts the screen position to a world position before animating.
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(startScreenPosition.x, startScreenPosition.y, topLayerCanvas.planeDistance));
        StartCoroutine(AnimateCoins(amount, worldPos));
    }

    // --- The Core Animation Logic (Now private) ---
    public IEnumerator AnimateCoins(int amount, Vector3 startWorldPosition)
    {
        if (topLayerCanvas == null) { AddCoins(amount); yield break; }

        GameObject effectGO = ParticlePooler.Instance?.SpawnFromPool("FlyingCoins", startWorldPosition, Quaternion.identity);

        if (effectGO != null)
        {
            ParticleSystem effectInstance = effectGO.GetComponent<ParticleSystem>();
            effectInstance.transform.SetParent(topLayerCanvas.transform, false);
            effectInstance.transform.position = startWorldPosition;

            var emission = effectInstance.emission;
            float coinsToSpawnRatio = (float)amount / 50f;
            int burstCount = (int)Mathf.Clamp(coinsToSpawnRatio, 5, maxVisualCoins);
            emission.SetBurst(0, new ParticleSystem.Burst(0.0f, burstCount));

            var forceModule = effectInstance.forceOverLifetime;
            forceModule.enabled = true;
            Vector3 targetPos = coinUIText.transform.position;
            forceModule.x = CreateAttractionCurve(startWorldPosition.x, targetPos.x);
            forceModule.y = CreateAttractionCurve(startWorldPosition.y, targetPos.y);
            forceModule.z = CreateAttractionCurve(startWorldPosition.z, targetPos.z);
        }

        if (audioSource != null && coinSound != null)
        {
            audioSource.PlayOneShot(coinSound);
        }
        AddCoins(amount);
    }

    // --- The rest of the script is unchanged and correct ---
    #region Unchanged Methods
    private ParticleSystem.MinMaxCurve CreateAttractionCurve(float start, float end) { float distance = end - start; var curve = new AnimationCurve(); curve.AddKey(0.0f, 0.0f); curve.AddKey(1.0f, distance * 2.0f); return new ParticleSystem.MinMaxCurve(1.0f, curve); }
    public void AddCoins(int amount) { if (amount <= 0) return; CurrentCoins += amount; UpdateUI(); }
    public bool TrySpendCoins(int amount) { if (amount <= 0) return false; if (CurrentCoins >= amount) { CurrentCoins -= amount; UpdateUI(); return true; } return false; }
    private void UpdateUI() { if (coinUIText != null) { coinUIText.text = CurrentCoins.ToString(); } OnCoinsChanged?.Invoke(CurrentCoins); }
    public void SaveProgress() { PlayerPrefs.SetInt(PlayerPrefsKeys.PlayerCoins, CurrentCoins); }
    public void LoadProgress() { CurrentCoins = PlayerPrefs.GetInt(PlayerPrefsKeys.PlayerCoins, 0); UpdateUI(); }
    #endregion
}