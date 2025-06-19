using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// A self-contained component that lives on the shop item button.
/// It knows about its own skin and how to talk to the ShopManager.
/// </summary>
public class ShopItemButton : MonoBehaviour, IPointerDownHandler
{
    // These will be set by the ShopManager when this item is created.
    private CatSkin catSkinToUnlock;
    private PaperSkin paperSkinToUnlock;
    private ShopManager shopManager;

    private Vector2 pointerDownPosition;

    /// <summary>
    /// Initializes this button with the data it needs to function.
    /// </summary>
    public void Setup(CatSkin skin, ShopManager manager)
    {
        catSkinToUnlock = skin;
        shopManager = manager;
    }

    public void Setup(PaperSkin skin, ShopManager manager)
    {
        paperSkinToUnlock = skin;
        shopManager = manager;
    }

    // This interface method captures the position when the pointer goes down.
    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
    }

    /// <summary>
    /// This is the public method that the Button's OnClick() event will call.
    /// </summary>
    public void OnItemClicked()
    {
        if (shopManager == null) return;

        // Tell the shop manager to try the unlock, passing along the correct
        // skin AND the position where the click happened.
        if (catSkinToUnlock != null)
        {
            shopManager.HandleUnlockAttempt(catSkinToUnlock, pointerDownPosition);
        }
        else if (paperSkinToUnlock != null)
        {
            shopManager.HandleUnlockAttempt(paperSkinToUnlock, pointerDownPosition);
        }
    }
}