using UnityEngine;

/// <summary>
/// A very simple Singleton that tracks if a major UI panel is open.
/// Other scripts can query this manager to know if gameplay input should be blocked.
/// </summary>
public class UIStateManager : MonoBehaviour
{
    // A "Singleton" instance allows any script to access it easily
    // with UIStateManager.Instance
    public static UIStateManager Instance { get; private set; }

    /// <summary>
    /// This property is true if a menu is open that should block gameplay.
    /// It's public so other scripts can read it, but can only be set by this script.
    /// </summary>
    public bool IsUIBlockingGameplay { get; private set; }

    private void Awake()
    {
        // Standard Singleton pattern: Ensure only one instance of this manager exists.
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Destroy(this.gameObject);
        }
        else
        {
            // Otherwise, set this as the one and only instance.
            Instance = this;
            // Optional: Use this if you want the manager to persist between scenes.
            // DontDestroyOnLoad(this.gameObject);
        }

        // Ensure that when the game starts, gameplay is NOT blocked.
        IsUIBlockingGameplay = false;
    }

    /// <summary>
    /// Call this method when opening or closing a full-screen panel 
    /// like the milestone or settings screen.
    /// </summary>
    /// <param name="isBlocking">True if the UI is now open, false if it is closing.</param>
    public void SetUIBlockingState(bool isBlocking)
    {
        IsUIBlockingGameplay = isBlocking;
        // This debug log is helpful for testing to see the state change.
        Debug.Log("UI Blocking Gameplay set to: " + isBlocking);
    }
}