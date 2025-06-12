using UnityEngine;

/// <summary>
/// A ScriptableObject data container for a single paper skin.
/// It holds the material for the 3D roll and the material for the flat paper tiles.
/// </summary>
[CreateAssetMenu(fileName = "NewPaperSkin", menuName = "Paper Skins/Create New Paper Skin")]
public class PaperSkin : ScriptableObject
{
    [Tooltip("The name of this skin (e.g., 'Money', 'Flowers').")]
    public string skinName;

    [Tooltip("The material to apply to the 3D spinning toilet paper roll model.")]
    public Material rollMaterial;

    [Tooltip("The material to apply to the flat paper tile prefabs as they are spawned.")]
    public Material tileMaterial;
}