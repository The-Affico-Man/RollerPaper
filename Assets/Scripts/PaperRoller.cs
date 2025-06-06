using UnityEngine;

public class PaperRoller : MonoBehaviour
{
    private SwipeController swipeController;
    private float totalSwipeDistance = 0f; // Accumulated swipe distance in Unity units
    public float TotalSwipeDistance => totalSwipeDistance;

    public float baseSpeed = 0.005f;
    public float speedMultiplier = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        swipeController = FindFirstObjectByType<SwipeController>();
    }

    void Update()
    {
        // --- THIS IS THE FIX ---
        // First, check if there are any active touches on the screen.
        // If the count is 0, we do nothing, and the paper stops instantly.
        if (swipeController.GetActivePawCount() > 0)
        {
            Vector2 swipe = swipeController.SwipeDelta;

            // Now, we only process the swipe if a finger is actually down.
            // Only use downward swipes.
            if (swipe.y < 0)
            {
                float movement = Mathf.Abs(swipe.y) * baseSpeed * speedMultiplier;
                transform.position += Vector3.down * movement;
            }
        }
        // If GetActivePawCount() is 0, this whole block is skipped, and no movement occurs.


        // The original logic for calculating total distance can remain.
        // It relies on SwipeDelta, which is correctly reset to zero in SwipeController.
        if (swipeController.SwipeDelta.y < 0f) // Only down swipes
        {
            // Note: This logic for totalSwipeDistance is slightly different from the one in
            // SwipeController. You might want to consolidate this later to have a single
            // source of truth, but for now, we'll leave it as is.
            float swipeAmount = -swipeController.SwipeDelta.y; // Make positive
            totalSwipeDistance += swipeAmount * baseSpeed * speedMultiplier;
        }
    }

    // Debug feature: reset paper position
    public void ResetPosition()
    {
        transform.position = startPos;
    }
}