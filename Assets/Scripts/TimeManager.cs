using System;
using UnityEngine;

/// <summary>
/// A persistent Singleton that provides the current time.
/// In this development version, it uses the local device's UTC time.
/// This will be replaced with a secure Firebase server timestamp later.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    // No more web requests needed.

    private void Awake()
    {
        // Standard Singleton pattern that persists across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Gets the current UTC time from the local device.
    /// </summary>
    public DateTime GetCurrentTime()
    {
        // Simply return the device's current universal time.
        return DateTime.UtcNow;
    }

    /// <summary>
    /// For development, we will always assume we have a "secure" time
    /// to allow the ChallengeManager's reset logic to run.
    /// </summary>
    public bool HasSecureTime()
    {
        return true;
    }
}