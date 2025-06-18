using UnityEngine;

/// <summary>
/// This is a ScriptableObject, a data container that lives in your Project files.
/// It holds all the information for a single cat skin.
/// </summary>
/// 
public enum UnlockType { ByCoins, ByMilestone, ByTotalDistance, Premium }

[CreateAssetMenu(fileName = "NewCatSkin", menuName = "Cat Skins/Create New Skin")]
public class CatSkin : ScriptableObject
{
    [Header("Shop Information")]
    [Tooltip("How is this skin unlocked?")]
    public UnlockType unlockType;

    [Tooltip("The price in coins if unlockType is 'ByCoins'.")]
    public int priceInCoins;

    [Tooltip("The milestone required to unlock this skin if unlockType is 'ByMilestone'.")]
    public Milestone requiredMilestone;

    [Tooltip("The name of this skin (e.g., 'Calico', 'Tuxedo').")]
    public string skinName = "Default";

    [Tooltip("The sprite for the cat paw for this skin.")]
    public Sprite pawSprite;

    [Tooltip("The total paper length in meters required to unlock this skin if unlockType is 'ByTotalDistance'.")]
    public float requiredTotalDistance;
    // You can add more things here later!
    // For example: public AudioClip meowSound;
}