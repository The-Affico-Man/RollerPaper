using UnityEngine;

/// <summary>
/// A ScriptableObject that holds all the data for a single progression milestone.
/// </summary>
[CreateAssetMenu(fileName = "NewMilestone", menuName = "Milestones/Create New Milestone")]
public class Milestone : ScriptableObject
{
    [Tooltip("The name of the milestone (e.g., 'Eiffel Tower', 'Moon').")]
    public string milestoneName;

    [Tooltip("The distance in meters required to unlock this milestone.")]
    public float distanceInMeters;

    [Tooltip("The image/icon that represents this milestone in the UI.")]
    public Sprite milestoneIcon;

    // We can add rewards later, like:
    // public int currencyReward;
    // public CatSkin unlockedCatSkin;
}