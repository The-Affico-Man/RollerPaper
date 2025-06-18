using UnityEngine;

/// <summary>
/// A ScriptableObject data container for a single paper skin.
/// It holds the material for the 3D roll and the material for the flat paper tiles.
/// </summary>
/// 

[CreateAssetMenu(fileName = "NewPaperSkin", menuName = "Paper Skins/Create New Paper Skin")]
public class PaperSkin : ScriptableObject
{

    [Header("Shop Information")]
    [Tooltip("How is this skin unlocked?")]
    public UnlockType unlockType;

    [Tooltip("The price in coins if unlockType is 'ByCoins'.")]
    public int priceInCoins;

    [Tooltip("The milestone required to unlock this skin if unlockType is 'ByMilestone'.")]
    public Milestone requiredMilestone;

    [Tooltip("The total paper length in meters required to unlock this skin if unlockType is 'ByTotalDistance'.")]
    public float requiredTotalDistance;

    [Tooltip("A small icon or thumbnail to represent this skin in the shop UI.")]
    public Sprite thumbnail;

    [Tooltip("The name of this skin (e.g., 'Money', 'Flowers').")]
    public string skinName;

    [Tooltip("The material to apply to the 3D spinning toilet paper roll model.")]
    public Material rollMaterial;

    [Tooltip("The material to apply to the flat paper tile prefabs as they are spawned.")]
    public Material tileMaterial;
}