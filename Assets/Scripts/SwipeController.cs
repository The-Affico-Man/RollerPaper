using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SwipeController : MonoBehaviour
{
    private GameControls controls;
    private Vector2 swipeDelta;
    public Vector2 SwipeDelta => swipeDelta;

    private Vector2 lastMousePosition;
    private float totalSwipeDistance = 0f;
    public float TotalSwipeDistance => totalSwipeDistance;

    [Header("Cat Paw Settings")]
    public GameObject catPawPrefab;

    // Dictionary to track paws for each touch ID
    private Dictionary<int, GameObject> activePaws = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
    private HashSet<int> processedTouchIds = new HashSet<int>(); // Track which touches we've already started

    private bool isMouseDragging = false;
    private GameObject mousePaw; // Separate paw for mouse input

    private Camera mainCamera;

    [Header("Sound Settings")]
    public AudioClip[] meowClips;
    public AudioClip paperRollingClip;
    public AudioClip paperClingClip;

    private AudioSource audioSource;
    private AudioSource rollingAudioSource;

    private bool isRollingPlaying = false;
    private float meowChancePerTouch = 0.3f;

    private void Awake()
    {
        controls = new GameControls();
        mainCamera = Camera.main;

        // Create AudioSources
        audioSource = gameObject.AddComponent<AudioSource>();
        rollingAudioSource = gameObject.AddComponent<AudioSource>();
        rollingAudioSource.clip = paperRollingClip;
        rollingAudioSource.loop = true;
        rollingAudioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        rollingAudioSource.spatialBlend = 0f;
        audioSource.volume = 0.1f;
        rollingAudioSource.volume = 3f;
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();

        // Keep original Input System events for backwards compatibility
        controls.Gameplay.Swipe.started += ctx =>
        {
            Vector2 pos = ctx.ReadValue<Vector2>();
            swipeDelta = Vector2.zero;
        };

        controls.Gameplay.Swipe.performed += ctx =>
        {
            swipeDelta = ctx.ReadValue<Vector2>();

            if (swipeDelta.y < -0.1f && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(paperClingClip);
            }
        };

        controls.Gameplay.Swipe.canceled += ctx =>
        {
            swipeDelta = Vector2.zero;
        };
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();

        // Clean up all paws
        foreach (var paw in activePaws.Values)
        {
            if (paw != null)
                Destroy(paw);
        }
        activePaws.Clear();
        lastTouchPositions.Clear();
        processedTouchIds.Clear();

        if (mousePaw != null)
        {
            Destroy(mousePaw);
            mousePaw = null;
        }
    }

    private void Update()
    {
        HandleTouchInput();
        HandleMouseInput();

        // Update total swipe distance (using primary swipe delta)
        if (swipeDelta.y < 0f)
        {
            totalSwipeDistance += -swipeDelta.y;
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;
        HashSet<int> currentlyActiveTouches = new HashSet<int>();

        // Process all touches
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            int touchId = touch.touchId.ReadValue();
            Vector2 touchPosition = touch.position.ReadValue();
            var phase = touch.phase.ReadValue();
            bool isPressed = touch.press.isPressed;

            // Track currently active touches (pressed and not ended/canceled)
            if (isPressed && phase != UnityEngine.InputSystem.TouchPhase.Ended &&
                phase != UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                currentlyActiveTouches.Add(touchId);
            }

            // Handle touch phases
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (!processedTouchIds.Contains(touchId))
                    {
                        OnTouchStart(touchId, touchPosition);
                        processedTouchIds.Add(touchId);
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (activePaws.ContainsKey(touchId))
                    {
                        OnTouchMove(touchId, touchPosition);
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (processedTouchIds.Contains(touchId))
                    {
                        OnTouchEnd(touchId);
                        processedTouchIds.Remove(touchId);
                    }
                    break;
            }
        }

        // Clean up any paws that no longer have active touches
        var pawsToRemove = new List<int>();
        foreach (var touchId in activePaws.Keys)
        {
            if (!currentlyActiveTouches.Contains(touchId))
            {
                pawsToRemove.Add(touchId);
            }
        }

        foreach (var touchId in pawsToRemove)
        {
            OnTouchEnd(touchId);
            processedTouchIds.Remove(touchId);
        }
    }

    private void HandleMouseInput()
    {
#if UNITY_EDITOR
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            OnMouseStart(mousePos);
        }
        else if (Mouse.current.leftButton.isPressed && isMouseDragging)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            OnMouseMove(mousePos);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && isMouseDragging)
        {
            OnMouseEnd();
        }
#endif
    }

    private void OnTouchStart(int touchId, Vector2 position)
    {
        // Only create paw if we don't already have one for this touch
        if (activePaws.ContainsKey(touchId)) return;

        // Create new paw for this touch
        GameObject newPaw = CreateCatPaw(position);
        if (newPaw != null)
        {
            activePaws[touchId] = newPaw;
            lastTouchPositions[touchId] = position;

            PlayMeowSound();
            StartRollingSound();
        }
    }

    private void OnTouchMove(int touchId, Vector2 position)
    {
        if (activePaws.ContainsKey(touchId) && activePaws[touchId] != null)
        {
            MoveCatPawToScreenPosition(activePaws[touchId], position);

            // Calculate swipe delta for this touch
            if (lastTouchPositions.ContainsKey(touchId))
            {
                Vector2 delta = position - lastTouchPositions[touchId];
                // Update main swipe delta with the most recent touch movement
                swipeDelta = delta;
            }

            lastTouchPositions[touchId] = position;
        }
    }

    private void OnTouchEnd(int touchId)
    {
        if (activePaws.ContainsKey(touchId))
        {
            GameObject paw = activePaws[touchId];
            if (paw != null)
            {
                paw.SetActive(false);
                Destroy(paw);
            }
            activePaws.Remove(touchId);
        }

        if (lastTouchPositions.ContainsKey(touchId))
        {
            lastTouchPositions.Remove(touchId);
        }

        // Stop rolling sound if no touches are active
        if (activePaws.Count == 0 && !isMouseDragging)
        {
            StopRollingSound();
        }
    }

    private void OnMouseStart(Vector2 position)
    {
        if (isMouseDragging) return; // Prevent multiple mouse paws

        isMouseDragging = true;
        mousePaw = CreateCatPaw(position);
        lastMousePosition = position;

        PlayMeowSound();
        StartRollingSound();
    }

    private void OnMouseMove(Vector2 position)
    {
        if (mousePaw != null)
        {
            MoveCatPawToScreenPosition(mousePaw, position);
            swipeDelta = position - lastMousePosition;
            lastMousePosition = position;
        }
    }

    private void OnMouseEnd()
    {
        isMouseDragging = false;
        if (mousePaw != null)
        {
            mousePaw.SetActive(false);
            Destroy(mousePaw);
            mousePaw = null;
        }
        lastMousePosition = Vector2.zero;
        swipeDelta = Vector2.zero;

        // Stop rolling sound if no touches are active
        if (activePaws.Count == 0)
        {
            StopRollingSound();
        }
    }

    private GameObject CreateCatPaw(Vector2 screenPosition)
    {
        if (catPawPrefab == null) return null;

        GameObject newPaw = Instantiate(catPawPrefab);

        // Random rotation for each new paw
        float randomZRotation = Random.Range(-30f, 30f);
        newPaw.transform.rotation = Quaternion.Euler(0f, 0f, randomZRotation);

        MoveCatPawToScreenPosition(newPaw, screenPosition);

        return newPaw;
    }

    private void MoveCatPawToScreenPosition(GameObject paw, Vector2 screenPos)
    {
        if (paw == null || mainCamera == null) return;

        float distanceFromCamera = 4.5f;
        Vector3 screenPositionWithDepth = new Vector3(screenPos.x, screenPos.y, distanceFromCamera);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPositionWithDepth);
        paw.transform.position = worldPos;
    }

    private void PlayMeowSound()
    {
        if (meowClips.Length > 0 && Random.value < meowChancePerTouch)
        {
            int clipIndex = Random.Range(0, meowClips.Length);
            audioSource.PlayOneShot(meowClips[clipIndex]);
        }
    }

    private void StartRollingSound()
    {
        if (!isRollingPlaying)
        {
            rollingAudioSource.Play();
            isRollingPlaying = true;
        }
    }

    private void StopRollingSound()
    {
        if (isRollingPlaying)
        {
            rollingAudioSource.Stop();
            isRollingPlaying = false;
        }
    }

    public float GetSwipeDeltaY()
    {
        return SwipeDelta.y;
    }

    // Public method to get count of active touches/paws
    public int GetActivePawCount()
    {
        int count = activePaws.Count;
        if (isMouseDragging) count++;
        return count;
    }
}