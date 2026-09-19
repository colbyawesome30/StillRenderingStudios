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

    private Vector3 startPosition;

    private bool pressing;
    private bool panning;
    private Vector2 startScreen;
    private Vector2 lastScreen;
    private float unitsPerPixel;
    private float verticalStretch; 

    private Vector3 targetPosition;
    private Vector3 velocity;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    //Set position
    private void Start()
    {
        startPosition = transform.position;
        targetPosition = transform.position;
    }
    private void Update()
    {
        HandleInput();

        // Glide toward the target instead of jumping
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    //Handle drags and touches
    private void HandleInput()
    {   
        if (WorldDrag.AnyDragging || Roster.UIDragging)
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