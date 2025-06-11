using UnityEngine;

/// <summary>
/// This is a ScriptableObject, a data container that lives in your Project files.
/// It holds all the information for a single cat skin.
/// </summary>
[CreateAssetMenu(fileName = "NewCatSkin", menuName = "Cat Skins/Create New Skin")]
public class CatSkin : ScriptableObject
{
    [Tooltip("The name of this skin (e.g., 'Calico', 'Tuxedo').")]
    public string skinName = "Default";

    [Tooltip("The sprite for the cat paw for this skin.")]
    public Sprite pawSprite;

    // You can add more things here later!
    // For example: public AudioClip meowSound;
}