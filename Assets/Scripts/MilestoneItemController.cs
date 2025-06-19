using UnityEngine;
using UnityEngine.UI;

public class MilestoneItemController : MonoBehaviour
{
    private Milestone assignedMilestone;
    private MilestoneUIManager uiManager;

    /// <summary>
    /// The MilestoneUIManager calls this to give this item its identity.
    /// </summary>
    public void Setup(Milestone milestone, MilestoneUIManager manager)
    {
        assignedMilestone = milestone;
        uiManager = manager;
    }

    /// <summary>
    /// This is the public method that the "Collect" button will call.
    /// </summary>
    public void OnCollectButtonPressed()
    {
        if (assignedMilestone == null) return;

        // Tell the main manager to collect the reward for our assigned milestone.
        MilestoneManager.Instance.CollectMilestoneReward(assignedMilestone);

        // Tell the UI manager to rebuild the list to reflect the change.
        uiManager.RefreshMilestoneList();
    }
}