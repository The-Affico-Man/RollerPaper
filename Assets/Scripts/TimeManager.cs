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
    /// <summary>
    /// Calculates the time remaining until the next daily reset (00:00 UTC).
    /// </summary>
    public TimeSpan GetTimeUntilNextDailyReset()
    {
        DateTime now = GetCurrentTime();
        // Get tomorrow's date at midnight.
        DateTime nextResetTime = now.Date.AddDays(1);
        return nextResetTime - now;
    }

    /// <summary>
    /// Calculates the time remaining until the next weekly reset (e.g., Sunday at 00:00 UTC).
    /// </summary>
    public TimeSpan GetTimeUntilNextWeeklyReset()
    {
        DateTime now = GetCurrentTime();
        // DayOfWeek in C# starts with Sunday = 0, Monday = 1, etc.
        // Let's say our week resets on Sunday.
        int daysUntilSunday = (7 - (int)now.DayOfWeek) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7; // If today is Sunday, the next reset is 7 days away.

        DateTime nextResetTime = now.Date.AddDays(daysUntilSunday);
        return nextResetTime - now;
    }
}