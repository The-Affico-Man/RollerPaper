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
    private HashSet<Milestone> collectedRewards;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }

        unlockedMilestones = new HashSet<Milestone>();
        collectedRewards = new HashSet<Milestone>();

        if (allMilestones != null)
        {
            SortedMilestones = allMilestones.Where(m => m != null).OrderBy(m => m.distanceInMeters).ToList();
        }
    }

    public void UnlockMilestone(Milestone milestone)
    {
        if (milestone != null && unlockedMilestones.Add(milestone))
        {
            Debug.Log($"Milestone Unlocked: {milestone.milestoneName}");
        }
    }
   /*public void CollectMilestoneReward(Milestone milestone, Vector3 startPosition)
    {
        if (milestone == null || !IsMilestoneUnlocked(milestone) || HasRewardBeenCollected(milestone)) return;

        if (milestone.coinReward > 0)
        {
            // Pass the startPosition on to the currency manager's animation.
            CurrencyManager.Instance.AddCoinsWithAnimation(milestone.coinReward, startPosition);
        }
        collectedRewards.Add(milestone);
    }*/
    public void CollectMilestoneRewardFromWorld(Milestone milestone, Vector3 startWorldPosition)
    {
        if (milestone == null || !IsMilestoneUnlocked(milestone) || HasRewardBeenCollected(milestone)) return;

        if (milestone.coinReward > 0)
        {
            // Call the new, specific method for world positions
            CurrencyManager.Instance.AddCoinsFromWorldPosition(milestone.coinReward, startWorldPosition);
        }
        collectedRewards.Add(milestone);
    }
    public bool HasRewardBeenCollected(Milestone milestone)
    {
        return collectedRewards.Contains(milestone);
    }

    /*public void CollectMilestoneReward(Milestone milestone)
    {
        if (milestone == null || !IsMilestoneUnlocked(milestone) || HasRewardBeenCollected(milestone)) return;
        if (milestone.coinReward > 0)
        {
            Vector3 buttonPosition = InputPositionTracker.Instance.LastPointerDownPosition;
            CurrencyManager.Instance.AddCoinsWithAnimation(milestone.coinReward, buttonPosition);
        }
        collectedRewards.Add(milestone);
    }*/

    public bool AreThereUncollectedRewards()
    {
        return unlockedMilestones.Any(unlocked => !HasRewardBeenCollected(unlocked));
    }

    public bool IsMilestoneUnlocked(Milestone milestone)
    {
        return unlockedMilestones.Contains(milestone);
    }

    // --- NEW SAVE/LOAD METHODS ---

    public void SaveProgress()
    {
        string unlockedString = string.Join("|", unlockedMilestones.Select(m => m.milestoneName));
        PlayerPrefs.SetString(PlayerPrefsKeys.UnlockedMilestones, unlockedString);

        string collectedString = string.Join("|", collectedRewards.Select(m => m.milestoneName));
        PlayerPrefs.SetString(PlayerPrefsKeys.CollectedMilestoneRewards, collectedString);
    }

    public void LoadProgress()
    {
        // This is a safety check. If for some reason the master list of milestones
        // isn't assigned in the inspector, we exit early to prevent any errors.
        if (allMilestones == null)
        {
            Debug.LogError("MilestoneManager: The 'allMilestones' list has not been assigned in the Inspector!", this.gameObject);
            return;
        }

        // --- Load Unlocked Milestones ---
        unlockedMilestones.Clear();
        string unlockedString = PlayerPrefs.GetString(PlayerPrefsKeys.UnlockedMilestones);

        // This is the critical fix: Only process the string if it actually has content.
        if (!string.IsNullOrEmpty(unlockedString))
        {
            List<string> unlockedNames = new List<string>(unlockedString.Split('|'));
            foreach (Milestone m in allMilestones)
            {
                // Ensure the milestone from our master list is not null before checking its name.
                if (m != null && unlockedNames.Contains(m.milestoneName))
                {
                    unlockedMilestones.Add(m);
                }
            }
        }

        // --- Load Collected Rewards ---
        collectedRewards.Clear();
        string collectedString = PlayerPrefs.GetString(PlayerPrefsKeys.CollectedMilestoneRewards);

        // Apply the same critical fix here.
        if (!string.IsNullOrEmpty(collectedString))
        {
            List<string> collectedNames = new List<string>(collectedString.Split('|'));
            foreach (Milestone m in allMilestones)
            {
                if (m != null && collectedNames.Contains(m.milestoneName))
                {
                    collectedRewards.Add(m);
                }
            }
        }
    }
}