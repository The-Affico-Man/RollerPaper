using System.Collections;
using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("UI Reference")]
    public TextMeshProUGUI coinUIText;

    [Header("Coin Animation")]
    [Tooltip("Drag your new FlyingCoinsEffect prefab here.")]
    public ParticleSystem coinEffectPrefab;
    [Tooltip("Drag your 'TopLayerCanvas' object from the scene hierarchy here.")]
    public Canvas topLayerCanvas;
    [Tooltip("How many visual coins to show for a large reward.")]
    public int maxVisualCoins = 30;
    [Tooltip("How long the animation takes for a single coin to travel.")]
    public float coinMoveDuration = 1.5f;

    public AudioClip coinSound;
    private AudioSource audioSource;

    // ... other variables and Awake() are unchanged ...
    #region Unchanged Code
    public int CurrentCoins { get; private set; }
    public static event System.Action<int> OnCoinsChanged;
    private void Awake() { if (Instance != null && Instance != this) { Destroy(this.gameObject); } else { Instance = this; } audioSource = gameObject.AddComponent<AudioSource>(); }
    #endregion

    public void AddCoinsWithAnimation(int amount, Vector3 startWorldPosition)
    {
        if (coinEffectPrefab == null || topLayerCanvas == null)
        {
            AddCoins(amount);
            return;
        }

        // 1. Instantiate the effect prefab.
        ParticleSystem effectInstance = Instantiate(coinEffectPrefab);

        // 2. Set the parent to the TopLayerCanvas.
        // The 'false' parameter resets its local transform, which is good practice.
        effectInstance.transform.SetParent(topLayerCanvas.transform, false);

        // 3. Set its world position.
        effectInstance.transform.position = startWorldPosition;

        // 4. Configure the particle count
        var emission = effectInstance.emission;
        float coinsToSpawnRatio = (float)amount / 50f;
        int burstCount = (int)Mathf.Clamp(coinsToSpawnRatio, 5, maxVisualCoins);
        emission.SetBurst(0, new ParticleSystem.Burst(0.0f, burstCount));

        // 5. --- THIS IS THE FINAL FIX ---
        // Configure the force to attract particles towards the coin UI, but ONLY in X and Y.
        var forceModule = effectInstance.forceOverLifetime;
        forceModule.enabled = true;
        Vector3 targetPos = coinUIText.transform.position;

        // We only create curves for X and Y. Z is left as a constant 0 from the prefab.
        forceModule.x = CreateAttractionCurve(startWorldPosition.x, targetPos.x);
        forceModule.y = CreateAttractionCurve(startWorldPosition.y, targetPos.y);

        // 6. Play sound and add coins
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