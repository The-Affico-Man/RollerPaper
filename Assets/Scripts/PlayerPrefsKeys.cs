/// <summary>
/// A static class that holds all the string keys used for PlayerPrefs.
/// This prevents typos and makes managing save data keys much easier.
/// Using 'const' means these values are baked in at compile time and are very fast.
/// </summary>
public static class PlayerPrefsKeys
{
    // --- Player Progress ---
    public const string TotalDistancePulled = "Player_TotalDistancePulled";

    // --- Currency ---
    public const string PlayerCoins = "Player_CurrentCoins";
    public const string LastCoinRewardCheck = "Player_LastCoinRewardCheck"; // For ContinuousPaperManager

    // --- Skins ---
    public const string UnlockedPawSkins = "Player_UnlockedPawSkins";
    public const string EquippedPawSkin = "Player_EquippedPawSkin";
    public const string UnlockedPaperSkins = "Player_UnlockedPaperSkins";
    public const string EquippedPaperSkin = "Player_EquippedPaperSkin";

    // --- Milestones ---
    public const string UnlockedMilestones = "Player_UnlockedMilestones";
    public const string CollectedMilestoneRewards = "Player_CollectedMilestoneRewards";
}