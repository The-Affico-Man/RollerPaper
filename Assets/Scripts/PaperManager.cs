using System.Collections.Generic;
using UnityEngine;

public class PaperManager : MonoBehaviour
{
    [Header("Prefab & Holder")]
    public GameObject paperSegmentPrefab;
    public Transform paperHolder;

    [Header("Segment Settings")]
    public float segmentHeight = 0.1f;
    public int maxVisibleSegments = 20; // Keep only this many segments visible

    [Header("Spawning Control")]
    public float spawnDistance = 0.05f; // How far the paper needs to move before spawning next segment
    public Transform toiletPaperRoll; // Drag your toilet paper roll here

    [Header("Culling Settings")]
    public float cullDistance = 5f; // Remove segments this far below the camera/screen
    public Camera playerCamera; // Will auto-find if not assigned

    private List<GameObject> segments = new List<GameObject>();
    private Vector3 lastSpawnPosition;
    private PaperRoller paperRoller;
    private int totalSegmentsCreated = 0; // For debugging/scoring

    void Start()
    {
        // Find the PaperRoller component
        paperRoller = FindFirstObjectByType<PaperRoller>();

        if (paperRoller == null)
        {
            Debug.LogError("PaperRoller not found! Make sure it exists in the scene.");
            return;
        }

        // Find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        }

        // Set initial spawn position based on roll position
        if (toiletPaperRoll != null)
        {
            lastSpawnPosition = toiletPaperRoll.position;
        }
        else
        {
            lastSpawnPosition = paperHolder.position;
            Debug.LogWarning("Toilet paper roll not assigned! Using paperHolder position instead.");
        }

        SpawnInitialSegments();
    }

    void Update()
    {
        // Check if we need to spawn new segments based on paper movement
        CheckForNewSegmentSpawn();

        // Remove segments that are too far off screen
        CullOffScreenSegments();

        // TEMP: Press Space to spawn another for testing on PC
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnSegment();
        }
    }

    void CheckForNewSegmentSpawn()
    {
        if (paperRoller == null)
            return;

        // Get the current position of the paper
        Vector3 currentPaperPosition = paperRoller.transform.position;

        // Calculate how far the paper has moved down from the last spawn position
        float distanceMoved = lastSpawnPosition.y - currentPaperPosition.y;

        // If the paper has moved far enough down, spawn a new segment
        if (distanceMoved >= spawnDistance)
        {
            SpawnSegment();
            // Update last spawn position to the current paper position
            lastSpawnPosition = currentPaperPosition;
        }
    }

    void CullOffScreenSegments()
    {
        if (playerCamera == null || segments.Count == 0)
            return;

        // Calculate the cull position (camera position minus cull distance)
        float cullY = playerCamera.transform.position.y - cullDistance;

        // Remove segments that are below the cull line
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            if (segments[i] != null && segments[i].transform.position.y < cullY)
            {
                GameObject segmentToRemove = segments[i];
                segments.RemoveAt(i);
                Destroy(segmentToRemove);
                Debug.Log($"Culled segment at Y: {segmentToRemove.transform.position.y}, Total segments: {segments.Count}");
            }
        }

        // Alternative culling method: Keep only the most recent segments
        while (segments.Count > maxVisibleSegments)
        {
            GameObject oldestSegment = segments[0];
            segments.RemoveAt(0);
            if (oldestSegment != null)
            {
                Destroy(oldestSegment);
                Debug.Log($"Removed oldest segment, Total segments: {segments.Count}");
            }
        }
    }

    void SpawnInitialSegments()
    {
        // Spawn initial segments starting from paper holder position
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = paperHolder.position + Vector3.down * segmentHeight * i;
            GameObject newSeg = Instantiate(paperSegmentPrefab, spawnPos, paperHolder.rotation);
            newSeg.transform.SetParent(paperHolder, true);
            segments.Add(newSeg);
            totalSegmentsCreated++;

            Debug.Log($"Initial segment {i} spawned at: {spawnPos}");
        }
    }

    void SpawnSegment()
    {
        Vector3 spawnPos;

        if (segments.Count == 0)
        {
            // First segment spawns from the paper holder position
            spawnPos = paperHolder.position;
            Debug.Log($"First segment - PaperHolder position: {paperHolder.position}");
        }
        else
        {
            // Subsequent segments spawn below the last segment
            GameObject last = segments[segments.Count - 1];
            if (last != null)
            {
                spawnPos = last.transform.position + Vector3.down * segmentHeight;
                Debug.Log($"Spawning below last segment at: {last.transform.position}");
            }
            else
            {
                // Fallback if last segment was destroyed
                spawnPos = paperHolder.position + Vector3.down * segmentHeight * segments.Count;
                Debug.Log($"Fallback spawn position: {spawnPos}");
            }
        }

        // Create the segment at world position first
        GameObject newSeg = Instantiate(paperSegmentPrefab, spawnPos, paperHolder.rotation);

        // Then set the parent (this preserves world position)
        newSeg.transform.SetParent(paperHolder, true);

        segments.Add(newSeg);
        totalSegmentsCreated++;

        Debug.Log($"Spawned segment #{totalSegmentsCreated} at WORLD position: {spawnPos}");
        Debug.Log($"Segment LOCAL position after parenting: {newSeg.transform.localPosition}");
        Debug.Log($"PaperHolder position: {paperHolder.position}, rotation: {paperHolder.rotation}");
    }

    // Get the total length of paper rolled (for scoring/UI)
    public float GetTotalPaperLength()
    {
        return totalSegmentsCreated * segmentHeight;
    }

    // Get current number of visible segments
    public int GetVisibleSegmentCount()
    {
        return segments.Count;
    }

    // Optional Debug Tool: Reset all segments
    [ContextMenu("Clear Paper")]
    public void ClearPaper()
    {
        foreach (var seg in segments)
        {
            if (seg != null)
                Destroy(seg);
        }
        segments.Clear();
        totalSegmentsCreated = 0;

        if (toiletPaperRoll != null)
            lastSpawnPosition = toiletPaperRoll.position;
        else
            lastSpawnPosition = paperHolder.position;

        Debug.Log("Cleared all paper segments.");
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        // Show paper holder position
        if (paperHolder != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(paperHolder.position, Vector3.one * 0.2f);
            Gizmos.DrawLine(paperHolder.position, paperHolder.position + Vector3.down * segmentHeight * 3);
        }

        // Show toilet paper roll position
        if (toiletPaperRoll != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(toiletPaperRoll.position, 0.1f);
        }

        // Show cull line
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            float cullY = playerCamera.transform.position.y - cullDistance;
            Gizmos.DrawLine(new Vector3(-2, cullY, 0), new Vector3(2, cullY, 0));
        }

        // Show active segments
        Gizmos.color = Color.yellow;
        foreach (var segment in segments)
        {
            if (segment != null)
            {
                Gizmos.DrawWireCube(segment.transform.position, Vector3.one * 0.05f);
            }
        }
    }
}