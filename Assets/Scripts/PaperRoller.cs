using UnityEngine;

public class PaperRoller : MonoBehaviour
{
    public SwipeController swipeController; // Drag in inspector
    public float baseSpeed = 0.005f;
    public float speedMultiplier = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
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
    }

    // Debug feature: reset paper position
    public void ResetPosition()
    {
        transform.position = startPos;
    }
}
