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
        Vector2 swipe = swipeController.SwipeDelta;

        // Only use downward swipes
        if (swipe.y < 0)
        {
            float movement = Mathf.Abs(swipe.y) * baseSpeed * speedMultiplier;
            transform.position += Vector3.down * movement;
        }
        if (swipeController.SwipeDelta.y < 0f) // Only down swipes
        {
            float swipeAmount = -swipeController.SwipeDelta.y * Time.deltaTime; // Make positive
            totalSwipeDistance += swipeAmount;
        }
    }

    // Debug feature: reset paper position
    public void ResetPosition()
    {
        transform.position = startPos;
    }
}
