using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MilestoneManager : MonoBehaviour
{
    [Header("Data")]
    public List<Milestone> allMilestones;

    public List<Milestone> SortedMilestones { get; private set; }
    public static MilestoneManager Instance { get; private set; }

    private HashSet<Milestone> unlockedMilestones;
    private HashSet<Milestone> collectedRewards; // NEW: Tracks collected rewards

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }

        unlockedMilestones = new HashSet<Milestone>();
        collectedRewards = new HashSet<Milestone>(); // NEW: Initialize the set

        if (allMilestones != null)
        {
            SortedMilestones = allMilestones.Where(m => m != null).OrderBy(m => m.distanceInMeters).ToList();
        }
    }

    // This method NO LONGER gives coins. It only unlocks the milestone.
    public void UnlockMilestone(Milestone milestone)
    {
        if (milestone != null && unlockedMilestones.Add(milestone))
        {
            Debug.Log($"Milestone Unlocked: {milestone.milestoneName}");
        }
    }

    // --- START OF NEW METHODS ---

    /// <summary>
    /// Checks if a milestone's reward has been collected.
    /// </summary>
    public bool HasRewardBeenCollected(Milestone milestone)
    {
        return collectedRewards.Contains(milestone);
    }

    /// <summary>
    /// Called when the player clicks "Collect". Gives coins and marks as collected.
    /// </summary>
    public void CollectMilestoneReward(Milestone milestone)
    {
        if (milestone == null || !IsMilestoneUnlocked(milestone) || HasRewardBeenCollected(milestone)) return;

        if (milestone.coinReward > 0)
        {
            CurrencyManager.Instance.AddCoins(milestone.coinReward);
        }
        collectedRewards.Add(milestone);
    }

    /// <summary>
    /// Checks if ANY unlocked milestone has a reward waiting to be collected.
    /// Used to show the red dot notification.
    /// </summary>
    public bool AreThereUncollectedRewards()
    {
        foreach (Milestone milestone in unlockedMilestones)
        {
            // If we find even one that is unlocked but not collected, return true.
            if (!HasRewardBeenCollected(milestone))
            {
                return true;
            }
        }
        return false;
    }
    // --- END OF NEW METHODS ---

    public bool IsMilestoneUnlocked(Milestone milestone) { return unlockedMilestones.Contains(milestone); }
    public void ResetProgress() { if (unlockedMilestones != null) unlockedMilestones.Clear(); if (collectedRewards != null) collectedRewards.Clear(); }
}