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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        // Initialize the set, but do not reset progress here.
        unlockedMilestones = new HashSet<Milestone>();

        if (allMilestones != null)
        {
            SortedMilestones = allMilestones.OrderBy(m => m.distanceInMeters).ToList();
        }
    }

    public bool IsMilestoneUnlocked(Milestone milestone)
    {
        return unlockedMilestones.Contains(milestone);
    }

    public void UnlockMilestone(Milestone milestone)
    {
        if (milestone != null)
        {
            unlockedMilestones.Add(milestone);
        }
    }

    /// <summary>
    /// A public method that other managers can call to reset progress for a new game.
    /// </summary>
    public void ResetProgress()
    {
        unlockedMilestones.Clear();
        Debug.Log("Milestone progress reset for this session.");
    }
}