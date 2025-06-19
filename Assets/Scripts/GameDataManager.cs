using UnityEngine;

/// <summary>
/// A master manager that controls the game's saving and loading sequence.
/// It ensures that all other managers load and save their data at the correct times.
/// This should be placed on a persistent GameObject in your main scene.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    // References to all the managers that need to save/load data.
    private CurrencyManager currencyManager;
    private MilestoneManager milestoneManager;
    private SkinManager catSkinManager;
    private PaperSkinManager paperSkinManager;
    private PaperRoller paperRoller;
    private ContinuousPaperManager continuousPaperManager;

    // The key fix: The logic is moved to Start() to guarantee all Awake() methods
    // in other scripts have finished running before we try to load anything.
    void Start()
    {
        // Find all the necessary manager instances in the scene.
        currencyManager = FindFirstObjectByType<CurrencyManager>();
        milestoneManager = FindFirstObjectByType<MilestoneManager>();
        catSkinManager = FindFirstObjectByType<SkinManager>();
        paperSkinManager = FindFirstObjectByType<PaperSkinManager>();
        paperRoller = FindFirstObjectByType<PaperRoller>();
        continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();

        // Start the loading process. By running this in Start(), we guarantee
        // all other scripts have completed their Awake() initialization.
        LoadGame();
    }

    /// <summary>
    /// This is called by Unity when the application is about to lose focus,
    /// such as when the player presses the home button or gets a call.
    /// This is the safest place to save data on mobile.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    /// <summary>
    /// This is called by Unity when the application is about to be closed.
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveGame();
    }

    [ContextMenu("Load All Game Data")]
    public void LoadGame()
    {
        Debug.Log("GameDataManager: Loading all game data...");

        // The load order is still important for dependencies.
        currencyManager?.LoadProgress();
        milestoneManager?.LoadProgress();
        catSkinManager?.LoadProgress();
        paperSkinManager?.LoadProgress();
        continuousPaperManager?.LoadProgress();

        // The PaperRoller is last because it depends on the skin managers being ready
        // and it triggers the visual setup of the scene.
        paperRoller?.LoadProgress();

        Debug.Log("GameDataManager: All data loaded.");
    }

    [ContextMenu("Save All Game Data")]
    public void SaveGame()
    {
        Debug.Log("GameDataManager: Saving all game data...");

        // Call the SaveProgress method on each manager.
        currencyManager?.SaveProgress();
        milestoneManager?.SaveProgress();
        catSkinManager?.SaveProgress();
        paperSkinManager?.SaveProgress();
        paperRoller?.SaveProgress();
        continuousPaperManager?.SaveProgress();

        // This forces PlayerPrefs to write to disk immediately.
        PlayerPrefs.Save();

        Debug.Log("GameDataManager: All data saved.");
    }

    [ContextMenu("!!! DELETE ALL SAVE DATA !!!")]
    public void DeleteAllSaveData()
    {
        Debug.LogWarning("GameDataManager: Deleting all player save data!");
        PlayerPrefs.DeleteAll();
    }
}