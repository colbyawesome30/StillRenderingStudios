using UnityEngine;
using UnityEngine.EventSystems;

public class CameraPan : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private float smoothTime = 0.08f; //Slows the pan
    [SerializeField] private float panSpeed = 1f;

    //Max panning
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minOffset = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxOffset = new Vector2(10f, 10f);

    //Zoom settings
    [Header("Zoom")]
    public float minZoom = 3f;            // closest
    public float maxZoom = 8f;            // farthest
    [SerializeField] private float scrollZoomSpeed = 0.1f;  // per mouse wheel notch
    [SerializeField] private float pinchZoomSensitivity = 1f;
    [SerializeField] private float zoomSmoothTime = 0.08f;

    private Vector3 startPosition;

    private bool pressing;
    private bool panning;
    private Vector2 startScreen;
    private Vector2 lastScreen;
    private float unitsPerPixel;
    private float verticalStretch; 

    private Vector3 targetPosition;
    private Vector3 velocity;

    private float targetZoom;
    private float zoomVelocity;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    //Set position
    private void Start()
    {
        startPosition = transform.position;
        targetPosition = transform.position;
        targetZoom = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom);
    }

    private void Update()
    {
        HandleInput();
        HandleZoom();

        // Glide toward the target instead of jumping
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    //Anything that should stop the camera from taking input
    private bool InputBlocked =>
        BuildMode.Active || WorldDrag.AnyDragging || Roster.UIDragging || WorkStation.UIOpen || PayWall.TabOpen || InGameSettings.SettingsIsOpen;

    //Pinch with two fingers or scroll the mouse wheel
    private void HandleZoom()
    {
        if (InputBlocked)
        {
            targetZoom = _camera.orthographicSize;
            zoomVelocity = 0f;
            return;
        }

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (IsFingerOverUI(t0.fingerId) || IsFingerOverUI(t1.fingerId)) return;

            float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            if (prevDist < 1f || currDist < 1f) return;

            // Fingers spreading apart (curr > prev) shrinks the size, which zooms in
            targetZoom *= Mathf.Pow(prevDist / currDist, pinchZoomSensitivity);
        }
        else
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !IsOverUI())
            {
                // Scroll up zooms in
                targetZoom *= 1f - scroll * scrollZoomSpeed;
            }
        }

        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    //Handle drags and touches
    private void HandleInput()
    {   
        if (InputBlocked)
        {
            pressing = false;
            panning = false;
            targetPosition = transform.position;
            velocity = Vector3.zero;
            return;
        }

        // Ignore multiple touches
        if (Input.touchCount > 1) { pressing = false; panning = false; return; }

        if (PressedThisFrame(out Vector2 pressPos))
        {
            // Only start if the press was not on a worker or on UI
            pressing = !IsOverUI() && !IsOverWorker(pressPos);
            panning = false;
            startScreen = pressPos;
            lastScreen = pressPos;
            return;
        }

        if (!pressing) return;

        if (ReleasedThisFrame())
        {
            pressing = false;
            panning = false;
            return;
        }

        Vector2 pos = CurrentPointerPosition();

        // Wait until the pointer has moved far enough to pan
        if (!panning)
        {
            if ((pos - startScreen).magnitude < dragThreshold) return;
            if (IsOverUI()) { pressing = false; return; }
            panning = true;
            lastScreen = pos;
            CalculateScale();
            targetPosition = transform.position;
            velocity = Vector3.zero;
            return;
        }

        // Zoom can change while dragging with the mouse wheel, so keep the scale current
        CalculateScale();

        Vector2 pixelDelta = pos - lastScreen;
        lastScreen = pos;

        // Camera axes flattened onto the floor
        Vector3 right = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
        Vector3 up = Vector3.ProjectOnPlane(_camera.transform.up, Vector3.up).normalized;

        // Drag right & world moves right
        Vector3 move = (right * pixelDelta.x + up * pixelDelta.y * verticalStretch) * unitsPerPixel * panSpeed;
        targetPosition -= move;

        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, startPosition.x + minOffset.x, startPosition.x + maxOffset.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, startPosition.z + minOffset.y, startPosition.z + maxOffset.y);
        }
    }

    
    private void CalculateScale()
    {
        unitsPerPixel = (_camera.orthographicSize * 2f) / Screen.height;

        verticalStretch = 1f / Mathf.Max(0.3f, Mathf.Abs(_camera.transform.forward.y));
    }

    //True if hit a worker
    private bool IsOverWorker(Vector2 screenPos)
    {
        Ray ray = _camera.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f)
               && hit.collider.GetComponentInParent<WorldDrag>() != null;
    }

    // True if the press is on UI
    private bool IsOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    // True if a specific finger is on UI
    private bool IsFingerOverUI(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private bool PressedThisFrame(out Vector2 pos)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pos = Input.GetTouch(0).position;
            return true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            return true;
        }
        pos = default;
        return false;
    }

    private bool ReleasedThisFrame()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled;
        return Input.GetMouseButtonUp(0);
    }

    private Vector2 CurrentPointerPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return Input.mousePosition;
    }
}