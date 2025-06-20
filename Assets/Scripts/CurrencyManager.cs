using System.Collections;
using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("UI Reference")]
    public TextMeshProUGUI coinUIText;

    [Header("Coin Animation")]
    [Tooltip("Drag your 'TopLayerCanvas' object from the scene hierarchy here.")]
    public Canvas topLayerCanvas;
    [Tooltip("How many visual coins to show for a large reward.")]
    public int maxVisualCoins = 30;
    [Tooltip("How long the animation takes for a single coin to travel.")]
    public float coinMoveDuration = 1.5f;

    public AudioClip coinSound;
    private AudioSource audioSource;

    public int CurrentCoins { get; private set; }
    public static event System.Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void AddCoinsWithAnimation(int amount, Vector3 startWorldPosition)
    {
        if (topLayerCanvas == null)
        {
            AddCoins(amount);
            return;
        }

        // 1. Spawn the effect from the pool. The pooler places it at the correct world position.
        GameObject effectGO = ParticlePooler.Instance?.SpawnFromPool("FlyingCoins", startWorldPosition, Quaternion.identity);

        if (effectGO != null)
        {
            ParticleSystem effectInstance = effectGO.GetComponent<ParticleSystem>();

            // --- THIS IS THE DEFINITIVE FIX ---

            // 2. Set its parent to the TopLayerCanvas, with 'worldPositionStays' set to 'false'.
            // This is CRITICAL. It resets the particle system's local scale to (1,1,1)
            // and local position to (0,0,0), fixing the gigantic scale bug.
            effectInstance.transform.SetParent(topLayerCanvas.transform, false);

            // 3. NOW that it is properly parented and scaled, set its world position again.
            // This correctly places the clean, newly-parented object at the desired start point.
            effectInstance.transform.position = startWorldPosition;

            // --- END OF FIX ---

            // 4. Configure the rest of the effect as before. This logic is correct.
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

        // Play sound and add coins
        if (audioSource != null && coinSound != null)
        {
            audioSource.PlayOneShot(coinSound);
        }
        AddCoins(amount);
    }

    // The rest of the script is correct and unchanged
    #region Unchanged Methods
    private ParticleSystem.MinMaxCurve CreateAttractionCurve(float start, float end) { float distance = end - start; var curve = new AnimationCurve(); curve.AddKey(0.0f, 0.0f); curve.AddKey(1.0f, distance * 2.0f); return new ParticleSystem.MinMaxCurve(1.0f, curve); }
    public void AddCoins(int amount) { if (amount <= 0) return; CurrentCoins += amount; UpdateUI(); }
    public bool TrySpendCoins(int amount) { if (amount <= 0) return false; if (CurrentCoins >= amount) { CurrentCoins -= amount; UpdateUI(); return true; } return false; }
    private void UpdateUI() { if (coinUIText != null) { coinUIText.text = CurrentCoins.ToString(); } OnCoinsChanged?.Invoke(CurrentCoins); }
    public void SaveProgress() { PlayerPrefs.SetInt(PlayerPrefsKeys.PlayerCoins, CurrentCoins); }
    public void LoadProgress() { CurrentCoins = PlayerPrefs.GetInt(PlayerPrefsKeys.PlayerCoins, 0); UpdateUI(); }
    #endregion
}