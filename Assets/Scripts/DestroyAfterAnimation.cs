using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    // This function can be called by an Animation Event.
    public void DestroyGameObject()
    {
        Destroy(gameObject);
    }
}