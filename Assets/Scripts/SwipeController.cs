using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SwipeController : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("How quickly the swipe input adapts. Lower is smoother, higher is more responsive.")]
    [Range(1f, 30f)]
    public float swipeSmoothingFactor = 15f;
    [Tooltip("Only touches that hit an object on this layer will be processed.")]
    public LayerMask interactableLayerMask;
    [Tooltip("The maximum number of fingers allowed to touch the screen at once.")]
    public int maxSimultaneousTouches = 2;

    [Header("Cat Paw Settings")]
    public GameObject catPawPrefab;

    private GameControls controls;
    private Vector2 rawSwipeDelta;
    private Vector2 smoothedSwipeDelta;
    public Vector2 SwipeDelta => smoothedSwipeDelta;
    public int ActivePullingFingers { get; private set; }

    private Vector2 lastMousePosition;
    private float totalSwipeDistance = 0f;
    public float TotalSwipeDistance => totalSwipeDistance;
    private Dictionary<int, GameObject> activePaws = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
    private bool isMouseDragging = false;
    private GameObject mousePaw;
    private Camera mainCamera;
    [Header("Sound Settings")]
    public AudioClip[] meowClips;
    public AudioClip paperRollingClip;
    public AudioClip paperClingClip;
    private AudioSource audioSource;
    private AudioSource rollingAudioSource;
    private bool isRollingPlaying = false;
    private float meowChancePerTouch = 0.3f;
    private float lastSwipeSpeed = 0f;
    public float GetSwipeSpeed() => Mathf.Abs(lastSwipeSpeed);

    private void Awake()
    {
        controls = new GameControls();
        mainCamera = Camera.main;
        audioSource = gameObject.AddComponent<AudioSource>();
        rollingAudioSource = gameObject.AddComponent<AudioSource>();
        rollingAudioSource.clip = paperRollingClip;
        rollingAudioSource.loop = true;
        rollingAudioSource.playOnAwake = false;
        audioSource.volume = 0.1f;
        rollingAudioSource.volume = 3f;
    }

    private void OnEnable() { controls.Gameplay.Enable(); }
    private void OnDisable() { controls.Gameplay.Disable(); }

    private void Update()
    {
        HandleTouchInput();
        HandleMouseInput();
        smoothedSwipeDelta = Vector2.Lerp(smoothedSwipeDelta, rawSwipeDelta, Time.deltaTime * swipeSmoothingFactor);
        lastSwipeSpeed = smoothedSwipeDelta.y < 0f ? -smoothedSwipeDelta.y / Time.deltaTime : 0f;
        if (smoothedSwipeDelta.y < 0f) { totalSwipeDistance += -smoothedSwipeDelta.y; }
        if (smoothedSwipeDelta.y < -0.1f) { ActivePullingFingers = GetActivePawCount(); }
        else { ActivePullingFingers = 0; }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;
        var touches = Touchscreen.current.touches;
        HashSet<int> currentlyActiveTouches = new HashSet<int>();
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            int touchId = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            if (touch.press.isPressed) { currentlyActiveTouches.Add(touchId); }
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began: OnTouchStart(touchId, touch.position.ReadValue()); break;
                case UnityEngine.InputSystem.TouchPhase.Moved: if (activePaws.ContainsKey(touchId)) { OnTouchMove(touchId, touch.position.ReadValue()); } break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled: if (activePaws.ContainsKey(touchId)) { OnTouchEnd(touchId); } break;
            }
        }
        var pawsToRemove = new List<int>();
        foreach (var touchId in activePaws.Keys) { if (!currentlyActiveTouches.Contains(touchId)) { pawsToRemove.Add(touchId); } }
        foreach (var touchId in pawsToRemove) { OnTouchEnd(touchId); }
    }

    private void HandleMouseInput()
    {
#if UNITY_EDITOR
        if (Mouse.current.leftButton.wasPressedThisFrame) { OnMouseStart(Mouse.current.position.ReadValue()); }
        else if (Mouse.current.leftButton.isPressed && isMouseDragging) { OnMouseMove(Mouse.current.position.ReadValue()); }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && isMouseDragging) { OnMouseEnd(); }
#endif
    }

    private void OnTouchStart(int touchId, Vector2 position)
    {
        Ray ray = mainCamera.ScreenPointToRay(position);
        if (!Physics.Raycast(ray, 100f, interactableLayerMask)) return;
        if (activePaws.Count + (isMouseDragging ? 1 : 0) >= maxSimultaneousTouches) return;
        if (activePaws.ContainsKey(touchId)) return;
        GameObject newPaw = CreateCatPaw(position);
        if (newPaw != null) { activePaws[touchId] = newPaw; lastTouchPositions[touchId] = position; PlayMeowSound(); StartRollingSound(); }
    }

    private void OnTouchMove(int touchId, Vector2 position)
    {
        if (activePaws.TryGetValue(touchId, out GameObject paw) && paw != null)
        {
            MoveCatPawToScreenPosition(paw, position);
            if (lastTouchPositions.TryGetValue(touchId, out Vector2 lastPos))
            {
                rawSwipeDelta = position - lastPos;
                lastTouchPositions[touchId] = position;
            }
        }
    }

    private void OnTouchEnd(int touchId)
    {
        if (activePaws.TryGetValue(touchId, out GameObject paw)) { if (paw != null) { Destroy(paw); } activePaws.Remove(touchId); lastTouchPositions.Remove(touchId); }
        if (activePaws.Count == 0 && !isMouseDragging) { StopRollingSound(); rawSwipeDelta = Vector2.zero; }
    }

    private void OnMouseStart(Vector2 position)
    {
        Ray ray = mainCamera.ScreenPointToRay(position);
        if (!Physics.Raycast(ray, 100f, interactableLayerMask)) return;
        if (activePaws.Count >= maxSimultaneousTouches) return;
        if (isMouseDragging) return;
        isMouseDragging = true;
        mousePaw = CreateCatPaw(position);
        lastMousePosition = position;
        PlayMeowSound();
        StartRollingSound();
    }

    private void OnMouseMove(Vector2 position)
    {
        if (mousePaw != null) { MoveCatPawToScreenPosition(mousePaw, position); rawSwipeDelta = position - lastMousePosition; lastMousePosition = position; }
    }

    private void OnMouseEnd()
    {
        isMouseDragging = false;
        if (mousePaw != null) { Destroy(mousePaw); mousePaw = null; }
        lastMousePosition = Vector2.zero;
        rawSwipeDelta = Vector2.zero;
        if (activePaws.Count == 0) { StopRollingSound(); }
    }

    private GameObject CreateCatPaw(Vector2 screenPosition) { if (catPawPrefab == null) return null; GameObject newPaw = Instantiate(catPawPrefab); newPaw.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f)); MoveCatPawToScreenPosition(newPaw, screenPosition); return newPaw; }
    private void MoveCatPawToScreenPosition(GameObject paw, Vector2 screenPos) { if (paw == null || mainCamera == null) return; Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 4.5f)); paw.transform.position = worldPos; }
    private void PlayMeowSound() { if (meowClips.Length > 0 && Random.value < meowChancePerTouch) { audioSource.PlayOneShot(meowClips[Random.Range(0, meowClips.Length)]); } }
    private void StartRollingSound() { if (!isRollingPlaying) { rollingAudioSource.Play(); isRollingPlaying = true; } }
    private void StopRollingSound() { if (isRollingPlaying) { rollingAudioSource.Stop(); isRollingPlaying = false; } }
    public float GetSwipeDeltaY() => SwipeDelta.y;
    public int GetActivePawCount() { return activePaws.Count + (isMouseDragging ? 1 : 0); }
}