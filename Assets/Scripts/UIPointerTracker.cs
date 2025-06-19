using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A simple component that sits on a UI element and tracks the last known position
/// where a pointer (mouse or touch) went down on it.
/// </summary>
public class UIPointerTracker : MonoBehaviour, IPointerDownHandler
{
    // A static variable means there's only one copy shared across all instances.
    // This is a simple way to track the most recent pointer position.
    public static Vector2 LastPointerDownPosition { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        // When a pointer goes down on this UI element, store its screen position.
        LastPointerDownPosition = eventData.position;
    }
}