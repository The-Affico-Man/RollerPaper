using System.Collections.Generic;
using UnityEngine;

public class ParticlePooler : MonoBehaviour
{
    // A "Singleton" instance so any script can access it easily
    public static ParticlePooler Instance { get; private set; }

    // This is a nested class that defines what a "pool" is in the Inspector
    [System.Serializable]
    public class Pool
    {
        [Tooltip("A unique name to identify this pool (e.g., 'Confetti', 'FlyingCoins').")]
        public string tag;
        [Tooltip("The particle system prefab for this pool.")]
        public GameObject prefab;
        [Tooltip("The initial number of objects to create in this pool.")]
        public int size;
    }

    [Header("Pool Definitions")]
    [Tooltip("Define all the different particle pools you need here.")]
    public List<Pool> pools;

    // The master dictionary that holds all the pools, identified by their tag
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // Standard Singleton pattern
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // Loop through all the pool definitions you created in the Inspector
        foreach (Pool pool in pools)
        {
            // For each one, create a new queue to hold the objects
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Instantiate the objects for the pool
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.transform.SetParent(this.transform); // Keep the hierarchy clean
                obj.SetActive(false); // Deactivate it immediately
                objectPool.Enqueue(obj); // Add it to the queue
            }

            // Add the newly created queue to our master dictionary with its tag
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Spawns an object from the specified pool.
    /// </summary>
    /// <param name="tag">The tag of the pool to use.</param>
    /// <param name="position">The world position to spawn the object at.</param>
    /// <param name="rotation">The rotation to spawn the object with.</param>
    /// <returns>The spawned GameObject, which you can then configure.</returns>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        // Check if a pool with the requested tag exists
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
            return null;
        }

        // Get the object from the front of the queue
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // Activate it, position it, and rotate it
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // CRITICAL: Re-add the object to the back of the queue.
        // This means it's "in use" but will be available again later.
        // We rely on the particle system's "Stop Action: Disable" to make it reusable.
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}