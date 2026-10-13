using UnityEngine;

public interface IInteractable
{
    void Interact(RaycastHit hit);
}

public class InteractScript : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera to raycast from. Defaults to Camera.main if left empty.")]
    [SerializeField] private Camera _camera;

    [Tooltip("Which layers the ray is allowed to hit. Defaults to Everything.")]
    [SerializeField] private LayerMask _hitLayers = ~0;

    [Header("Debug")]
    [SerializeField] private bool _drawDebugRay = false;

    [SerializeField] private GameObject gridField;
    [SerializeField] private LayerMask _groundLayer;  

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void Update()
    {
        if (WorkStation.UIOpen) return;

        UpdateGroundHover();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Only fire on the first frame of contact, i.e. an actual "tap".
            if (touch.phase == TouchPhase.Began)
            {
                TryInteract(touch.position);
            }
        }
        // --- Mouse fallback (editor / desktop testing) ---
        else if (Input.GetMouseButtonDown(0))
        {
            TryInteract(Input.mousePosition);
        }
    }

    //Send ray to try and interact
    private void TryInteract(Vector2 screenPosition)
    {
        Ray ray = _camera.ScreenPointToRay(screenPosition);

        if (_drawDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 1f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 1000, _hitLayers))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(hit);
            }
        }
    }

    private void UpdateGroundHover()
    {
        if (gridField == null || _camera == null) return;

        // Pick the pointer: finger if touching, otherwise mouse
        Vector3 pointer;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) return;
            pointer = touch.position;
        }
        else
        {
            pointer = Input.mousePosition;
        }

        // Skip invalid positions (mouse outside window, no mouse on device)
        if (float.IsInfinity(pointer.x) || float.IsInfinity(pointer.y) ||
            float.IsNaN(pointer.x) || float.IsNaN(pointer.y)) return;

        // Skip positions outside the camera's viewport
        if (!_camera.pixelRect.Contains(pointer)) return;

        Ray ray = _camera.ScreenPointToRay(pointer);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
        {
            // World-space position, slightly above the surface along its normal
            gridField.transform.position = hit.point + hit.normal * 0.02f;
        }
    }
}