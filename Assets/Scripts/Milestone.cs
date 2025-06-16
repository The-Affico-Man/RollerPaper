using UnityEngine;

// We define the choice list outside the class so other scripts can easily access it.
public enum MilestoneType { Length, Height }

/// <summary>
/// A ScriptableObject that holds all the data for a single progression milestone.
/// </summary>
[CreateAssetMenu(fileName = "NewMilestone", menuName = "Milestones/Create New Milestone")]
public class Milestone : ScriptableObject
{
    [Tooltip("The name of the milestone (e.g., 'a Blue Whale', 'the Eiffel Tower').")]
    public string milestoneName;

    [Tooltip("The distance in meters required to unlock this milestone.")]
    public float distanceInMeters;

    [Tooltip("The image/icon that represents this milestone in the UI.")]
    public Sprite milestoneIcon;

    // --- THIS IS THE NEW PART ---
    [Tooltip("Is this milestone about length (like a whale) or height (like a tower)?")]
    public MilestoneType measurementType = MilestoneType.Length;
    // ----------------------------
}