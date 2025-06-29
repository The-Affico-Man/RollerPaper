using UnityEngine;
using UnityEngine.UI;

public class ChallengeItemController : MonoBehaviour
{
    private ChallengeState currentState;
    private ChallengeUIManager uiManager;

    // This method is called by the ChallengeUIManager to give this item its data.
    public void Setup(ChallengeState state, ChallengeUIManager manager)
    {
        currentState = state;
        uiManager = manager;
    }

    // This is the public method that the "Claim" button's OnClick() event will call.
    public void OnClaimButtonPressed()
    {
        if (currentState == null || uiManager == null) return;

        // Get the world position of this specific button.
        Vector3 buttonWorldPosition = this.transform.position;

        // Tell the manager to claim the reward, passing both the challenge and the position.
        ChallengeManager.Instance.ClaimReward(currentState.challenge, buttonWorldPosition);

        // Crucially, tell the main UI manager to refresh itself to show the "Claimed" state.
        uiManager.RefreshUI();
    }
}