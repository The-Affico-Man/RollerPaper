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
        //  MilestoneManager.Instance.CollectMilestoneReward(assignedMilestone);
        Vector3 buttonWorldPosition = this.transform.position;

        // OLD, WRONG WAY:
        // MilestoneManager.Instance.CollectMilestoneReward(assignedMilestone, buttonWorldPosition); 

        // NEW, CORRECT WAY: We now pass this world position directly to the new method.
        MilestoneManager.Instance.CollectMilestoneRewardFromWorld(assignedMilestone, buttonWorldPosition); 
        
        uiManager.RefreshMilestoneList();
    }
}