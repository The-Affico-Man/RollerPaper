using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A Singleton manager that sits on the main canvas and tracks the last known
/// screen position of any pointer down event that occurs on the UI.
/// </summary>
public class InputPositionTracker : MonoBehaviour, IPointerDownHandler
{
    public static InputPositionTracker Instance { get; private set; }

    public Vector2 LastPointerDownPosition { get; private set; }

    private void Awake()
    {
        // Standard Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// This method is called by the EventSystem whenever a pointer down event
    /// occurs on this GameObject or any of its children.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // Store the screen position of the click or touch.
        LastPointerDownPosition = eventData.position;
    }
}