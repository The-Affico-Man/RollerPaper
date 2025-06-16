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

        unlockedMilestones = new HashSet<Milestone>();

        if (allMilestones != null)
        {
            // This robustly filters out any null entries and sorts the list.
            SortedMilestones = allMilestones
                .Where(m => m != null)
                .OrderBy(m => m.distanceInMeters)
                .ToList();
        }
        else
        {
            SortedMilestones = new List<Milestone>();
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

    public void ResetProgress()
    {
        if (unlockedMilestones != null)
        {
            unlockedMilestones.Clear();
        }
    }
}